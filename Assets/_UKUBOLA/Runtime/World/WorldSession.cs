using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.UI;

namespace Ukubola.World
{
    public sealed class WorldSession : MonoBehaviour
    {
        [SerializeField] private WorldSaveController saves;
        [SerializeField] private WorldExit exit;
        private MenuScreen screen;
        private PlayerInputReader input;
        private PlayerMotor motor;
        private Ukubola.Rendering.ModernDisplay display;
        private Camera worldCamera;
        private bool paused, finished, loading;
        private bool AutomatedQa=>QaEnvironment.Active&&QaEnvironment.Argument("-ukubola-qa-mode")!="manual";
        public bool Paused => paused;
        public void Configure(WorldSaveController repository, WorldExit destination) { saves = repository; exit = destination; }
        private void Awake(){if(!AutomatedQa){paused=true;Time.timeScale=0;}}
        private IEnumerator Start()
        {
            while (!saves.Ready) yield return null;
            input = saves.Player.GetComponent<PlayerInputReader>(); motor = saves.Player.GetComponent<PlayerMotor>();
            display=FindAnyObjectByType<Ukubola.Rendering.ModernDisplay>();worldCamera=saves.Player.GetComponent<PlayerView>().PlayerCamera;
            motor.SensitivityMultiplier=Mathf.Clamp(QaEnvironment.GetFloat("UKUBOLA.Evolution.Sensitivity",1),.5f,2);
            worldCamera.fieldOfView=Mathf.Clamp(QaEnvironment.GetFloat("UKUBOLA.Evolution.Fov",worldCamera.fieldOfView),60,110);
            saves.Player.Died += OnDeath; exit.Finished += OnFinish;
            if (!AutomatedQa) { screen = new MenuScreen(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")); ShowIntro(); }
        }
        private void OnDestroy()
        {
            if (saves && saves.Player) saves.Player.Died -= OnDeath;
            if (exit) exit.Finished -= OnFinish;
            screen?.Dispose();
            if (paused) Time.timeScale = 1;
        }
        private void Update()
        {
            if (AutomatedQa || !saves.Ready || loading || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame && !saves.Player.IsDead && !finished)
            { if (paused) Resume(); else ShowPause(); }
            if (!paused && Keyboard.current.f5Key.wasPressedThisFrame) saves.Save();
            if (Keyboard.current.f9Key.wasPressedThisFrame && !finished) BeginLoad(false);
        }
        private void Pause()
        {
            paused = true; Time.timeScale = 0;
            input.enabled = false; motor.enabled = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void Resume()
        {
            if (loading || saves.Player.IsDead || finished) return;
            screen?.Hide(); paused = false; Time.timeScale = 1;
            motor.enabled = true; input.enabled = true;
        }
        private void ShowIntro()
        {
            Pause();screen.Begin("THE OUTPOST","EVOLUTION PREVIEW");
            screen.Label("Restore power. Drain the coolant depot.\nSecure the control room and reach the exit.\n\nWASD MOVE   /   MOUSE AIM   /   CLICK FIRE\nE OR SPACE USE   /   2-3 WEAPONS",55,155,530,128,17);
            screen.Action("ENTER THE OUTPOST",170,305,300,40,Resume);
            screen.Action("DISPLAY AND CONTROLS",170,355,300,36,()=>ShowDisplay(true));
            screen.Label("F5 SAVE   /   F9 LOAD   /   ESC PAUSE",80,404,480,24,14);screen.Finish();
        }
        private void ApplyDisplay(int quality,bool classic,bool intro)
        {
            display.Apply(quality,classic);QaEnvironment.SetInt("UKUBOLA.Evolution.Quality",quality);QaEnvironment.SetInt("UKUBOLA.Evolution.ClassicAspect",classic?1:0);QaEnvironment.SavePreferences();ShowDisplay(intro);
        }
        private void ShowDisplay(bool intro)
        {
            Pause();screen.Begin("DISPLAY AND CONTROLS","Changes apply immediately and are remembered.");
            screen.Label("WORLD RESOLUTION: "+display.Profile.internalHeights[display.Quality]+"p",80,143,480,24,16);
            for(int i=0;i<3;i++){int quality=i;screen.Action(new[]{"LOW","MEDIUM","HIGH"}[i],70+i*175,175,150,36,()=>ApplyDisplay(quality,display.Aspect<1.5f,intro));}
            screen.Action("ASPECT: "+(display.Aspect<1.5f?"4:3":"16:9"),170,223,300,36,()=>ApplyDisplay(display.Quality,display.Aspect>=1.5f,intro));
            void Sensitivity(float change){motor.SensitivityMultiplier=Mathf.Clamp(motor.SensitivityMultiplier+change,.5f,2);QaEnvironment.SetFloat("UKUBOLA.Evolution.Sensitivity",motor.SensitivityMultiplier);QaEnvironment.SavePreferences();ShowDisplay(intro);}
            void Fov(float change){worldCamera.fieldOfView=Mathf.Clamp(worldCamera.fieldOfView+change,60,110);QaEnvironment.SetFloat("UKUBOLA.Evolution.Fov",worldCamera.fieldOfView);QaEnvironment.SavePreferences();ShowDisplay(intro);}
            screen.Action("SENS -",70,272,150,34,()=>Sensitivity(-.1f));screen.Label(motor.SensitivityMultiplier.ToString("F1"),245,275,150,28,20);screen.Action("SENS +",420,272,150,34,()=>Sensitivity(.1f));
            screen.Action("FOV -",70,315,150,34,()=>Fov(-5));screen.Label(Mathf.RoundToInt(worldCamera.fieldOfView).ToString(),245,318,150,28,20);screen.Action("FOV +",420,315,150,34,()=>Fov(5));
            screen.Action("BACK",170,375,300,36,()=>{if(intro)ShowIntro();else ShowPause();});screen.Finish();
        }
        private void ShowPause(string status = "E / SPACE: USE   |   F5: SAVE   |   F9: LOAD")
        {
            if (screen == null) return; Pause();
            screen.Begin("THE OUTPOST", status);
            screen.Action("RESUME", 170, 165, 300, 38, Resume);
            screen.Action("SAVE GAME", 170, 213, 300, 38, () => { saves.Save(); ShowPause(saves.LastStatus); }, saves.CanSave);
            screen.Action("LOAD GAME", 170, 261, 300, 38, () => BeginLoad(false), saves.Repository.TryRead(out _, out _, out _));
            screen.Action("RESTART MISSION", 170, 309, 300, 38, () => BeginLoad(true));
            screen.Action("DISPLAY AND CONTROLS",170,357,300,34,()=>ShowDisplay(false));
            screen.Finish();
        }
        private void OnDeath(Health actor, DamageInfo damage)
        {
            if (screen == null) return; Pause(); screen.Begin("YOU DIED", "The installation is still waiting.");
            screen.Action("LOAD GAME", 170, 190, 300, 42, () => BeginLoad(false), saves.Repository.TryRead(out _, out _, out _));
            screen.Action("RESTART MISSION", 170, 247, 300, 42, () => BeginLoad(true)); screen.Finish();
        }
        private void OnFinish()
        {
            if (screen == null) return; finished = true; Pause();
            int dead = FindObjectsByType<WorldActorState>(FindObjectsSortMode.None).Count(a => a.Health.IsDead);
            int secrets = FindObjectsByType<WorldSecret>(FindObjectsSortMode.None).Count(s => s.Found);
            screen.Begin("ACCESS RESTORED", "Power restored. Coolant drained. Transfer route secured.");
            screen.Label("KILLS  " + dead + "     SECRETS  " + secrets, 80, 178, 480, 48, 24);
            screen.Action("PLAY AGAIN", 170, 260, 300, 42, () => BeginLoad(true)); screen.Finish();
        }
        private void BeginLoad(bool restart)
        {
            if (loading) return; StartCoroutine(Load(restart));
        }
        private IEnumerator Load(bool restart)
        {
            loading = true; Pause();
            screen.Begin("RESTORING", "Please wait");
            bool success = false; string message = "LOAD FAILED";
            Action<bool, string> complete = (ok, status) => { success = ok; message = status; };
            if (restart) yield return saves.Restart(complete); else yield return saves.LoadStored(complete);
            loading = false;
            if (success) { finished = false; Resume(); }
            else if (saves.Player.IsDead) { OnDeath(saves.Player, default); screen.Label(message, 40, 320, 560, 60, 16); }
            else ShowPause(message);
        }
    }
}
