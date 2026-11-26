using UnityEngine;
using Ukubola.Combat;

namespace Ukubola.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(Armor), typeof(PlayerInventory))]
    [RequireComponent(typeof(PlayerMotor), typeof(PlayerInputReader), typeof(CharacterController))]
    public sealed class PlayerLife : MonoBehaviour
    {
        private Health health;
        private PlayerMotor motor;
        private PlayerInputReader input;
        private CharacterController controller;
        private bool suppressed, motorWasEnabled, inputWasEnabled, controllerWasEnabled;
        public Health Health => GetComponent<Health>();
        private void Awake()
        {
            health = GetComponent<Health>(); motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputReader>(); controller = GetComponent<CharacterController>();
        }
        private void OnEnable()
        {
            health.Died += OnDeath; health.Revived += OnRevived;
            if (health.IsDead) StopPlayer(); else RestorePlayer();
        }
        private void OnDisable()
        {
            if (health) { health.Died -= OnDeath; health.Revived -= OnRevived; }
        }
        private void OnDeath(Health _, DamageInfo damage) => StopPlayer();
        private void OnRevived(Health _) => RestorePlayer();
        private void StopPlayer()
        {
            if (suppressed) return;
            suppressed = true; motorWasEnabled = motor.enabled; inputWasEnabled = input.enabled; controllerWasEnabled = controller.enabled;
            motor.enabled = false; input.enabled = false; controller.enabled = false;
            // Disabling PlayerInputReader releases the captured cursor through its own OnDisable.
        }
        private void RestorePlayer()
        {
            if (!suppressed) return;
            suppressed = false; controller.enabled = controllerWasEnabled; input.enabled = inputWasEnabled; motor.enabled = motorWasEnabled;
        }
    }
}
