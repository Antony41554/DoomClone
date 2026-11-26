using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ukubola.Player;
using Ukubola.Rendering;

namespace Ukubola.EditorTools
{
    public static class UkubolaRetroSetup
    {
        internal const string Root = "Assets/_UKUBOLA";
        internal const string SourcePath = Root + "/Scenes/Tests/PlayerLab.unity";
        internal const string LabPath = Root + "/Scenes/Tests/RetroLab.unity";
        internal const string Generated = Root + "/Generated/Phase02";
        internal const string TextureDir = Root + "/Art/Textures/Phase02/";
        internal const string SpriteDir = Root + "/Art/Sprites/Phase02/";
        internal const string TargetPath = Generated + "/Retro320x200.renderTexture";
        internal const string ShaderPath = Root + "/Art/Shaders/UkubolaSpriteCutout.shader";
        internal static bool CanRun() => UkubolaPlayerSetup.CanRun();
        internal static T Require<T>(string path) where T : UnityEngine.Object => UkubolaPlayerSetup.RequireAsset<T>(path);
        internal static T[] Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static bool Exists(string path) => File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null;

        [MenuItem("Tools/UKUBOLA/Phase 02/Setup Retro Lab", false, 30)]
        public static void Setup()
        {
            if (!CanRun()) return;
            try
            {
                if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) throw new InvalidOperationException("Active render pipeline is not URP.");
                Require<SceneAsset>(SourcePath);
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (Exists(LabPath))
                {
                    var existing = OpenInternal();
                    UkubolaRetroValidator.CheckScene(existing);
                    Debug.Log("[UKUBOLA P02] Existing RetroLab preserved. VALIDATION PASSED | 0 errors.");
                    return;
                }
                if (!AssetDatabase.IsValidFolder(Generated))
                {
                    if (!AssetDatabase.IsValidFolder(Root + "/Generated")) throw new InvalidOperationException("Finish Phase 00 first.");
                    if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(Root + "/Generated", "Phase02"))) throw new IOException("Cannot create generated folder.");
                }
                // First prepare assets, before copying or editing the scene.
                var target = LoadOrCreateTarget();
                var wall = WorldMaterial("Wall", new Vector2(4, 2));
                var floor = WorldMaterial("Floor", new Vector2(8, 6));
                var accent = WorldMaterial("Accent", Vector2.one);
                var frames = Enumerable.Range(0, 8).Select(i => ImportSprite(SpriteDir + "Dummy_" + i + ".png")).ToArray();
                var beacon = ImportSprite(SpriteDir + "Beacon.png");
                var spriteShader = Require<Shader>(ShaderPath);
                if (!spriteShader.isSupported || ShaderUtil.ShaderHasError(spriteShader)) throw new InvalidOperationException("SpriteCutout shader failed compilation or is not supported. See Console.");
                var spriteMaterial = LoadOrCreateMaterial("SpriteCutout", spriteShader);
                // Open a saved copy in Single mode, exactly as the accepted Phase 01 setup.
                if (!AssetDatabase.CopyAsset(SourcePath, LabPath)) throw new IOException("Cannot copy PlayerLab to RetroLab.");
                AssetDatabase.ImportAsset(LabPath, ImportAssetOptions.ForceSynchronousImport);
                var scene = OpenInternal();
                var view = Find<PlayerView>(scene).Single();
                var world = view.PlayerCamera;
                if (!world || !world.isActiveAndEnabled || world.targetTexture != null || !world.CompareTag("MainCamera")) throw new InvalidOperationException("Expected Phase 01 player camera without target texture.");
                if (Find<Camera>(scene).Count(c => c.isActiveAndEnabled) != 1 || Find<AudioListener>(scene).Count(a => a.isActiveAndEnabled) != 1)
                    throw new InvalidOperationException("Expected one active camera and one AudioListener in PlayerLab.");
                if (Find<RetroDisplay>(scene).Length != 0 || Find<Canvas>(scene).Any(c => c.isActiveAndEnabled)) throw new InvalidOperationException("Source already contains display elements; not replacing them.");
                var context = Find<LevelContext>(scene).Single();
                var contextData = new SerializedObject(context);
                var level = contextData.FindProperty("levelId");
                if (level == null) throw new InvalidOperationException("Phase 00 LevelContext.levelId missing.");
                level.stringValue = "RETRO_LAB";
                contextData.ApplyModifiedPropertiesWithoutUndo();

