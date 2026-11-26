using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Setup = Ukubola.EditorTools.UkubolaProjectSetup;

namespace Ukubola.EditorTools
{
    public static class ProjectValidator
    {
        [MenuItem("Tools/UKUBOLA/Validate Project", false, 2)]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { Debug.LogWarning("[UKUBOLA] Validate in Edit mode after compilation/import."); return; }
            var errors = new List<string>();
            var warnings = new List<string>();
            if (Application.unityVersion != Setup.TargetVersion) warnings.Add("Expected editor " + Setup.TargetVersion + "; actual " + Application.unityVersion);
            if (!Setup.IsUrp()) errors.Add("Active pipeline is not URP.");
            foreach (string package in Setup.Packages) if (!Setup.HasPackage(package)) errors.Add("Missing package: " + package);
#if !ENABLE_INPUT_SYSTEM
            errors.Add("New Input System backend is not active. Edit > Project Settings > Player > Other Settings > Active Input Handling: Input System Package (New). Restart Unity.");
#endif
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64) warnings.Add("Windows x64 is not the active build target. Set it before Windows builds.");
            foreach (string folder in Setup.Folders) if (!AssetDatabase.IsValidFolder(Setup.Root + "/" + folder)) errors.Add("Missing folder: " + folder);
            foreach (string layer in Setup.Layers) if (LayerMask.NameToLayer(layer) < 0) errors.Add("Missing layer: " + layer);
            CheckInput(errors);
            CheckArena(errors);
            foreach (string warning in warnings) Debug.LogWarning("[UKUBOLA] " + warning);
            foreach (string error in errors) Debug.LogError("[UKUBOLA] " + error);
            string result = errors.Count == 0 ? "VALIDATION PASSED" : "VALIDATION FAILED";
            string summary = "[UKUBOLA] " + result + " | " + errors.Count + " errors | " + warnings.Count + " warnings. Manual Play and repeat-setup checks are still required.";
            if (errors.Count == 0) Debug.Log(summary); else Debug.LogError(summary);
        }

        private static void CheckInput(List<string> errors)
        {
            try
            {
                if (!File.Exists(Setup.InputPath)) { errors.Add("Missing input actions asset."); return; }
                var asset = AssetDatabase.LoadMainAssetAtPath(Setup.InputPath);
                if (asset == null || asset.GetType().FullName != "UnityEngine.InputSystem.InputActionAsset") errors.Add("Input actions file did not import as InputActionAsset.");
                var definition = JsonUtility.FromJson<Setup.InputDefinition>(File.ReadAllText(Setup.InputPath));
                if (definition?.maps == null) { errors.Add("Invalid input actions JSON."); return; }
                var ids = new HashSet<Guid>();
                foreach (var map in definition.maps)
                {
                    if (map == null) { errors.Add("Null input map."); continue; }
                    CheckId(map.id, ids, errors, map.name);
                    foreach (var action in map.actions ?? Array.Empty<Setup.ActionDef>()) CheckId(action.id, ids, errors, map.name + "/" + action.name);
                    foreach (var binding in map.bindings ?? Array.Empty<Setup.Binding>()) CheckId(binding.id, ids, errors, map.name + "/binding");
                }
                CheckMap(definition, "Gameplay", new[] { "Move", "Look", "Fire", "Use", "Run", "WeaponSlot", "Pause", "Automap" }, errors);
                CheckMap(definition, "UI", new[] { "Navigate", "Submit", "Cancel", "Point", "Click", "RightClick", "MiddleClick", "ScrollWheel" }, errors);
            }
            catch (Exception ex) { errors.Add("Input validation: " + ex.Message); }
        }
        private static void CheckId(string value, HashSet<Guid> ids, List<string> errors, string owner)
        {
            if (!Guid.TryParse(value, out Guid id) || id == Guid.Empty || !ids.Add(id)) errors.Add("Invalid or duplicate input GUID: " + owner);
        }
        private static void CheckMap(Setup.InputDefinition definition, string name, string[] expected, List<string> errors)
        {
            var maps = definition.maps.Where(m => m != null && m.name == name).ToArray();
            if (maps.Length != 1) { errors.Add("Expected exactly one input map: " + name); return; }
            var actions = maps[0].actions ?? Array.Empty<Setup.ActionDef>();
            var bindings = maps[0].bindings ?? Array.Empty<Setup.Binding>();
            foreach (string action in expected)
            {
                if (actions.Count(a => a != null && a.name == action) != 1) errors.Add("Missing or duplicate action: " + name + "/" + action);
                if (!bindings.Any(b => b != null && b.action == action && !string.IsNullOrWhiteSpace(b.path) && !b.isComposite)) errors.Add("Missing binding: " + name + "/" + action);
            }
        }

        private static void CheckArena(List<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Setup.ArenaPath) == null) { errors.Add("Missing TestArena scene."); return; }
            Scene scene = SceneManager.GetSceneByPath(Setup.ArenaPath);
            Scene previous = SceneManager.GetActiveScene();
            bool opened = false;
            try
            {
                if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(Setup.ArenaPath, OpenSceneMode.Additive); opened = true; }
                var roots = scene.GetRootGameObjects();
                var contexts = roots.SelectMany(r => r.GetComponentsInChildren<LevelContext>(true)).ToArray();
                var sessions = roots.SelectMany(r => r.GetComponentsInChildren<GameSession>(true)).ToArray();
                if (contexts.Length != 1 || !contexts[0].isActiveAndEnabled || string.IsNullOrWhiteSpace(contexts[0].LevelId)) errors.Add("TestArena needs exactly one active LevelContext with a level ID.");
                else errors.AddRange(contexts[0].RebuildRegistry());
                if (sessions.Length != 1 || !sessions[0].isActiveAndEnabled || sessions[0].transform.parent != null) errors.Add("TestArena needs exactly one active root GameSession.");
                var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Where(c => c.isActiveAndEnabled).ToArray();
                if (cameras.Length != 1 || !cameras[0].CompareTag("MainCamera")) errors.Add("TestArena needs one active MainCamera.");
                if (roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)).Count(a => a.isActiveAndEnabled) != 1) errors.Add("TestArena needs one active AudioListener.");
                var colliders = roots.SelectMany(r => r.GetComponentsInChildren<BoxCollider>(true)).Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger).ToArray();
                var floor = colliders.Where(c => c.name == "Floor").ToArray();
                if (floor.Length != 1) errors.Add("TestArena floor collider is missing or duplicated.");
                if (colliders.Length < 8 || colliders.Any(c => c.gameObject.layer != LayerMask.NameToLayer("World"))) errors.Add("TestArena requires at least 8 solid World colliders.");
                var entities = roots.SelectMany(r => r.GetComponentsInChildren<EntityId>(true)).ToArray();
                if (entities.Length < 1) errors.Add("Missing EntityId diagnostic object.");
                foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) errors.Add("Missing script: " + t.name);
            }
            catch (Exception ex) { errors.Add("Arena validation: " + ex.Message); }
            finally
            {
                if (opened && previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
