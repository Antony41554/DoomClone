using UnityEngine;
using UnityEngine.UI;

namespace Ukubola.Rendering
{
    [DisallowMultipleComponent]
    public sealed class RetroDisplay : MonoBehaviour
    {
        public const int Width = 320;
        public const int Height = 200;
        public const float DisplayAspect = 4f / 3f;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera outputCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RawImage image;
        [SerializeField] private RenderTexture target;
        public Camera WorldCamera => worldCamera;
        public Camera OutputCamera => outputCamera;
        public Canvas Canvas => canvas;
        public RawImage Image => image;
        public RenderTexture Target => target;

        public void Configure(Camera world, Camera output, Canvas screen, RawImage view, RenderTexture texture)
        {
            worldCamera = world; outputCamera = output; canvas = screen; image = view; target = texture;
        }

        private void OnEnable()
        {
            if (!worldCamera || !outputCamera || !canvas || !image || !target || worldCamera == outputCamera ||
                worldCamera.targetTexture != target || image.texture != target || target.width != Width || target.height != Height)
            {
                Debug.LogError("[UKUBOLA P02] Invalid retro display references or target. Run Validate Retro Lab.", this);
                enabled = false;
                return;
            }
            // Project for the physical 4:3 display rather than the 8:5 texture.
            // The texture is stretched only within that 4:3 area; its pixels are not square.
            worldCamera.aspect = DisplayAspect;
            UpdateLayout();
        }

        private void LateUpdate() => UpdateLayout();

        private void UpdateLayout()
        {
            Rect pixels = canvas.pixelRect;
            if (pixels.width <= 0f || pixels.height <= 0f) return;
            Rect area = Fit(pixels.width / pixels.height, DisplayAspect);
            RectTransform rect = image.rectTransform;
            Vector2 min = area.min, max = area.max;
            if (rect.anchorMin != min || rect.anchorMax != max || rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero)
            {
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        // Unit rectangle in screen coordinates, centered with no cropping.
        public static Rect Fit(float screenAspect, float contentAspect)
        {
            if (screenAspect <= 0f || contentAspect <= 0f || float.IsNaN(screenAspect) || float.IsNaN(contentAspect) ||
                float.IsInfinity(screenAspect) || float.IsInfinity(contentAspect)) return new Rect(0f, 0f, 1f, 1f);
            float w = Mathf.Min(1f, contentAspect / screenAspect);
            float h = Mathf.Min(1f, screenAspect / contentAspect);
            return new Rect((1f - w) * .5f, (1f - h) * .5f, w, h);
        }
    }
}
