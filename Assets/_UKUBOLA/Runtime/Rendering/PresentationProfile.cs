using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Ukubola.Rendering
{
    [CreateAssetMenu(menuName = "UKUBOLA/Evolution/Presentation Profile")]
    public sealed class PresentationProfile : ScriptableObject
    {
        public int[] internalHeights = { 270, 360, 540 };
        public int defaultQuality = 1;
        public float aspect = 16f / 9f;
        public UniversalRenderPipelineAsset[] pipelines;
        public int[] shadowedLights = { 0, 2, 4 };
        public int[] particlesPerEmitter = { 12, 24, 36 };
        public Vector2Int Resolution(int quality, float ratio)
        {
            if(internalHeights==null||internalHeights.Length!=3||System.Array.Exists(internalHeights,h=>h<180||h>1080)||float.IsNaN(ratio)||float.IsInfinity(ratio)||ratio<1||ratio>3)
                throw new System.InvalidOperationException("Invalid presentation resolution settings.");
            int height = internalHeights[Mathf.Clamp(quality, 0, 2)]; return new Vector2Int(Mathf.RoundToInt(height * ratio), height);
        }
    }
}
