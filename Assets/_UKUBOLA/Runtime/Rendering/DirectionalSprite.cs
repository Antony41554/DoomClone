using UnityEngine;

namespace Ukubola.Rendering
{
    [DisallowMultipleComponent]
    public sealed class DirectionalSprite : MonoBehaviour
    {
        [SerializeField] private Transform actor;
        [SerializeField] private Camera viewer;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        public Transform Actor => actor;
        public Camera Viewer => viewer;
        public SpriteRenderer Renderer => spriteRenderer;
        public Sprite[] Frames => frames;
        public int CurrentIndex { get; private set; } = -1;

        // Frames: front, front-right, right, back-right, back, back-left, left, front-left.
        public void Configure(Transform root, Camera camera, SpriteRenderer renderer, Sprite[] sprites)
        {
            actor = root; viewer = camera; spriteRenderer = renderer; frames = sprites;
        }
        private void LateUpdate() => UpdateFrame();
        public void UpdateFrame()
        {
            if (!actor || !viewer || !spriteRenderer || frames == null || frames.Length != 8) return;
            Vector3 direction = viewer.transform.position - actor.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < .000001f) return;
            Vector3 local = actor.InverseTransformDirection(direction);
            int index = IndexFromAngle(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg);
            if (!frames[index]) return;
            // No dependency on Billboard execution order: use the logical root, not visual rotation.
            if (spriteRenderer.sprite != frames[index]) spriteRenderer.sprite = frames[index];
            CurrentIndex = index;
        }
        public static int IndexFromAngle(float angle)
        {
            if (float.IsNaN(angle) || float.IsInfinity(angle)) return 0;
            return Mathf.FloorToInt(Mathf.Repeat(angle + 22.5f, 360f) / 45f) % 8;
        }
    }
}
