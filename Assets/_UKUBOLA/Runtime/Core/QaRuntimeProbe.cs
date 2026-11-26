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
        private readonly List<string> behaviorChecks = new List<string>();
        private readonly List<string> runtimeErrors = new List<string>();
        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnLog(string message,string stack,LogType type)
        {if((type==LogType.Error||type==LogType.Exception||type==LogType.Assert)&&runtimeErrors.Count<8)runtimeErrors.Add(message);}
        private QaScenario scenario;
        private SaveData loaded;
        private RenderTexture qaOutput, previousOutput;
        private Camera qaOutputCamera;
        private Canvas[] qaCanvases;
        private Camera[] previousCanvasCameras;
        private float[] previousCanvasDistances;
        private void PrepareCaptureOutput()
        {
            if(scenario.captureWidth==0&&scenario.captureHeight==0)return;
            if(scenario.captureWidth<320||scenario.captureWidth>3840||scenario.captureHeight<240||scenario.captureHeight>2160)
                throw new InvalidDataException("Invalid QA output size.");
            var retro=FindAnyObjectByType<RetroDisplay>();
            if(!retro)throw new InvalidOperationException("QA output requires the presentation cameras.");
            qaOutput=new RenderTexture(scenario.captureWidth,scenario.captureHeight,24,RenderTextureFormat.ARGB32){name="UKUBOLA QA output"};
            if(!qaOutput.Create())throw new InvalidOperationException("Cannot allocate QA output.");
            qaOutputCamera=retro.OutputCamera;previousOutput=qaOutputCamera.targetTexture;qaOutputCamera.targetTexture=qaOutput;
            qaOutputCamera.aspect=qaOutput.width/(float)qaOutput.height;
            qaCanvases=FindObjectsByType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            previousCanvasCameras=qaCanvases.Select(c=>c.worldCamera).ToArray();previousCanvasDistances=qaCanvases.Select(c=>c.planeDistance).ToArray();
            foreach(var canvas in qaCanvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=qaOutputCamera;canvas.planeDistance=1;}
            // Subsequent frames let CanvasScaler and ModernDisplay lay out at the requested
            // output size, independently of the physical monitor's window size limit.
            Canvas.ForceUpdateCanvases();
        }
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
            var modern=FindAnyObjectByType<ModernDisplay>();if(modern&&scenario.quality>=0)modern.Apply(scenario.quality,scenario.classicAspect);
            try{PrepareCaptureOutput();}catch(Exception e){valid=false;problem=e.ToString();}
            if(!valid){Complete(false,problem,roundTrip);yield break;}
            if (player) player.GetComponent<Powerup>().Grant(PowerupKind.Invulnerability, 600);
            if(player&&scenario.selectedWeapon>=0&&scenario.selectedWeapon<8)
            {
                var guns=player.GetComponent<WeaponController>();var bag=player.GetComponent<Ukubola.Player.PlayerInventory>();
                var weapon=(Ukubola.Player.WeaponId)scenario.selectedWeapon;bag.TryGrantWeapon(weapon);
                var definition=weapon==Ukubola.Player.WeaponId.Pistol?guns.Pistol:weapon==Ukubola.Player.WeaponId.Shotgun?guns.Shotgun:guns.Advanced.FirstOrDefault(w=>w.Weapon==weapon);
                if(definition){if(definition.UsesAmmo)bag.AddAmmo(definition.Ammo,20);guns.Select(definition);}
            }
            if(scenario.freezeActors)
            {
                foreach(var brain in FindObjectsByType<Ukubola.Enemies.EnemyBrain>()) { brain.enabled=false;brain.GetComponent<Ukubola.Enemies.EnemyMotor>().Stop(); }
                if(player) { player.GetComponent<Ukubola.Player.PlayerMotor>().enabled=false;player.GetComponent<Ukubola.Player.PlayerInputReader>().enabled=false; }
            }
            if(scenario.verifyArtRoom&&player)
            {
                var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=new Vector3(-7,.03f,11);body.enabled=true;
                for(int i=0;i<240&&player.transform.position.z<17;i++)
                {body.Move(new Vector3(0,-4,2.5f)*Mathf.Min(Time.unscaledDeltaTime,.04f));yield return null;}
                if(player.transform.position.z<16.8f||player.transform.position.y<2.2f){Complete(false,"Stair walk failed: "+player.transform.position,roundTrip);yield break;}
                behaviorChecks.Add("CharacterController climbed 2.4m service stairs without jumping");
                var path=new UnityEngine.AI.NavMeshPath();
                if(!UnityEngine.AI.NavMesh.CalculatePath(new Vector3(-7,0,11),new Vector3(-7,2.425f,17),UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {Complete(false,"Service stair navigation disconnected",roundTrip);yield break;}
                behaviorChecks.Add("Enemy navigation connects floor to upper walkway");
            }
            for (int i = 0; i < Mathf.Clamp(scenario.warmupFrames, 3, 300); i++) yield return null;
            for (int i = 0; i < Mathf.Clamp(scenario.sampleFrames, 1, 18000); i++)
            {
                yield return null;
                times.Add(Time.unscaledDeltaTime * 1000f);
            }
            var poses=scenario.poses;
            if(poses==null||poses.Length==0)poses=(scenario.yaw??new float[]{0}).Select(y=>new QaPose{position=player?player.transform.position:Vector3.zero,yaw=y}).ToArray();
            foreach (var pose in poses)
            {
                if(!Ukubola.World.MissionDefinition.Finite(pose.position)||!SaveData.Range(pose.yaw,0,360)) {Complete(false,"Invalid capture pose",roundTrip);yield break;}
                if (player)
                {
                    var body=player.GetComponent<CharacterController>();bool wasEnabled=body&&body.enabled;if(body)body.enabled=false;
                    player.transform.SetPositionAndRotation(pose.position,Quaternion.Euler(0,pose.yaw,0));if(body)body.enabled=wasEnabled;
                }
                yield return null; yield return new WaitForEndOfFrame();
                string path = Path.Combine(QaEnvironment.Root, "capture-" + captures.Count.ToString("00") + ".png");
                try { CaptureRenderedFrame(path,qaOutput?qaOutput.width:Screen.width,qaOutput?qaOutput.height:Screen.height); captures.Add(path); }
                catch (Exception e) { Complete(false, e.ToString(), roundTrip); yield break; }
            }
            Complete(true, null, roundTrip);
        }
        private static void CaptureRenderedFrame(string path,int width,int height)
        {
            var retro = FindAnyObjectByType<RetroDisplay>();
            if (!retro) throw new InvalidOperationException("Offscreen QA requires a configured RetroDisplay.");
            var request = new RenderPipeline.StandardRequest { destination = retro.WorldCamera.targetTexture };
            if (!RenderPipeline.SupportsRenderRequest(retro.WorldCamera, request)) throw new NotSupportedException("URP camera render requests are unavailable.");
            RenderPipeline.SubmitRenderRequest(retro.WorldCamera, request);
            var output = retro.OutputCamera;
            var canvases = FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var targets = canvases.Select(c => c.worldCamera).ToArray();
            var distances = canvases.Select(c => c.planeDistance).ToArray();
            int mask = output.cullingMask; float aspect = output.aspect;
            var previousTarget = output.targetTexture; var previousActive = RenderTexture.active;
            var destination = RenderTexture.GetTemporary(Mathf.Max(320,width),Mathf.Max(240,height),24,RenderTextureFormat.ARGB32);
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
            if(runtimeErrors.Count>0){success=false;error=(error??"")+" Runtime errors: "+string.Join(" | ",runtimeErrors);}
            var modern=FindAnyObjectByType<ModernDisplay>();var display=FindAnyObjectByType<RetroDisplay>();var world=display?display.WorldCamera.targetTexture:null;
            var sorted = times.OrderBy(x => x).ToArray();
            float Percent(float p) => sorted.Length == 0 ? 0 : sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Length * p) - 1, 0, sorted.Length - 1)];
            var result = new QaRunResult { success = success, error = error, snapshotRoundTrip = roundTrip, mode = scenario?.mode,
                scene = SceneManager.GetActiveScene().name, unity = Application.unityVersion, device = SystemInfo.graphicsDeviceName,
                outputWidth=qaOutput?qaOutput.width:Screen.width,outputHeight=qaOutput?qaOutput.height:Screen.height,windowWidth=Screen.width,windowHeight=Screen.height,internalWidth=world?world.width:0,internalHeight=world?world.height:0,quality=modern?modern.Quality:-1,
                captureMethod = "URP StandardRequest; live world and UI rendered at outputWidth/outputHeight; physical window reported separately", timingKind = "player-loop samples; static QA, not final gameplay benchmark",
                utc = DateTime.UtcNow.ToString("O"), frames = times.Count, averageMs = times.Count == 0 ? 0 : times.Average(),
                p95Ms = Percent(.95f), p99Ms = Percent(.99f), allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), captures = captures.ToArray(), behaviorChecks=behaviorChecks.ToArray() };
            File.WriteAllText(Path.Combine(QaEnvironment.Root, "run-result.json"), JsonUtility.ToJson(result, true));
            Debug.Log("[UKUBOLA QA] " + (success ? "PASSED" : "FAILED") + " | " + QaEnvironment.Root);
#if !UNITY_EDITOR
            Application.Quit(success ? 0 : 1);
#endif
        }
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if(qaCanvases!=null)for(int i=0;i<qaCanvases.Length;i++)if(qaCanvases[i]){qaCanvases[i].renderMode=RenderMode.ScreenSpaceOverlay;qaCanvases[i].worldCamera=previousCanvasCameras[i];qaCanvases[i].planeDistance=previousCanvasDistances[i];}
            if(qaOutputCamera)qaOutputCamera.targetTexture=previousOutput;
            if(qaOutput){qaOutput.Release();Destroy(qaOutput);}
            if (!finished && QaEnvironment.Active) File.WriteAllText(Path.Combine(QaEnvironment.Root, "interrupted.txt"), "QA run interrupted before completion.");
        }
    }
}
