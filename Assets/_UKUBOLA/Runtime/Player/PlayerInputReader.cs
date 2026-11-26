using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ukubola.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset runtimeActions;
        private InputActionMap gameplay;
        private InputAction move, look, run, fire, pause;
        private bool captured;
        public InputActionAsset Actions => actions;
        public bool IsReady => gameplay != null && gameplay.enabled;
        public bool Captured => captured && Cursor.lockState == CursorLockMode.Locked;
        public Vector2 Move => Captured && IsReady ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => Captured && IsReady ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool Run => Captured && IsReady && run.IsPressed();

        public void Configure(InputActionAsset asset) => actions = asset;

        private void OnEnable()
        {
            try
            {
                if (actions == null) throw new InvalidOperationException("Missing UkubolaInput asset.");
                // A private clone prevents changes to the shared asset and future UI map.
                runtimeActions = Instantiate(actions);
                runtimeActions.Disable();
                gameplay = runtimeActions.FindActionMap("Gameplay", true);
                move = gameplay.FindAction("Move", true);
                look = gameplay.FindAction("Look", true);
                run = gameplay.FindAction("Run", true);
                fire = gameplay.FindAction("Fire", true);
                pause = gameplay.FindAction("Pause", true);
                gameplay.Enable();
            }
            catch (Exception ex)
            {
                CleanupActions();
                Debug.LogError("[UKUBOLA] Player input initialization failed: " + ex.Message, this);
            }
        }

        // Called once per Update by PlayerMotor. First capture frame ignores mouse delta.
        public bool PollCapture()
        {
            if (!IsReady || !Application.isFocused) { ReleaseCursor(); return false; }
            if (pause.WasPressedThisFrame()) { ReleaseCursor(); return false; }
            if (captured && Cursor.lockState != CursorLockMode.Locked) ReleaseCursor();
            if (!captured)
            {
                if (fire.WasPressedThisFrame())
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    captured = true;
                }
                return false;
            }
            return Captured;
        }

        private void ReleaseCursor()
        {
            if (!captured) return;
            captured = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
        private void OnApplicationPause(bool paused) { if (paused) ReleaseCursor(); }
        private void OnDisable() { ReleaseCursor(); CleanupActions(); }

        private void CleanupActions()
        {
            if (runtimeActions != null)
            {
                runtimeActions.Disable();
                Destroy(runtimeActions);
            }
            runtimeActions = null;
            gameplay = null;
            move = look = run = fire = pause = null;
        }
    }
}
