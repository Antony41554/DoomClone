using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;
using Object = UnityEngine.Object;

namespace Ukubola.EditorTools
{
    public static class UkubolaCombatSetup
    {
        internal const string Root = UkubolaLevelSetup.Root;
        internal const string Source = UkubolaSurvivalSetup.Lab;
        internal const string Lab = Root + "/Scenes/Tests/CombatLab.unity";
        internal const string Data = Root + "/Data/Weapons";
        internal const string Generated = Root + "/Generated/Phase05";
        internal const string Art = Root + "/Art/Sprites/Phase05";
        internal const string ProjectilePath = Generated + "/DiagnosticProjectile.prefab";
        internal const string LevelId = "COMBAT_LAB";
        internal static bool CanRun() => UkubolaSurvivalSetup.CanRun();
        internal static T[] Find<T>(Scene s) where T : Component => UkubolaSurvivalSetup.Find<T>(s);
        internal static T Require<T>(string path) where T : Object => UkubolaLevelSetup.Require<T>(path);
        internal static bool Exists(string path) => UkubolaLevelSetup.Exists(path);
        internal static Scene OpenInternal()
        {
            Require<SceneAsset>(Lab);
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Lab || SceneManager.sceneCount != 1) scene = EditorSceneManager.OpenScene(Lab, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.GetActiveScene() != scene) throw new InvalidOperationException("CombatLab not active/loaded.");
            return scene;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            if (!AssetDatabase.IsValidFolder(path.Substring(0, slash)) || string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash+1)))) throw new IOException("Cannot create " + path);
        }
        private static Texture2D Texture(string name)
        {
            string path = Art + "/" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing texture importer: " + path);
            if (importer.textureType != TextureImporterType.Default || importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp || importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.textureType = TextureImporterType.Default; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            var texture = Require<Texture2D>(path);
            if (texture.width != 96 || texture.height != 64 || texture.filterMode != FilterMode.Point) throw new InvalidOperationException("Unexpected weapon texture size/filter: " + path);
            return texture;
        }
        private static WeaponDefinition Definition(string name, WeaponId id, AmmoType ammo, int pellets, float interval, float spread)
        {
            string path = Data + "/" + name + ".asset";
            if (Exists(path)) return Require<WeaponDefinition>(path);
            // All four textures have already been imported before creating definitions.
            var ready = Require<Texture2D>(Art + "/" + name + "_Idle.png");
            var flash = Require<Texture2D>(Art + "/" + name + "_Fire.png");
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.Configure(id, ammo, pellets, interval, spread, ready, flash);
            AssetDatabase.CreateAsset(def, path);
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssetIfDirty(def);
            return Require<WeaponDefinition>(path);
        }
        private static Projectile ProjectileAsset(int layer)
        {
            if (Exists(ProjectilePath)) return Require<GameObject>(ProjectilePath).GetComponent<Projectile>();
            var path = Generated + "/Projectile.mat";
            Material mat;
            if (Exists(path)) mat = Require<Material>(path);
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (!shader || !shader.isSupported) throw new InvalidOperationException("URP Unlit unavailable.");
                mat = new Material(shader); mat.SetColor("_BaseColor", new Color(1f,.68f,.13f)); AssetDatabase.CreateAsset(mat, path);
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                go.name = "DiagnosticProjectile"; go.layer = layer; go.transform.localScale = Vector3.one * .16f;
                Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mat;
                go.AddComponent<Projectile>();
                var saved = PrefabUtility.SaveAsPrefabAsset(go, ProjectilePath);
                if (!saved) throw new IOException("Could not save diagnostic projectile prefab.");
                return saved.GetComponent<Projectile>();
            }
            finally { Object.DestroyImmediate(go); }
        }
        [MenuItem("Tools/UKUBOLA/Phase 05/Setup Combat Lab", false, 60)]
        public static void Setup()
        {
            if (!CanRun()) return;
            bool created = false;
            try
            {
                Require<SceneAsset>(Source);
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (Exists(Lab)) { UkubolaCombatValidator.CheckScene(OpenInternal()); Debug.Log("[UKUBOLA P05] Existing CombatLab preserved. VALIDATION PASSED | 0 errors."); return; }
                var source = EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
                UkubolaSurvivalValidator.CheckScene(source);
                int world = LayerMask.NameToLayer("World"), enemy = LayerMask.NameToLayer("Enemy"), playerLayer = LayerMask.NameToLayer("Player"), projectileLayer = LayerMask.NameToLayer("Projectile"), interact = LayerMask.NameToLayer("Interactable");
                if (world < 0 || enemy < 0 || playerLayer < 0 || projectileLayer < 0 || interact < 0) throw new InvalidOperationException("Missing required layers from Phase 00.");
                int mask = (1 << world) | (1 << enemy) | (1 << interact) | (1 << playerLayer) | 1;
                Folder(Data); Folder(Generated);
                // Complete texture reimports first; avoid holding asset handles across scene switches.
                foreach (string name in new[] { "Pistol_Idle", "Pistol_Fire", "Shotgun_Idle", "Shotgun_Fire" }) Texture(name);
                Definition("Pistol", WeaponId.Pistol, AmmoType.Bullets, 1, .4f, 0f);
                Definition("Shotgun", WeaponId.Shotgun, AmmoType.Shells, 7, .85f, 5.5f);
                AssetDatabase.SaveAssets();
                UkubolaCombatValidator.CheckDefinition(Require<WeaponDefinition>(Data + "/Pistol.asset"), WeaponId.Pistol, AmmoType.Bullets, 1);
                UkubolaCombatValidator.CheckDefinition(Require<WeaponDefinition>(Data + "/Shotgun.asset"), WeaponId.Shotgun, AmmoType.Shells, 7);
                if ((Find<RetroDisplay>(source).Single().WorldCamera.cullingMask & (1 << projectileLayer)) == 0) throw new InvalidOperationException("World camera excludes Projectile layer.");
                if (!AssetDatabase.CopyAsset(Source, Lab)) throw new IOException("Cannot copy SurvivalLab.");
                created = true; AssetDatabase.ImportAsset(Lab, ImportAssetOptions.ForceSynchronousImport);
                var scene = OpenInternal();
                var projectile = ProjectileAsset(projectileLayer);
                if (!projectile) throw new InvalidOperationException("Invalid projectile asset.");
                // Opening a scene or importing a prefab can unload asset instances that were
                // only referenced by local variables. Reload persistent references afterwards.
                var pistol = Require<WeaponDefinition>(Data + "/Pistol.asset");
                var shotgun = Require<WeaponDefinition>(Data + "/Shotgun.asset");
                UkubolaCombatValidator.CheckDefinition(pistol, WeaponId.Pistol, AmmoType.Bullets, 1);
                UkubolaCombatValidator.CheckDefinition(shotgun, WeaponId.Shotgun, AmmoType.Shells, 7);
                var old = Find<SurvivalDiagnostics>(scene).Single();
                var hp = old.Player; var bag = old.Inventory; var target = old.Target; var visual = old.TargetVisual;
                var display = Find<RetroDisplay>(scene).Single();
                Object.DestroyImmediate(old.Readout.transform.parent.gameObject); Object.DestroyImmediate(old.gameObject);
                var context = Find<LevelContext>(scene).Single(); var so = new SerializedObject(context); var prop = so.FindProperty("levelId");
                if (prop == null) throw new InvalidOperationException("Missing LevelContext.levelId"); prop.stringValue = LevelId; so.ApplyModifiedPropertiesWithoutUndo();
                var viewport = new GameObject("Weapon Viewport", typeof(RectTransform), typeof(RectMask2D));
                viewport.transform.SetParent(display.Image.transform, false); Stretch(viewport.GetComponent<RectTransform>());
                var weaponGo = new GameObject("Weapon Sprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                weaponGo.transform.SetParent(viewport.transform, false);
                var r = weaponGo.GetComponent<RectTransform>(); r.anchorMin = new Vector2(.25f,0f); r.anchorMax = new Vector2(.75f,.53f); r.offsetMin = r.offsetMax = Vector2.zero;
                var image = weaponGo.GetComponent<RawImage>(); image.texture = pistol.Idle; image.raycastTarget = false;
                var view = viewport.AddComponent<WeaponView>(); view.Configure(image);
                var controller = Undo.AddComponent<WeaponController>(hp.gameObject); controller.Configure(pistol, shotgun, display.WorldCamera, view, mask, 1 << enemy);
                EditorUtility.SetDirty(controller); if (PrefabUtility.IsPartOfPrefabInstance(controller)) PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
                var panel = new GameObject("Phase05 Diagnostic Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); panel.transform.SetParent(display.Image.transform, false);
                var pr = panel.GetComponent<RectTransform>(); pr.anchorMin = new Vector2(.015f,.73f); pr.anchorMax = new Vector2(.77f,.985f); pr.offsetMin = pr.offsetMax = Vector2.zero;
                var bg = panel.GetComponent<Image>(); bg.color = new Color(.035f,.045f,.055f,.9f); bg.raycastTarget = false;
                var textGo = new GameObject("Readout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); textGo.transform.SetParent(panel.transform, false);
                var tr = textGo.GetComponent<RectTransform>(); Stretch(tr); tr.offsetMin = new Vector2(4,3); tr.offsetMax = new Vector2(-4,-3);
                var text = textGo.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 14; text.resizeTextForBestFit = true; text.resizeTextMinSize = 7; text.resizeTextMaxSize = 22; text.alignment = TextAnchor.UpperLeft; text.color = Color.white; text.raycastTarget = false; text.text = "COMBAT LAB - Press Play";
                new GameObject("Phase05 Diagnostics").AddComponent<CombatDiagnostics>().Configure(controller, hp, target, bag, visual, text, projectile);
                EditorSceneManager.MarkSceneDirty(scene); UkubolaCombatValidator.CheckScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save CombatLab.");
                AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(Source, OpenSceneMode.Single); UkubolaCombatValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P05] CombatLab generated. SurvivalLab preserved."); Debug.Log("[UKUBOLA P05] VALIDATION PASSED | 0 errors.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P05] Setup stopped: " + ex.Message + "\n" + ex + (created ? "\nCombatLab may be incomplete; see README recovery." : "")); }
        }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        [MenuItem("Tools/UKUBOLA/Phase 05/Open Combat Lab", false, 61)]
        public static void Open()
        {
            if (!CanRun()) return;
            try { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) OpenInternal(); }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P05] " + ex.Message); }
        }
    }
}
