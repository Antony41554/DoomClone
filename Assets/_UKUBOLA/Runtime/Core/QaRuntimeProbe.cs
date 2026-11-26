using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Ukubola.Rendering;
using UnityEngine.SceneManagement;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Persistence;
using Ukubola.UI;

namespace Ukubola
{
    /// <summary>Runs only with explicit QA opt-in. Screenshots contain real rendered gameplay and UI.</summary>
    public sealed class QaRuntimeProbe : MonoBehaviour
    {
        private bool finished;
        private readonly List<string> captures = new List<string>();
        private readonly List<float> times = new List<float>();
        private QaScenario scenario;
        private SaveData loaded;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!QaEnvironment.Active || FindAnyObjectByType<QaRuntimeProbe>()) return;
            var go = new GameObject("UKUBOLA isolated QA"); DontDestroyOnLoad(go); go.AddComponent<QaRuntimeProbe>();
        }
        private IEnumerator Start()
        {
            string config = Path.Combine(QaEnvironment.Root, "scenario.json");
            scenario = File.Exists(config) ? JsonUtility.FromJson<QaScenario>(File.ReadAllText(config)) : new QaScenario();
            scenario.mode = QaEnvironment.Argument("-ukubola-qa-mode") ?? scenario.mode;
            scenario.scene = QaEnvironment.Argument("-ukubola-qa-scene") ?? scenario.scene;
            if (scenario.mode == "manual") { finished = true; yield break; }
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            yield return null; yield return null;
            bool valid = true; string problem = null;
            try
            {
                if (scenario.mode == "legacy-load")
                {
                    if (!SaveStore.TryRead(SaveStore.FilePath, out loaded, out _, out var reason)) throw new InvalidDataException(reason);
                    SaveStore.Pending = loaded; CampaignRun.Set(loaded.Entry());
                    SceneManager.LoadScene(Path.GetFileNameWithoutExtension(SaveCampaignPaths.Missions[loaded.stage]));
                }
                else if (!string.IsNullOrEmpty(scenario.scene) && SceneManager.GetActiveScene().name != Path.GetFileNameWithoutExtension(scenario.scene))
                    SceneManager.LoadScene(Path.GetFileNameWithoutExtension(scenario.scene));
            }
            catch (Exception e) { valid = false; problem = e.ToString(); }
            if (!valid) { Complete(false, problem, false); yield break; }
            yield return null; yield return null; yield return null;
            bool roundTrip = false;
            try
            {
                if (loaded != null)
                {
                    var flow = FindAnyObjectByType<MenuFlow>();
                    var actual = SaveWorld.Capture(flow.GetComponent<CampaignStage>(), flow.Hud);
                    if (actual.current.hp != loaded.current.hp || Vector3.Distance(actual.position, loaded.position) > .3f)
                        throw new InvalidDataException("Legacy load did not restore player state.");
                    SaveWorld.Check(SaveStore.Decode(SaveStore.Encode(actual)), flow.GetComponent<CampaignStage>(), flow.Hud);
                    roundTrip = true;
                }
            }
            catch (Exception e) { valid = false; problem = e.ToString(); }
            if (!valid) { Complete(false, problem, false); yield break; }
            var player = FindAnyObjectByType<Ukubola.Player.PlayerLife>();
            if (player) player.GetComponent<Powerup>().Grant(PowerupKind.Invulnerability, 600);
            for (int i = 0; i < Mathf.Clamp(scenario.warmupFrames, 3, 300); i++) yield return null;
            for (int i = 0; i < Mathf.Clamp(scenario.sampleFrames, 1, 18000); i++)
            {
                yield return null;
                times.Add(Time.unscaledDeltaTime * 1000f);
            }
            foreach (float yaw in scenario.yaw ?? new float[] { 0 })
            {
                if (player) player.transform.rotation = Quaternion.Euler(0, yaw, 0);
                yield return null; yield return new WaitForEndOfFrame();
                string path = Path.Combine(QaEnvironment.Root, "capture-" + captures.Count.ToString("00") + ".png");
                try { CaptureRenderedFrame(path); captures.Add(path); }
                catch (Exception e) { Complete(false, e.ToString(), roundTrip); yield break; }
            }
            Complete(true, null, roundTrip);
        }
        private static void CaptureRenderedFrame(string path)
        {
            var retro = FindAnyObjectByType<RetroDisplay>();
            if (!retro) throw new InvalidOperationException("Offscreen QA requires a configured RetroDisplay.");
            var request = new RenderPipeline.StandardRequest { destination = retro.WorldCamera.targetTexture };
            if (!RenderPipeline.SupportsRenderRequest(retro.WorldCamera, request)) throw new NotSupportedException("URP camera render requests are unavailable.");
            RenderPipeline.SubmitRenderRequest(retro.WorldCamera, request);
            var output = retro.OutputCamera;
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var targets = canvases.Select(c => c.worldCamera).ToArray();
            var distances = canvases.Select(c => c.planeDistance).ToArray();
            int mask = output.cullingMask; float aspect = output.aspect;
            var previousTarget = output.targetTexture; var previousActive = RenderTexture.active;
            var destination = RenderTexture.GetTemporary(Mathf.Max(320, Screen.width), Mathf.Max(240, Screen.height), 24, RenderTextureFormat.ARGB32);
            Texture2D texture = null;
            try
            {
                output.targetTexture = destination; output.aspect = destination.width / (float)destination.height;
                output.cullingMask = 1 << LayerMask.NameToLayer("UI");
                foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = output; canvas.planeDistance = 1f; }
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(output, new RenderPipeline.StandardRequest { destination = destination });
                RenderTexture.active = destination;
                texture = new Texture2D(destination.width, destination.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, destination.width, destination.height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); int varied = 0; var first = pixels[0];
                for (int i = 0; i < pixels.Length; i += 97)
                    if (Math.Abs(pixels[i].r - first.r) + Math.Abs(pixels[i].g - first.g) + Math.Abs(pixels[i].b - first.b) > 30) varied++;
                if (varied < 30) throw new InvalidOperationException("Blank render rejected; no visual acceptance evidence produced.");
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = targets[i]; canvases[i].planeDistance = distances[i]; }
                output.targetTexture = previousTarget; output.cullingMask = mask; output.aspect = aspect;
                RenderTexture.active = previousActive; RenderTexture.ReleaseTemporary(destination);
                if (texture) Destroy(texture); Canvas.ForceUpdateCanvases();
            }
        }
        private void Complete(bool success, string error, bool roundTrip)
        {
            finished = true;
            var sorted = times.OrderBy(x => x).ToArray();
            float Percent(float p) => sorted.Length == 0 ? 0 : sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Length * p) - 1, 0, sorted.Length - 1)];
            var result = new QaRunResult { success = success, error = error, snapshotRoundTrip = roundTrip, mode = scenario?.mode,
                scene = SceneManager.GetActiveScene().name, unity = Application.unityVersion, device = SystemInfo.graphicsDeviceName,
                captureMethod = "URP StandardRequest; live cameras and camera-projected UI", timingKind = "player-loop samples; static QA, not final gameplay benchmark",
                utc = DateTime.UtcNow.ToString("O"), frames = times.Count, averageMs = times.Count == 0 ? 0 : times.Average(),
                p95Ms = Percent(.95f), p99Ms = Percent(.99f), allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), captures = captures.ToArray() };
            File.WriteAllText(Path.Combine(QaEnvironment.Root, "run-result.json"), JsonUtility.ToJson(result, true));
            Debug.Log("[UKUBOLA QA] " + (success ? "PASSED" : "FAILED") + " | " + QaEnvironment.Root);
#if !UNITY_EDITOR
            Application.Quit(success ? 0 : 1);
#endif
        }
        private void OnDestroy()
        {
            if (!finished && QaEnvironment.Active) File.WriteAllText(Path.Combine(QaEnvironment.Root, "interrupted.txt"), "QA run interrupted before completion.");
        }
    }
}
