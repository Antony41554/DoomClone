using UnityEngine;

namespace Ukubola.Player
{
    public sealed class PlayerConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 5f;
        [SerializeField, Min(0.1f)] private float runSpeed = 8f;
        [SerializeField, Min(0.1f)] private float acceleration = 28f;
        [SerializeField, Min(0.1f)] private float friction = 24f;
        [SerializeField, Min(0.1f)] private float gravity = 24f;
        [SerializeField, Min(0.1f)] private float maxFallSpeed = 35f;
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float Acceleration => acceleration;
        public float Friction => friction;
        public float Gravity => gravity;
        public float MaxFallSpeed => maxFallSpeed;
        public float MouseSensitivity => mouseSensitivity;

        private static bool Positive(float v) => v > 0f && !float.IsInfinity(v);
        public bool IsValid => Positive(walkSpeed) && Positive(runSpeed) && runSpeed >= walkSpeed &&
            Positive(acceleration) && Positive(friction) && Positive(gravity) &&
            Positive(maxFallSpeed) && Positive(mouseSensitivity);
    }
}
