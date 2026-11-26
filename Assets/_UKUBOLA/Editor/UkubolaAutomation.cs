using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.EditorTools
{
    [Serializable] public sealed class EvolutionJob { public string id, operation, scene; }
    [Serializable] public sealed class EvolutionJobState { public EvolutionJob job; public string phase, boot, originalScene; public long startedUtc, deadlineUtc; }
    [Serializable] public sealed class EvolutionJobResult { public string id, operation, status, detail, utc; public double seconds; }

    /// <summary>Single-worker, disk-journalled, closed-command Editor automation; no arbitrary code execution.</summary>
    [InitializeOnLoad]
    public static class UkubolaAutomation
    {
        public static string Project => Directory.GetParent(Application.dataPath).FullName;
        public static string Queue => Path.Combine(Project, "Temp", "UkubolaAutomation");
        public static string Evidence => Path.Combine(Project, "Evidence", "Evolution");
        private static string Journal => Path.Combine(Queue, "active.json");
        private static readonly string Boot = Guid.NewGuid().ToString("N");
        private static bool busy;
        private static double next, started;
        private static string sourceHash = SourceHash();
        private static string SourceHash()
        {
            string sources = string.Join("\n", Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories)
                .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.Ordinal).Select(p => p + ":" + HashFile(p)));
            return SaveStore.Hash(sources);
        }
        static UkubolaAutomation() { EditorApplication.update += Tick; }
        private static void Write(string path, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); string temporary = path + ".writing";
            File.WriteAllText(temporary, JsonUtility.ToJson(value, true));
            if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
        }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void CleanScenes()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                Check(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty, "Unsaved scene: save or discard it before automation.");
        }
        private static void Tick()
        {
            if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .5;
            Directory.CreateDirectory(Queue); Directory.CreateDirectory(Evidence);
            if (File.Exists(Journal))
            {
                var state = JsonUtility.FromJson<EvolutionJobState>(File.ReadAllText(Journal));
                if (state.phase == "reload" && state.boot != Boot) { Finish(state.job, true, "Checkpoint resumed after domain reload; operation not repeated."); return; }
                if (state.phase == "play") { PollPlay(state); return; }
                if (state.phase == "executing" && state.boot != Boot) { Finish(state.job, false, "Interrupted during execution. Inspect outputs before submitting a new job; no automatic replay."); return; }
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string request = Directory.GetFiles(Queue, "*.job.json").OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (request == null) return;
            string currentSources = SourceHash();
            if (currentSources != sourceHash)
            {
                sourceHash = currentSources;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                return; // Wait for compilation/domain reload before claiming any request.
            }
            EvolutionJob job = null;
            try
            {
                job = JsonUtility.FromJson<EvolutionJob>(File.ReadAllText(request));
                Check(job != null && Guid.TryParseExact(job.id, "N", out _) && !string.IsNullOrEmpty(job.operation), "Malformed job.");
                if (File.Exists(Path.Combine(Evidence, job.id, "result.json"))) { File.Move(request, request + ".duplicate-" + Guid.NewGuid().ToString("N")); return; }
                CleanScenes();
                var state = new EvolutionJobState { job = job, phase = "executing", boot = Boot, startedUtc = DateTime.UtcNow.Ticks, originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path };
                Write(Journal, state); File.Move(request, request + ".claimed"); started = EditorApplication.timeSinceStartup;
                if (job.operation == "ReloadCheckpoint") { state.phase = "reload"; Write(Journal, state); EditorUtility.RequestScriptReload(); return; }
                if (job.operation == "PlayQa") { StartPlay(state); return; }
                busy = true;
                string detail = Execute(job);
                Finish(job, true, detail);
            }
            catch (Exception e)
            {
                if (job != null && Guid.TryParseExact(job.id, "N", out _))
                {
                    if (File.Exists(request)) File.Move(request, request + ".rejected-" + Guid.NewGuid().ToString("N"));
                    Finish(job, false, e.ToString());
                }
                else { File.Move(request, request + ".rejected-" + Guid.NewGuid().ToString("N")); Debug.LogWarning("[UKUBOLA Evolution] Rejected malformed request: " + e.Message); }
            }
            finally { busy = false; }
        }
        private static string Execute(EvolutionJob job)
        {
            switch (job.operation)
            {
                case "Audit": return Audit();
                case "SelfTests": return UkubolaEvolutionTests.Run();
                case "ValidateLegacy": WithScenes(UkubolaPersistenceSetup.ValidateAll); return "Legacy scene references, routes and save signatures valid.";
                case "BuildLegacy": return BuildLegacy(job.id);
                case "RestoreBuildSettings":
                    var preloaded = PlayerSettings.GetPreloadedAssets();
                    Check(preloaded.Length == 0 || preloaded.Length == 1 && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preloaded[0])) == "052faaac586de48259a63d0c4782560b", "Preloaded assets differ from the known Input System build side effect; preserving settings.");
                    PlayerSettings.SetPreloadedAssets(Array.Empty<UnityEngine.Object>()); AssetDatabase.SaveAssets();
                    return "Restored baseline empty preloadedAssets using PlayerSettings API.";
                case "Fail": throw new InvalidOperationException("Intentional failure to verify failure reporting.");
                default: throw new InvalidOperationException("Unknown operation: " + job.operation);
            }
        }
        private static void WithScenes(Action action)
        {
            CleanScenes(); var setup = EditorSceneManager.GetSceneManagerSetup();
            try { action(); } finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        public static string HashFile(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static string Audit()
        {
            Check(Application.unityVersion == "6000.6.10f1", "Unexpected Unity version.");
            Check(Directory.Exists(Path.Combine(Project, "Backups", "Evolution", "baseline")), "Missing baseline backup.");
            Check(File.Exists(Path.Combine(Project, "Backups", "Evolution", "baseline", "manifest.json")), "Missing baseline manifest.");
            return "Unity " + Application.unityVersion + " | " + SystemInfo.graphicsDeviceName + " | " + SystemInfo.systemMemorySize + " MB RAM | backup present.";
        }
        private static string BuildLegacy(string id)
        {
            string folder = Path.Combine(Project, "Builds", "UKUBOLA_Legacy_baee6d2_QA2");
            string executable = Path.Combine(folder, "UKUBOLA.exe"), complete = Path.Combine(folder, "BUILD_COMPLETE.json");
            if (File.Exists(complete) && File.Exists(executable))
            {
                if (!File.Exists(folder + ".zip")) UkubolaFinalBuild.Package(folder, folder + ".zip");
                Check(File.Exists(folder + ".zip.sha256") && File.ReadAllText(folder + ".zip.sha256").StartsWith(HashFile(folder + ".zip"), StringComparison.Ordinal), "Conservation archive checksum mismatch.");
                return "Existing completed conservation build and archive preserved: " + folder;
            }
            Check(!Directory.Exists(folder), "Incomplete legacy build exists; preserve it and select a new output before retrying.");
            WithScenes(() => {
                UkubolaPersistenceSetup.ValidateAll(); Directory.CreateDirectory(folder);
                var previousPreloads = PlayerSettings.GetPreloadedAssets();
                BuildReport report;
                try { report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = UkubolaPersistenceSetup.Paths,
                    target = BuildTarget.StandaloneWindows64, locationPathName = executable, options = BuildOptions.Development }); }
                finally { PlayerSettings.SetPreloadedAssets(previousPreloads); AssetDatabase.SaveAssets(); }
                Check(report.summary.result == BuildResult.Succeeded, "Conservation build failed: " + report.summary.result);
                Write(complete, new EvolutionJobResult { id = id, operation = "BuildLegacy", status = "succeeded", detail = "Base baee6d2 + explicit QA isolation. Four legacy scenes; production identity retained.", utc = DateTime.UtcNow.ToString("O") });
                File.WriteAllText(Path.Combine(folder, "README.txt"), "UKUBOLA conservation build. Normal launch uses original v1 saves. QA requires -ukubola-qa-root <absolute isolated directory>. Keep all extracted files together.\n");
            });
            UkubolaFinalBuild.Package(folder, folder + ".zip"); return folder;
        }
        private static void StartPlay(EvolutionJobState state)
        {
            Check(!string.IsNullOrEmpty(state.job.scene) && state.job.scene.StartsWith("Assets/_UKUBOLA/Scenes/", StringComparison.Ordinal) && AssetDatabase.LoadAssetAtPath<SceneAsset>(state.job.scene), "QA requires a known UKUBOLA scene.");
            string root = Path.Combine(Evidence, state.job.id, "qa"); Directory.CreateDirectory(root);
            Write(Path.Combine(root, "scenario.json"), new QaScenario { scene = state.job.scene });
            File.WriteAllText(Path.Combine(Queue, "qa-root.txt"), root);
            EditorSceneManager.OpenScene(state.job.scene); state.phase = "play"; state.deadlineUtc = DateTime.UtcNow.AddMinutes(3).Ticks; Write(Journal, state); EditorApplication.isPlaying = true;
        }
        private static void PollPlay(EvolutionJobState state)
        {
            string path = Path.Combine(Evidence, state.job.id, "qa", "run-result.json");
            if (!File.Exists(path))
            {
                if (state.deadlineUtc > 0 && DateTime.UtcNow.Ticks > state.deadlineUtc && EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
                if (!EditorApplication.isPlayingOrWillChangePlaymode) { CleanupPlay(state); Finish(state.job, false, "Play Mode interrupted before QA completed."); }
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
            var report = JsonUtility.FromJson<QaRunResult>(File.ReadAllText(path)); CleanupPlay(state); Finish(state.job, report.success, path);
        }
        private static void CleanupPlay(EvolutionJobState state)
        {
            string marker = Path.Combine(Queue, "qa-root.txt"); if (File.Exists(marker)) File.Delete(marker);
            if (!string.IsNullOrEmpty(state.originalScene)) EditorSceneManager.OpenScene(state.originalScene);
        }
        private static void Finish(EvolutionJob job, bool success, string detail)
        {
            double seconds = Math.Max(0, EditorApplication.timeSinceStartup - started);
            if (File.Exists(Journal))
            {
                var state = JsonUtility.FromJson<EvolutionJobState>(File.ReadAllText(Journal));
                if (state.startedUtc > 0) seconds = Math.Max(0, (DateTime.UtcNow.Ticks - state.startedUtc) / (double)TimeSpan.TicksPerSecond);
            }
            Write(Path.Combine(Evidence, job.id, "result.json"), new EvolutionJobResult { id = job.id, operation = job.operation,
                status = success ? "succeeded" : "failed", detail = detail, utc = DateTime.UtcNow.ToString("O"), seconds = seconds });
            if (File.Exists(Journal)) File.Delete(Journal);
            Debug.Log("[UKUBOLA Evolution] " + job.operation + " " + (success ? "SUCCEEDED" : "FAILED") + " | " + detail);
        }
        [MenuItem("Tools/UKUBOLA/Evolution/Audit")] public static void QueueAudit() => Enqueue("Audit");
        [MenuItem("Tools/UKUBOLA/Evolution/Run Infrastructure Tests")] public static void QueueTests() => Enqueue("SelfTests");
        [MenuItem("Tools/UKUBOLA/Evolution/Build Conservation Copy")] public static void QueueLegacy() => Enqueue("BuildLegacy");
        public static string Enqueue(string operation, string scene = null)
        {
            var job = new EvolutionJob { id = Guid.NewGuid().ToString("N"), operation = operation, scene = scene };
            Write(Path.Combine(Queue, job.id + ".job.json"), job); return job.id;
        }
        public static void RunBatch()
        {
            try
            {
                CleanScenes(); var job = new EvolutionJob { id = Guid.NewGuid().ToString("N"), operation = QaEnvironment.Argument("-ukubola-operation") ?? "Audit" };
                started = EditorApplication.timeSinceStartup; string detail = Execute(job); Finish(job, true, detail); EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
