using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;

namespace Ukubola.EditorTools
{
    public static class UkubolaSurvivalSetup
    {
        internal const string Source = UkubolaLevelSetup.Lab;
        internal const string Lab = UkubolaLevelSetup.Root + "/Scenes/Tests/SurvivalLab.unity";
        internal const string LevelId = "SURVIVAL_LAB";
        internal static bool CanRun() => UkubolaLevelSetup.CanRun();
        internal static T[] Find<T>(Scene scene) where T : Component => UkubolaLevelSetup.Find<T>(scene);
        internal static Scene OpenInternal()
        {
            UkubolaLevelSetup.Require<SceneAsset>(Lab);
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Lab || SceneManager.sceneCount != 1) scene = EditorSceneManager.OpenScene(Lab, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.GetActiveScene() != scene) throw new InvalidOperationException("SurvivalLab is not active and loaded.");
            return scene;
        }
        [MenuItem("Tools/UKUBOLA/Phase 04/Setup Survival Lab", false, 50)]
        public static void Setup()
        {
            if (!CanRun()) return;
            bool created = false;
            try
            {
                UkubolaLevelSetup.Require<SceneAsset>(Source);
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (UkubolaLevelSetup.Exists(Lab))
                {
                    UkubolaSurvivalValidator.CheckScene(OpenInternal());
                    Debug.Log("[UKUBOLA P04] Existing SurvivalLab preserved. VALIDATION PASSED | 0 errors."); return;
                }
                var source = EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
                UkubolaLevelValidator.CheckScene(source);
                if (Find<Health>(source).Length != 0 || Find<PlayerInventory>(source).Length != 0 || Find<SurvivalDiagnostics>(source).Length != 0)
                    throw new InvalidOperationException("Source already contains survival components; cannot safely generate.");
                int enemyLayer = LayerMask.NameToLayer("Enemy"), playerLayer = LayerMask.NameToLayer("Player");
                if (enemyLayer < 0 || playerLayer < 0 || Physics.GetIgnoreLayerCollision(enemyLayer, playerLayer))
                    throw new InvalidOperationException("Enemy/Player layers must exist and collide.");
                var display = Find<RetroDisplay>(source).Single();
                if ((display.WorldCamera.cullingMask & (1 << enemyLayer)) == 0) throw new InvalidOperationException("Player camera cannot see Enemy layer.");
                var def = UkubolaLevelSetup.Require<LevelDefinition>(UkubolaLevelSetup.Data);
                if (!def.TryValidate(out string reason, out var start, out _)) throw new InvalidOperationException(reason);
                var cell = start + Vector2Int.up * 2;
                if (def.Tile(cell.x,cell.y) != '.') throw new InvalidOperationException("Default diagnostic target cell is not clear; preserve customized LevelLab.");
                var targetPosition = def.Center(cell);
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (!font) throw new InvalidOperationException("Built-in runtime font not found.");
                var material = UkubolaLevelSetup.Require<Material>(UkubolaLevelSetup.Generated + "/Exit.mat");
                if (!AssetDatabase.CopyAsset(Source, Lab)) throw new IOException("Cannot copy LevelLab.");
                created = true;
                AssetDatabase.ImportAsset(Lab, ImportAssetOptions.ForceSynchronousImport);
                var scene = OpenInternal();
                var motor = Find<PlayerMotor>(scene).Single();
                var armor = Undo.AddComponent<Armor>(motor.gameObject);
                var hp = Undo.AddComponent<Health>(motor.gameObject); hp.Configure(100, armor);
                var inventory = Undo.AddComponent<PlayerInventory>(motor.gameObject);
                Undo.AddComponent<PlayerLife>(motor.gameObject);
                EditorUtility.SetDirty(hp);
                if (PrefabUtility.IsPartOfPrefabInstance(hp)) PrefabUtility.RecordPrefabInstancePropertyModifications(hp);
                var context = Find<LevelContext>(scene).Single();
                var serialized = new SerializedObject(context);
                var id = serialized.FindProperty("levelId");
                if (id == null) throw new InvalidOperationException("LevelContext.levelId missing.");
                id.stringValue = LevelId; serialized.ApplyModifiedPropertiesWithoutUndo();
                var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "Damage Target"; target.layer = enemyLayer;
                target.transform.position = targetPosition + Vector3.up * .75f;
                target.transform.localScale = new Vector3(.8f, 1.5f, .8f);
                var visual = target.GetComponent<MeshRenderer>(); visual.sharedMaterial = material;
                var targetHealth = target.AddComponent<Health>(); targetHealth.Configure(100, null);
                target.AddComponent<EntityId>();
                var canvas = Find<RetroDisplay>(scene).Single().Canvas;
                var panel = new GameObject("Phase04 Diagnostic Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panel.transform.SetParent(canvas.transform, false);
                panel.transform.SetAsLastSibling();
                var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1);
                rect.anchoredPosition = new Vector2(8,-8); rect.sizeDelta = new Vector2(450,154);
                var background = panel.GetComponent<Image>(); background.color = new Color(.035f,.045f,.055f,.90f); background.raycastTarget = false;
                var textGo = new GameObject("Readout", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textGo.transform.SetParent(panel.transform, false);
                var tr = textGo.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(8,5); tr.offsetMax = new Vector2(-8,-5);
                var text = textGo.GetComponent<Text>(); text.font = font; text.fontSize = 14; text.color = Color.white; text.alignment = TextAnchor.UpperLeft; text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.text = "SURVIVAL LAB - Press Play";
                var diagnostics = new GameObject("Phase04 Diagnostics").AddComponent<SurvivalDiagnostics>();
                diagnostics.Configure(hp, inventory, targetHealth, visual, text);
                EditorSceneManager.MarkSceneDirty(scene);
                UkubolaSurvivalValidator.CheckScene(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save SurvivalLab.");
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
                UkubolaSurvivalValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P04] SurvivalLab generated. LevelLab preserved.");
                Debug.Log("[UKUBOLA P04] VALIDATION PASSED | 0 errors.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UKUBOLA P04] Setup stopped: " + ex.Message + "\n" + ex + (created ? "\nA new SurvivalLab may be incomplete; see README recovery." : ""));
            }
        }
        [MenuItem("Tools/UKUBOLA/Phase 04/Open Survival Lab", false, 51)]
        public static void Open()
        {
            if (!CanRun()) return;
            try { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) OpenInternal(); }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P04] " + ex.Message); }
        }
    }
}
