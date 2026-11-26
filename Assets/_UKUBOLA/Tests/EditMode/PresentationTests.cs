using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Ukubola.EditorTools;
using Ukubola.Rendering;

namespace Ukubola.Tests
{
    public sealed class PresentationTests
    {
        [Test] public void QualityProfilesKeepSquarePixelsAtBothAspects()
        {
            var profile=ScriptableObject.CreateInstance<PresentationProfile>();
            try
            {
                foreach(float aspect in new[]{16f/9,4f/3})for(int q=0;q<3;q++)
                {
                    var size=profile.Resolution(q,aspect);
                    Assert.That(size.y,Is.EqualTo(new[]{270,360,540}[q]));
                    Assert.That(Mathf.Abs(size.x-size.y*aspect),Is.LessThanOrEqualTo(.5f));
                }
            }
            finally{Object.DestroyImmediate(profile);}
        }
        [Test] public void PbrMapsUseCorrectColorSpacesAndMipmaps()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{UkubolaEvolutionArt.Textures}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(path.EndsWith("_Normal.png")){Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.NormalMap),path);Assert.That(importer.sRGBTexture,Is.False,path);}
                else if(path.EndsWith("_Mask.png"))Assert.That(importer.sRGBTexture,Is.False,path);
                else Assert.That(importer.sRGBTexture,Is.True,path);
                Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point),path);
                if(path.EndsWith("Digits.png")||path.EndsWith("HudPanel.png"))Assert.That(importer.mipmapEnabled,Is.False,path);
            }
        }
        [Test] public void LuminairesAndVisorsRemainEmissiveAfterImport()
        {
            foreach(string name in new[]{"CyanLight","AmberLight","CyanVisor","AmberVisor"})
            {
                var material=UkubolaEvolutionArt.Asset<Material>(UkubolaEvolutionArt.Root+"/Materials/"+name+".mat");
                Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.True,name);
                Assert.That(material.globalIlluminationFlags&MaterialGlobalIlluminationFlags.AnyEmissive,Is.Not.EqualTo(0),name);
                Assert.That(material.GetColor("_EmissionColor").maxColorComponent,Is.GreaterThan(1),name);
            }
        }
        [Test] public void IndustrialSceneHasBakedLightingAndCompleteCharacterViews()
        {
            var scene=EditorSceneManager.OpenPreviewScene(UkubolaEvolutionScene.RoomPath);
            try
            {
                var roots=scene.GetRootGameObjects();
                var views=roots.SelectMany(r=>r.GetComponentsInChildren<EnemyModelView>(true)).ToArray();
                Assert.That(views.Length,Is.EqualTo(2));Assert.That(views.All(v=>v.IsValid),Is.True);
                var probe=roots.SelectMany(r=>r.GetComponentsInChildren<ReflectionProbe>(true)).Single();
                Assert.That(probe.mode,Is.EqualTo(ReflectionProbeMode.Baked));Assert.That(probe.bakedTexture,Is.Not.Null);
                Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<ModernDisplay>(true)).Count(),Is.EqualTo(1));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void QualityRenderersOwnTheirFeaturesWithoutChangingLegacyRenderer()
        {
            var profile=UkubolaEvolutionArt.Asset<PresentationProfile>(UkubolaEvolutionArt.Root+"/Presentation.asset");
            Assert.That(profile.pipelines.Length,Is.EqualTo(3));Assert.That(profile.pipelines.Distinct().Count(),Is.EqualTo(3));
            for(int q=0;q<3;q++)
            {
                var serialized=new SerializedObject(profile.pipelines[q]);
                var renderer=(UniversalRendererData)serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;
                Assert.That(AssetDatabase.GetAssetPath(renderer),Does.StartWith(UkubolaEvolutionArt.Root));
                foreach(var feature in renderer.rendererFeatures)
                {
                    Assert.That(AssetDatabase.GetAssetPath(feature),Is.EqualTo(AssetDatabase.GetAssetPath(renderer)));
                    Assert.That(feature.isActive,Is.EqualTo(q>0));
                }
                Assert.That(profile.pipelines[q].msaaSampleCount,Is.EqualTo(1));
            }
            var legacy=UkubolaEvolutionArt.Asset<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            Assert.That(legacy.rendererFeatures.All(f=>f.isActive),Is.True);
        }
    }
}
