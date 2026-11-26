using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Ukubola.Player;

namespace Ukubola.EditorTools
{
    public static class UkubolaPlayerSetup
    {
        internal const string Root = "Assets/_UKUBOLA";
        internal const string ArenaPath = Root + "/Scenes/Tests/TestArena.unity";
        internal const string LabPath = Root + "/Scenes/Tests/PlayerLab.unity";
        internal const string InputPath = Root + "/Data/Input/UkubolaInput.inputactions";
        internal const string ConfigPath = Root + "/Data/Player/PlayerConfig.asset";
        internal const string PrefabPath = Root + "/Prefabs/Player/Player.prefab";
        internal const string WallMaterialPath = Root + "/Generated/Materials/Phase00_Wall.mat";

        internal static bool CanRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogWarning("[UKUBOLA P01] Exit Play/Prefab mode and wait for compilation/import.");
                return false;
            }
            return true;
        }

        [MenuItem("Tools/UKUBOLA/Phase 01/Setup Player Lab", false, 20)]
        public static void Setup()
        {
            if (!CanRun()) return;
            try
            {
                var actions = RequireAsset<InputActionAsset>(InputPath);
                RequireInput(actions);
                RequireAsset<SceneAsset>(ArenaPath);
                var wall = RequireAsset<Material>(WallMaterialPath);
                if (LayerMask.NameToLayer("Player") < 0 || LayerMask.NameToLayer("World") < 0)
                    throw new InvalidOperationException("Player/World layers missing; finish Phase 00.");
                if (Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("World")))
                    throw new InvalidOperationException("Player and World layers must collide (Project Settings > Physics).");
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                if (Exists(LabPath))
                {
                    // Never overwrite a previously generated or user-authored lab.
                    RequireAsset<SceneAsset>(LabPath);
                    OpenLabInternal();
                    Debug.Log("[UKUBOLA P01] Existing PlayerLab preserved; validating only.");
                    UkubolaPlayerValidator.Validate();
                    return;
                }

                EnsureFolder(Root + "/Data/Player");
                EnsureFolder(Root + "/Prefabs/Player");
                var config = LoadOrCreateConfig();
                if (!AssetDatabase.CopyAsset(ArenaPath, LabPath)) throw new IOException("Cannot copy TestArena to PlayerLab.");
                AssetDatabase.ImportAsset(LabPath, ImportAssetOptions.ForceSynchronousImport);
                var scene = OpenLabInternal();
                var roots = scene.GetRootGameObjects();
                var spawn = roots.SingleOrDefault(r => r.name == "PlayerSpawn_PLACEHOLDER");
                var diagnostic = roots.SingleOrDefault(r => r.name == "Diagnostic Camera");
                var context = roots.SelectMany(r => r.GetComponentsInChildren<LevelContext>(true)).SingleOrDefault();
                if (spawn == null || diagnostic == null || diagnostic.GetComponent<Camera>() == null || context == null)
                    throw new InvalidOperationException("The Phase 00 arena does not match the expected hierarchy.");
                if (roots.Any(r => r.GetComponentInChildren<PlayerMotor>(true) != null) || roots.Any(r => r.name == "Phase01 Terrain"))
                    throw new InvalidOperationException("Source arena already contains Phase 01 objects; no automatic overwrite.");
                var activeCameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Where(c => c.isActiveAndEnabled).ToArray();
                if (activeCameras.Length != 1 || activeCameras[0].gameObject != diagnostic)
                    throw new InvalidOperationException("Expected only the original active diagnostic camera.");
                var prefab = LoadOrCreatePrefab(config, actions);
                UkubolaPlayerValidator.CheckPrefabOrThrow(prefab, config, actions);
                var player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (player == null) throw new InvalidOperationException("Player prefab instantiation failed.");
                player.name = "Player";
                player.transform.SetPositionAndRotation(spawn.transform.position + Vector3.up * 0.03f,
                    Quaternion.Euler(0f, spawn.transform.eulerAngles.y, 0f));
                diagnostic.SetActive(false); // its AudioListener is also disabled
                AddSlope(wall);
                var contextData = new SerializedObject(context);
                var levelId = contextData.FindProperty("levelId");
                if (levelId == null) throw new InvalidOperationException("LevelContext.levelId not found; check Phase 00 version.");
                levelId.stringValue = "PLAYER_LAB";
                contextData.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                UkubolaPlayerValidator.CheckSceneOrThrow(scene, config, actions);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save PlayerLab.");
                Selection.activeGameObject = player;
                Debug.Log("[UKUBOLA P01] PlayerLab generated. TestArena preserved.");
                UkubolaPlayerValidator.Validate();
            }
            catch (Exception ex)
            {
                Debug.LogError("[UKUBOLA P01] Setup stopped: " + ex.Message + "\n" + ex +
                    "\nExisting assets are not overwritten. If PlayerLab was created but incomplete, see README recovery.");
            }
        }

        [MenuItem("Tools/UKUBOLA/Phase 01/Open Player Lab", false, 21)]
        public static void OpenLab()
        {
            if (!CanRun()) return;
            try
            {
                RequireAsset<SceneAsset>(LabPath);
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                OpenLabInternal();
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P01] " + ex.Message); }
        }

        internal static Scene OpenLabInternal()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != LabPath || SceneManager.sceneCount != 1)
                scene = EditorSceneManager.OpenScene(LabPath, OpenSceneMode.Single);
            // The scene is an already saved asset opened in Single mode, not an unsaved additive scene.
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.GetActiveScene() != scene)
                throw new InvalidOperationException("PlayerLab is not the active loaded scene.");
            return scene;
        }

        internal static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Missing or incompatible asset: " + path);
            return asset;
        }

        internal static void RequireInput(InputActionAsset actions)
        {
#if !ENABLE_INPUT_SYSTEM
            throw new InvalidOperationException("New Input System backend is not enabled. Enable it in Player Settings and restart.");
#else
            if (InputSystem.settings.updateMode != InputSettings.UpdateMode.ProcessEventsInDynamicUpdate)
                throw new InvalidOperationException("Set Input System Package > Update Mode to Process Events In Dynamic Update.");
            var map = actions.FindActionMap("Gameplay", true);
            foreach (string name in new[] { "Move", "Look", "Run", "Fire", "Pause" })
            {
                var action = map.FindAction(name, true);
                if (action.bindings.Count == 0) throw new InvalidOperationException("Missing input binding: " + name);
                var expected = name == "Move" || name == "Look" ? "Vector2" : "Button";
                if (action.expectedControlType != expected) throw new InvalidOperationException("Unexpected control type for " + name + ": " + action.expectedControlType);
            }
#endif
        }

        private static bool Exists(string path) => File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null;
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            string parent = path.Substring(0, i);
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, path.Substring(i + 1))))
                throw new IOException("Cannot create folder: " + path);
        }

        private static PlayerConfig LoadOrCreateConfig()
        {
            if (Exists(ConfigPath)) return RequireAsset<PlayerConfig>(ConfigPath);
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return RequireAsset<PlayerConfig>(ConfigPath);
        }

        private static GameObject LoadOrCreatePrefab(PlayerConfig config, InputActionAsset actions)
        {
            if (!config.IsValid) throw new InvalidOperationException("Invalid PlayerConfig; correct the asset before generating.");
            if (Exists(PrefabPath)) return RequireAsset<GameObject>(PrefabPath);
            GameObject player = new GameObject("Player");
            try
            {
                int layer = LayerMask.NameToLayer("Player");
                player.layer = layer;
                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.35f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.35f;
                cc.skinWidth = 0.04f;
                cc.minMoveDistance = 0f;
                var cameraGo = new GameObject("Player Camera");
                cameraGo.layer = layer;
                cameraGo.tag = "MainCamera";
                cameraGo.transform.SetParent(player.transform, false);
                cameraGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                var camera = cameraGo.AddComponent<Camera>();
                camera.fieldOfView = 60f; // vertical FOV; horizontal depends on aspect ratio
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.09f, 0.10f, 0.12f);
                cameraGo.AddComponent<AudioListener>();
                player.AddComponent<PlayerInputReader>().Configure(actions);
                player.AddComponent<PlayerView>().Configure(camera);
                player.AddComponent<PlayerMotor>().Configure(config);
                var saved = PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
                if (saved == null) throw new IOException("Cannot save player prefab.");
                return saved;
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }
        }

        private static void AddSlope(Material material)
        {
            var root = new GameObject("Phase01 Terrain");
            root.layer = LayerMask.NameToLayer("World");
            // Top of ramp starts at floor level and rises to ~1.035 m along +Z.
            var ramp = TerrainCube("Ramp", root.transform, new Vector3(5.5f, 0.3969f, 1.5f), new Vector3(3f, 0.25f, 4f), material);
            ramp.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            TerrainCube("Ramp Landing", root.transform, new Vector3(5.5f, 0.5176f, 4.375f), new Vector3(3f, 1.0352f, 2f), material);
        }

        private static GameObject TerrainCube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.layer = LayerMask.NameToLayer("World");
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            return obj;
        }
    }
}
