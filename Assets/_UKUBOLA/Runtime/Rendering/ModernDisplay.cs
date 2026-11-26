using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ukubola.Rendering
{
    [DefaultExecutionOrder(-150)]
    public sealed class ModernDisplay : MonoBehaviour
    {
        [SerializeField] private RetroDisplay display;
        [SerializeField] private PresentationProfile profile;
        private RenderTexture texture;
        private RenderPipelineAsset previousPipeline, appliedPipeline;
        private Light[] lights;
        private LightShadows[] originalShadows;
        private void CacheLights()
        {
            lights=GetComponentsInScene<Light>();originalShadows=new LightShadows[lights.Length];
            for(int i=0;i<lights.Length;i++)originalShadows[i]=lights[i].shadows;
        }
        private T[] GetComponentsInScene<T>() where T:Component
        { return System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(gameObject.scene.GetRootGameObjects(),r=>r.GetComponentsInChildren<T>(true))); }
        public PresentationProfile Profile => profile;
        public int Quality { get; private set; }
        public float Aspect { get; private set; }
        public void Configure(RetroDisplay target, PresentationProfile settings) { display = target; profile = settings; }
        private void Awake() { previousPipeline=QualitySettings.renderPipeline;CacheLights();display.enabled = false; Apply(QaEnvironment.GetInt("UKUBOLA.Evolution.Quality", profile.defaultQuality), QaEnvironment.GetInt("UKUBOLA.Evolution.ClassicAspect", 0) == 1); }
        public void Apply(int quality, bool classic)
        {
            Quality = Mathf.Clamp(quality, 0, 2); Aspect = classic ? 4f / 3f : profile.aspect;
            if(profile.pipelines!=null&&profile.pipelines.Length==3&&profile.pipelines[Quality]) { appliedPipeline=profile.pipelines[Quality];QualitySettings.renderPipeline=appliedPipeline; }
            int shadowBudget=profile.shadowedLights[Quality];
            for(int i=0;i<lights.Length;i++)
            {
                bool shadow=originalShadows[i]!=LightShadows.None&&shadowBudget>0;
                lights[i].shadows=shadow?originalShadows[i]:LightShadows.None;
                if(shadow)
                {
                    shadowBudget--;
                    lights[i].GetUniversalAdditionalLightData().additionalLightsShadowResolutionTier=Quality==2?UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh:UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
                }
            }
            foreach(var ps in GetComponentsInScene<ParticleSystem>()){var main=ps.main;main.maxParticles=profile.particlesPerEmitter[Quality];}
            var size = profile.Resolution(Quality, Aspect);
            var replacement = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGBHalf) { name = "UKUBOLA square-pixel world", filterMode = FilterMode.Point, antiAliasing = 1 };
            if (!replacement.Create()) { Destroy(replacement); throw new System.InvalidOperationException("Unable to allocate presentation texture."); }
            var previous = texture; texture = replacement; display.WorldCamera.targetTexture = texture; display.Image.texture = texture;
            display.WorldCamera.aspect = Aspect; display.WorldCamera.allowHDR = true;
            var data = display.WorldCamera.GetUniversalAdditionalCameraData(); data.renderPostProcessing = Quality > 0; data.volumeLayerMask = ~0;
            foreach(var overlay in data.cameraStack)if(overlay)overlay.aspect=Aspect;
            if (previous) { previous.Release(); Destroy(previous); }
            Layout();
        }
        private void LateUpdate() => Layout();
        private void Layout()
        {
            var pixels = display.Canvas.pixelRect; if (pixels.height <= 0) return;
            var fit = RetroDisplay.Fit(pixels.width / pixels.height, Aspect); var rect = display.Image.rectTransform;
            rect.anchorMin = fit.min; rect.anchorMax = fit.max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private void OnDestroy() { if (texture) { texture.Release(); Destroy(texture); } if(appliedPipeline&&QualitySettings.renderPipeline==appliedPipeline)QualitySettings.renderPipeline=previousPipeline; }
    }
}
