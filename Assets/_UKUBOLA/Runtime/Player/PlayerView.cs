using UnityEngine;

namespace Ukubola.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        public Camera PlayerCamera => playerCamera;
        public void Configure(Camera camera) => playerCamera = camera;

        // Classic mode: only yaw. Mouse delta is already a per-update displacement.
        public void ApplyLook(Vector2 mouseDelta, float sensitivity)
        {
            transform.Rotate(0f, mouseDelta.x * sensitivity, 0f, Space.World);
        }
    }
}
