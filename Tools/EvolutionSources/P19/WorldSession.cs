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
        private bool paused, finished, loading;
        public bool Paused => paused;
        public void Configure(WorldSaveController repository, WorldExit destination) { saves = repository; exit = destination; }
        private IEnumerator Start()
        {
            while (!saves.Ready) yield return null;
            input = saves.Player.GetComponent<PlayerInputReader>(); motor = saves.Player.GetComponent<PlayerMotor>();
            saves.Player.Died += OnDeath; exit.Finished += OnFinish;
            if (!QaEnvironment.Active) { screen = new MenuScreen(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")); screen.Hide(); }
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
            if (QaEnvironment.Active || !saves.Ready || loading || Keyboard.current == null) return;
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
        private void ShowPause(string status = "E / SPACE: USE   |   F5: SAVE   |   F9: LOAD")
        {
            if (screen == null) return; Pause();
            screen.Begin("THE OUTPOST", status);
            screen.Action("RESUME", 170, 165, 300, 38, Resume);
            screen.Action("SAVE GAME", 170, 213, 300, 38, () => { saves.Save(); ShowPause(saves.LastStatus); }, saves.CanSave);
            screen.Action("LOAD GAME", 170, 261, 300, 38, () => BeginLoad(false), saves.Repository.TryRead(out _, out _, out _));
            screen.Action("RESTART MISSION", 170, 309, 300, 38, () => BeginLoad(true));
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
