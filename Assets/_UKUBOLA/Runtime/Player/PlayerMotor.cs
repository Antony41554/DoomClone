using UnityEngine;

namespace Ukubola.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader), typeof(PlayerView))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        private CharacterController controller;
        private PlayerInputReader input;
        private PlayerView view;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        public PlayerConfig Config => config;
        // Runtime-only setting; the shared PlayerConfig asset is never changed.
        public float SensitivityMultiplier { get; set; } = 1f;
        public Vector3 HorizontalVelocity => horizontalVelocity;
        public bool Grounded => controller != null && controller.isGrounded;
        public void Configure(PlayerConfig value) => config = value;

        private void Awake()
        {
            // Only component references; do not assume the scene has finished loading.
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            view = GetComponent<PlayerView>();
            if (config == null || !config.IsValid || view.PlayerCamera == null)
            {
                Debug.LogError("[UKUBOLA] Invalid player configuration or missing camera.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void Update()
        {
            if (!input.IsReady || !controller.enabled) return;
            bool controlled = input.PollCapture();
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 displacement = Vector3.zero;
            if (controlled)
            {
                view.ApplyLook(input.Look, config.MouseSensitivity * Mathf.Clamp(SensitivityMultiplier, .5f, 2f));
                Vector2 axes = Vector2.ClampMagnitude(input.Move, 1f);
                float speed = input.Run ? config.RunSpeed : config.WalkSpeed;
                Vector3 target = (transform.right * axes.x + transform.forward * axes.y) * speed;
                float rate = axes.sqrMagnitude > 0.0001f ? config.Acceleration : config.Friction;
                displacement = IntegrateHorizontal(ref horizontalVelocity, target, rate, dt);
            }
            else horizontalVelocity = Vector3.zero;

            // CharacterController.Move does not apply gravity automatically.
            if (controller.isGrounded && verticalVelocity <= 0f) verticalVelocity = -2f;
            else verticalVelocity = Mathf.Max(-config.MaxFallSpeed, verticalVelocity - config.Gravity * dt);
            displacement.y = verticalVelocity * dt;
            CollisionFlags flags = controller.Move(displacement);
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f) verticalVelocity = -2f;
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        }

        // Exact integral for linear acceleration toward a constant velocity target during dt.
        // Used by both the live motor and the editor numerical self-tests.
        public static Vector3 IntegrateHorizontal(ref Vector3 velocity, Vector3 target, float rate, float dt)
        {
            if (dt <= 0f) return Vector3.zero;
            if (rate <= 0f) return velocity * dt;
            Vector3 difference = target - velocity;
            float distance = difference.magnitude;
            if (distance <= 0.000001f) { velocity = target; return target * dt; }
            float transition = Mathf.Min(dt, distance / rate);
            Vector3 next = Vector3.MoveTowards(velocity, target, rate * transition);
            Vector3 displacement = (velocity + next) * (0.5f * transition) + next * (dt - transition);
            velocity = next;
            return displacement;
        }
    }
}
