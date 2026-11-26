using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Ukubola.EditorTools
{
    public static class UkubolaProjectSetup
    {
        internal const string Root = "Assets/_UKUBOLA";
        internal const string ArenaPath = Root + "/Scenes/Tests/TestArena.unity";
        internal const string InputPath = Root + "/Data/Input/UkubolaInput.inputactions";
        internal const string TargetVersion = "6000.6.10f1";
        internal static readonly string[] Layers = { "World", "Player", "Enemy", "Pickup", "Projectile", "Interactable" };
        internal static readonly string[] Packages = { "com.unity.render-pipelines.universal", "com.unity.inputsystem", "com.unity.ugui" };
        internal static readonly string[] Folders = {
            "Runtime/Core", "Runtime/Player", "Runtime/Combat", "Runtime/Enemies", "Runtime/World", "Runtime/Rendering",
            "Runtime/UI", "Runtime/Audio", "Runtime/Persistence", "Editor", "Tests/EditMode", "Tests/PlayMode", "Data/Input",
            "Art/Sprites", "Art/Textures", "Art/Materials", "Art/Shaders", "Audio", "Prefabs", "Scenes/Tests", "Scenes/Levels",
            "Generated/Materials", "ThirdParty"
        };
        private static AddRequest pending;
        private static string pendingPackage;

        [MenuItem("Tools/UKUBOLA/Setup Project", false, 0)]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || pending != null)
            { Debug.LogWarning("[UKUBOLA] Exit Play mode and wait for Unity to finish importing/compiling."); return; }
            try
            {
                // A wrong template should not be converted silently.
                if (!IsUrp()) throw new InvalidOperationException("An active URP asset is required. Use the Universal 3D template or restore its pipeline settings.");
                var missing = Packages.FirstOrDefault(p => !HasPackage(p));
                if (missing != null)
                {
                    if (missing == Packages[0]) throw new InvalidOperationException("URP package not registered. Restore the Universal 3D template packages first.");
                    if (!EditorUtility.DisplayDialog("UKUBOLA - Package required", "Install " + missing + " using Unity Package Manager? Existing packages may be resolved again. Run Setup Project again after import.", "Install", "Cancel")) return;
                    pendingPackage = missing;
                    pending = Client.Add(missing);
                    EditorApplication.update += PollPackage;
                    Debug.Log("[UKUBOLA] Installing " + missing + ". Run Setup Project again after Unity finishes importing.");
                    return;
                }
                EnsureFolders();
                EnsureLayers();
                EnsureInputAsset();
                EnsureArena();
                Debug.Log("[UKUBOLA] Setup completed. Running validation; open TestArena using the menu.");
                ProjectValidator.Validate();
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA] Setup stopped: " + ex.Message + "\n" + ex); }
        }

        private static void PollPackage()
        {
            if (pending == null || !pending.IsCompleted) return;
            EditorApplication.update -= PollPackage;
            if (pending.Status == StatusCode.Success) Debug.Log("[UKUBOLA] Installed " + pendingPackage + ". Run Setup Project again after import.");
            else Debug.LogError("[UKUBOLA] Package install failed: " + pendingPackage + " / " + pending.Error?.message);
            pending = null;
        }

        internal static bool HasPackage(string name) => PackageInfo.GetAllRegisteredPackages().Any(p => p.name == name);
        internal static bool IsUrp() => GraphicsSettings.currentRenderPipeline != null &&
            GraphicsSettings.currentRenderPipeline.GetType().FullName == "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";

        private static void EnsureFolders()
        {
            foreach (string relative in Folders) EnsureFolder(Root + "/" + relative);
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split), name = path.Substring(split + 1);
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name))) throw new IOException("Cannot create folder " + path);
        }

        private static void EnsureLayers()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
            if (settings == null) throw new InvalidOperationException("Cannot read TagManager.asset.");
            var serialized = new SerializedObject(settings);
            var layers = serialized.FindProperty("layers");
            if (layers == null || !layers.isArray || layers.arraySize < 32) throw new InvalidOperationException("Unexpected TagManager layer layout.");
            var existing = new HashSet<string>(StringComparer.Ordinal);
            var free = new List<int>();
            for (int i = 0; i < layers.arraySize; i++)
            {
                string value = layers.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrEmpty(value)) existing.Add(value);
                else if (i >= 8 && i < 32) free.Add(i);
            }
            var missing = Layers.Where(l => !existing.Contains(l)).ToArray();
            if (missing.Length > free.Count) throw new InvalidOperationException("Not enough free user layers. No layer was replaced.");
            for (int i = 0; i < missing.Length; i++) layers.GetArrayElementAtIndex(free[i]).stringValue = missing[i];
            if (missing.Length > 0) { serialized.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
            foreach (string name in Layers) if (LayerMask.NameToLayer(name) < 0) throw new InvalidOperationException("Layer not available: " + name + ". Re-run setup after import.");
        }

        private static void EnsureInputAsset()
        {
            if (File.Exists(InputPath) || AssetDatabase.LoadMainAssetAtPath(InputPath) != null) return;
            File.WriteAllText(InputPath, JsonUtility.ToJson(CreateInputDefinition(), true), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
        }

        // Serializable JSON follows the Input System .inputactions schema.
        [Serializable] internal sealed class InputDefinition { public string name = "UkubolaInput"; public Map[] maps; public Scheme[] controlSchemes = Array.Empty<Scheme>(); }
        [Serializable] internal sealed class Scheme { public string name; public string bindingGroup; public Device[] devices; }
        [Serializable] internal sealed class Device { public string devicePath; public bool isOptional; public bool isOR; }
        [Serializable] internal sealed class Map { public string name; public string id = Guid.NewGuid().ToString(); public ActionDef[] actions; public Binding[] bindings; }
        [Serializable] internal sealed class ActionDef { public string name, type, expectedControlType; public string id = Guid.NewGuid().ToString(); public string processors = "", interactions = ""; public bool initialStateCheck; }
        [Serializable] internal sealed class Binding { public string name = "", id = Guid.NewGuid().ToString(), path, action; public string interactions = "", processors = "", groups = ""; public bool isComposite, isPartOfComposite; }
        private static ActionDef A(string name, string type = "Button", string control = "Button") => new ActionDef { name = name, type = type, expectedControlType = control, initialStateCheck = type == "Value" };
        private static Binding B(string action, string path, string name = "", bool composite = false, bool part = false) => new Binding { action = action, path = path, name = name, isComposite = composite, isPartOfComposite = part };
        private static void Vector(List<Binding> list, string action, string up, string down, string left, string right)
        {
            list.Add(B(action, "2DVector", "2D Vector", true));
            list.Add(B(action, "<Keyboard>/" + up, "up", part: true)); list.Add(B(action, "<Keyboard>/" + down, "down", part: true));
            list.Add(B(action, "<Keyboard>/" + left, "left", part: true)); list.Add(B(action, "<Keyboard>/" + right, "right", part: true));
        }
        private static InputDefinition CreateInputDefinition()
        {
            var gameplay = new List<Binding>();
            Vector(gameplay, "Move", "w", "s", "a", "d");
            gameplay.AddRange(new[] { B("Look", "<Mouse>/delta"), B("Fire", "<Mouse>/leftButton"), B("Use", "<Keyboard>/e"), B("Use", "<Keyboard>/space"), B("Run", "<Keyboard>/leftShift"), B("Pause", "<Keyboard>/escape"), B("Automap", "<Keyboard>/tab") });
            // Discrete selection: each numbered key reports its 1..7 value via Scale.
            for (int i = 1; i <= 7; i++) { var binding = B("WeaponSlot", "<Keyboard>/" + i); binding.processors = "scale(factor=" + i + ")"; gameplay.Add(binding); }
            var ui = new List<Binding>();
            Vector(ui, "Navigate", "upArrow", "downArrow", "leftArrow", "rightArrow");
            Vector(ui, "Navigate", "w", "s", "a", "d");
            ui.AddRange(new[] { B("Submit", "<Keyboard>/enter"), B("Submit", "<Keyboard>/space"), B("Cancel", "<Keyboard>/escape"), B("Point", "<Mouse>/position"), B("Click", "<Mouse>/leftButton"), B("RightClick", "<Mouse>/rightButton"), B("MiddleClick", "<Mouse>/middleButton"), B("ScrollWheel", "<Mouse>/scroll") });
            return new InputDefinition
            {
                maps = new[] {
                new Map { name = "Gameplay", actions = new[] { A("Move", "Value", "Vector2"), A("Look", "Value", "Vector2"), A("Fire"), A("Use"), A("Run"), A("WeaponSlot", "PassThrough", "Axis"), A("Pause"), A("Automap") }, bindings = gameplay.ToArray() },
                new Map { name = "UI", actions = new[] { A("Navigate", "Value", "Vector2"), A("Submit"), A("Cancel"), A("Point", "PassThrough", "Vector2"), A("Click", "PassThrough", "Button"), A("RightClick", "PassThrough", "Button"), A("MiddleClick", "PassThrough", "Button"), A("ScrollWheel", "PassThrough", "Vector2") }, bindings = ui.ToArray() }
            }
            };
        }

        private static Material MaterialFor(string name, Color color)
        {
            string path = Root + "/Generated/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new IOException("Existing non-material asset at " + path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader unavailable.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.layer = LayerMask.NameToLayer("World"); go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
        private static void EnsureArena()
        {
            if (File.Exists(ArenaPath) ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) != null)
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Scene creation cancelled.");

            var floor = MaterialFor("Phase00_Floor",
                new Color(.28f, .31f, .33f));

            var wall = MaterialFor("Phase00_Wall",
                new Color(.48f, .49f, .53f));

            var accent = MaterialFor("Phase00_Marker",
                new Color(.8f, .33f, .12f));

            var arena = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );

            if (!arena.IsValid() ||
                !arena.isLoaded ||
                SceneManager.GetActiveScene() != arena)
            {
                throw new InvalidOperationException(
                    "TestArena was not created as the active scene."
                );
            }

            var context = new GameObject("LevelContext");
            context.AddComponent<LevelContext>();

            new GameObject("GameSession")
                .AddComponent<GameSession>();

            var geometry = new GameObject("Geometry").transform;

            Cube("Floor", geometry,
                new Vector3(0, -.25f, 0),
                new Vector3(20, .5f, 16), floor);

            Cube("North Wall", geometry,
                new Vector3(0, 1.5f, 8),
                new Vector3(20, 3, .4f), wall);

            Cube("South Wall", geometry,
                new Vector3(0, 1.5f, -8),
                new Vector3(20, 3, .4f), wall);

            Cube("West Wall", geometry,
                new Vector3(-10, 1.5f, 0),
                new Vector3(.4f, 3, 16), wall);

            Cube("East Wall", geometry,
                new Vector3(10, 1.5f, 0),
                new Vector3(.4f, 3, 16), wall);

            Cube("Diagnostic Block", geometry,
                new Vector3(2, .75f, 1),
                new Vector3(1.5f, 1.5f, 1.5f), accent)
                .AddComponent<EntityId>();

            Cube("Step 1", geometry,
                new Vector3(-4, .125f, 1),
                new Vector3(2, .25f, 2), wall);

            Cube("Step 2", geometry,
                new Vector3(-4, .25f, 3),
                new Vector3(2, .5f, 2), wall);

            var spawn = new GameObject("PlayerSpawn_PLACEHOLDER");
            spawn.transform.position = new Vector3(0, 0, -5);

            var cameraGo = new GameObject("Diagnostic Camera");
            cameraGo.tag = "MainCamera";

            cameraGo.transform.position = new Vector3(0, 5, -7);
            cameraGo.transform.LookAt(new Vector3(0, .5f, 2));

            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 65;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.09f, .10f, .12f);

            cameraGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Diagnostic Light");
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            if (!EditorSceneManager.SaveScene(arena, ArenaPath))
                throw new IOException("Cannot save TestArena.");

            AssetDatabase.SaveAssets();

            Debug.Log("[UKUBOLA] TestArena generated successfully.");
        }

        [MenuItem("Tools/UKUBOLA/Open Test Arena", false, 1)]
        public static void OpenArena()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) == null) { Debug.LogWarning("[UKUBOLA] Run Setup Project first."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);
        }
    }
}
