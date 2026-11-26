using UnityEngine;

namespace Ukubola.Rendering
{
    // Attach only to the visual child; never rotate the actor's logical root.
    [DisallowMultipleComponent]
    public sealed class Billboard : MonoBehaviour
    {
        [SerializeField] private Camera viewer;
        public Camera Viewer => viewer;
        public void Configure(Camera camera) => viewer = camera;
        private void LateUpdate() => FaceViewer();
        public void FaceViewer()
        {
            if (!viewer) return;
            Vector3 toCamera = viewer.transform.position - transform.position;
            transform.rotation = FacingRotation(toCamera, transform.rotation);
        }
        public static Quaternion FacingRotation(Vector3 toCamera, Quaternion fallback)
        {
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.000001f) return fallback;
            // A standard sprite is seen from local -Z. Its local +X is image-right.
            return Quaternion.LookRotation(-toCamera, Vector3.up);
        }
    }
}