                // Keep the gameplay camera and its AudioListener. Add only a screen-clearing camera.
                ConfigureCamera(world);
                world.targetTexture = target;
                world.aspect = RetroDisplay.DisplayAspect;
                world.depth = -10;
                world.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
                if (PrefabUtility.IsPartOfPrefabInstance(world)) PrefabUtility.RecordPrefabInstancePropertyModifications(world);
                var outputGo = new GameObject("Retro Output Camera");
                var output = outputGo.AddComponent<Camera>();
                ConfigureCamera(output);
                output.clearFlags = CameraClearFlags.SolidColor;
                output.backgroundColor = Color.black;
                output.cullingMask = 0;
                output.depth = 10;
                output.targetDisplay = 0;
                output.orthographic = true;
                output.useOcclusionCulling = false;
                var canvasGo = new GameObject("Retro Canvas", typeof(RectTransform), typeof(Canvas));
                canvasGo.layer = LayerMask.NameToLayer("UI");
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.targetDisplay = 0;
                canvas.sortingOrder = 0;
                var imageGo = new GameObject("Retro Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                imageGo.layer = canvasGo.layer;
                imageGo.transform.SetParent(canvasGo.transform, false);
                var image = imageGo.GetComponent<RawImage>();
                image.texture = target; image.color = Color.white; image.raycastTarget = false;
                image.rectTransform.anchorMin = new Vector2(.125f, 0f);
                image.rectTransform.anchorMax = new Vector2(.875f, 1f);
                image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
                canvasGo.AddComponent<RetroDisplay>().Configure(world, output, canvas, image, target);

                // Scene overrides only: never modify Phase 00 materials, meshes or Player prefab.
                var geometry = scene.GetRootGameObjects().Single(r => r.name == "Geometry");
                var terrain = scene.GetRootGameObjects().Single(r => r.name == "Phase01 Terrain");
                foreach (var r in geometry.GetComponentsInChildren<MeshRenderer>(true).Concat(terrain.GetComponentsInChildren<MeshRenderer>(true)))
                    r.sharedMaterial = r.name == "Floor" ? floor : r.name == "Diagnostic Block" ? accent : wall;
                AddDemo("Billboard Beacon", new Vector3(-2f, .01f, -.5f), world, spriteMaterial, new[] { beacon });
                AddDemo("Directional Dummy", new Vector3(0f, .01f, 2.5f), world, spriteMaterial, frames);
                EditorSceneManager.MarkSceneDirty(scene);
                UkubolaRetroValidator.CheckScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save RetroLab.");
                AssetDatabase.SaveAssets();
                // Reopen the saved assets to verify that prefab overrides and scene references persisted.
                EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
                UkubolaRetroValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P02] RetroLab generated. PlayerLab preserved.");
                Debug.Log("[UKUBOLA P02] VALIDATION PASSED | 0 errors.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UKUBOLA P02] Setup stopped: " + ex.Message + "\n" + ex + "\nIf a partial RetroLab exists, follow README recovery; existing scenes are never overwritten automatically.");
            }
        }

        internal static Scene OpenInternal()
        {
            Require<SceneAsset>(LabPath);
            var scene = SceneManager.GetActiveScene();
            if (scene.path != LabPath || SceneManager.sceneCount != 1) scene = EditorSceneManager.OpenScene(LabPath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.GetActiveScene() != scene) throw new InvalidOperationException("RetroLab is not active and loaded.");
            return scene;
        }
        [MenuItem("Tools/UKUBOLA/Phase 02/Open Retro Lab", false, 31)]
        public static void OpenLab()
        {
            if (!CanRun()) return;
            try { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) OpenInternal(); }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P02] " + ex.Message); }
        }
        private static void ConfigureCamera(Camera camera)
        {
            camera.allowHDR = false; camera.allowMSAA = false; camera.allowDynamicResolution = false;
            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            if (!data) data = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            data.renderType = CameraRenderType.Base;
            if (data.cameraStack != null && data.cameraStack.Count != 0) throw new InvalidOperationException("Unexpected camera stack.");
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            if (PrefabUtility.IsPartOfPrefabInstance(data)) PrefabUtility.RecordPrefabInstancePropertyModifications(data);
        }
        private static RenderTexture LoadOrCreateTarget()
        {
            if (Exists(TargetPath)) return Require<RenderTexture>(TargetPath);
            var target = new RenderTexture(RetroDisplay.Width, RetroDisplay.Height, 24, RenderTextureFormat.ARGB32)
            {
                name = "Retro320x200", antiAliasing = 1, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
                useMipMap = false, autoGenerateMips = false, useDynamicScale = false
            };
            AssetDatabase.CreateAsset(target, TargetPath);
            return Require<RenderTexture>(TargetPath);
        }
        private static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            string path = Generated + "/" + name + ".mat";
            if (Exists(path))
            {
                var existing = Require<Material>(path);
                if (existing.shader != shader) throw new InvalidOperationException("Unexpected shader in " + path);
                return existing;
            }
            var material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static Material WorldMaterial(string name, Vector2 tiling)
        {
            var texture = ImportTexture(TextureDir + name + ".png", false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader || !shader.isSupported) throw new InvalidOperationException("URP Unlit shader unavailable.");
            string path = Generated + "/" + name + ".mat";
            bool exists = Exists(path);
            var material = LoadOrCreateMaterial(name, shader);
            if (!exists)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
                material.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
            }
            else if (material.GetTexture("_BaseMap") != texture) throw new InvalidOperationException("Unexpected texture in " + path);
            return material;
        }
        private static Texture2D ImportTexture(string path, bool sprite)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Missing Phase 02 PNG", path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Texture importer missing: " + path);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 128;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = !sprite;
            importer.wrapMode = sprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.alphaSource = sprite ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = sprite;
            if (sprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(.5f, 0f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
            importer.SaveAndReimport();
            return Require<Texture2D>(path);
        }
        private static Sprite ImportSprite(string path)
        {
            ImportTexture(path, true);
            return Require<Sprite>(path);
        }
        private static void AddDemo(string name, Vector3 position, Camera viewer, Material material, Sprite[] frames)
        {
            var actor = new GameObject(name);
            actor.transform.position = position;
            actor.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(actor.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            renderer.sprite = frames[0];
            var billboard = visual.AddComponent<Billboard>();
            billboard.Configure(viewer);
            billboard.FaceViewer();
            if (frames.Length == 8)
            {
                var directional = visual.AddComponent<DirectionalSprite>();
                directional.Configure(actor.transform, viewer, renderer, frames);
                directional.UpdateFrame();
            }
        }
    }
}
