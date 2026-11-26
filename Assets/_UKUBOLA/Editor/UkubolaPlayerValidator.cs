using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Ukubola.Player;
using Setup = Ukubola.EditorTools.UkubolaPlayerSetup;

namespace Ukubola.EditorTools
{
    public static class UkubolaPlayerValidator
    {
        [MenuItem("Tools/UKUBOLA/Phase 01/Validate Player Lab", false, 22)]
        public static void Validate()
        {
            if (!Setup.CanRun()) return;
            try
            {
                var actions = Setup.RequireAsset<InputActionAsset>(Setup.InputPath);
                Setup.RequireInput(actions);
                var config = Setup.RequireAsset<PlayerConfig>(Setup.ConfigPath);
                var prefab = Setup.RequireAsset<GameObject>(Setup.PrefabPath);
                CheckPrefabOrThrow(prefab, config, actions);
                var scene = SceneManager.GetActiveScene();
                if (SceneManager.sceneCount != 1 || scene.path != Setup.LabPath)
                    throw new InvalidOperationException("Open Player Lab first. Validate does not open or modify scenes.");
                CheckSceneOrThrow(scene, config, actions);
                Debug.Log("[UKUBOLA P01] VALIDATION PASSED | 0 errors. Play tests still required.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P01] VALIDATION FAILED: " + ex.Message); }
        }

        internal static void CheckPrefabOrThrow(GameObject prefab, PlayerConfig config, InputActionAsset actions)
        {
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab)) throw new InvalidOperationException("Player asset must be a prefab.");
            var errors = new List<string>();
            CheckPlayer(prefab, config, actions, errors);
            ThrowErrors(errors);
        }

        internal static void CheckSceneOrThrow(Scene scene, PlayerConfig config, InputActionAsset actions)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.path != Setup.LabPath)
                throw new InvalidOperationException("PlayerLab scene not loaded.");
            var errors = new List<string>();
            var roots = scene.GetRootGameObjects();
            var players = roots.SelectMany(r => r.GetComponentsInChildren<PlayerMotor>(true)).ToArray();
            var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Where(c => c.isActiveAndEnabled).ToArray();
            if (players.Length != 1) errors.Add("Expected exactly one PlayerMotor.");
            else
            {
                var motor = players[0];
                if (!motor.isActiveAndEnabled || motor.transform.parent != null) errors.Add("Player must be an active root object.");
                CheckPlayer(motor.gameObject, config, actions, errors);
                var view = motor.GetComponent<PlayerView>();
                if (cameras.Length != 1 || view == null || cameras[0] != view.PlayerCamera)
                    errors.Add("The player camera must be the only active camera.");
                if (PrefabUtility.GetCorrespondingObjectFromSource(motor.gameObject) != AssetDatabase.LoadAssetAtPath<GameObject>(Setup.PrefabPath))
                    errors.Add("Player must be an instance of the generated prefab.");
            }
            if (roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)).Count(a => a.isActiveAndEnabled) != 1)
                errors.Add("Expected exactly one active AudioListener.");
            var contexts = roots.SelectMany(r => r.GetComponentsInChildren<LevelContext>(true)).ToArray();
            if (contexts.Length != 1 || !contexts[0].isActiveAndEnabled || contexts[0].LevelId != "PLAYER_LAB") errors.Add("Expected one active PLAYER_LAB LevelContext.");
            else errors.AddRange(contexts[0].RebuildRegistry());
            var sessions = roots.SelectMany(r => r.GetComponentsInChildren<GameSession>(true)).ToArray();
            if (sessions.Length != 1 || !sessions[0].isActiveAndEnabled || sessions[0].transform.parent != null) errors.Add("Expected one active root GameSession.");
            int worldLayer = LayerMask.NameToLayer("World"), playerLayer = LayerMask.NameToLayer("Player");
            if (worldLayer < 0 || playerLayer < 0) errors.Add("World or Player layer missing.");
            else if (Physics.GetIgnoreLayerCollision(worldLayer, playerLayer)) errors.Add("Player/World layer collision disabled.");
            var colliders = roots.SelectMany(r => r.GetComponentsInChildren<BoxCollider>(true)).Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && c.gameObject.layer == worldLayer).ToArray();
            foreach (string name in new[] { "Floor", "North Wall", "South Wall", "West Wall", "East Wall", "Diagnostic Block", "Step 1", "Step 2", "Ramp", "Ramp Landing" })
                if (colliders.Count(c => c.name == name) != 1) errors.Add("Expected exactly one active World collider: " + name);
            foreach (var root in roots)
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0) errors.Add("Missing script: " + t.name);
            ThrowErrors(errors);
        }

        private static void CheckPlayer(GameObject player, PlayerConfig config, InputActionAsset actions, List<string> errors)
        {
            if (!config.IsValid) errors.Add("PlayerConfig values invalid.");
            var motor = player.GetComponent<PlayerMotor>();
            var input = player.GetComponent<PlayerInputReader>();
            var view = player.GetComponent<PlayerView>();
            var cc = player.GetComponent<CharacterController>();
            if (!player.activeSelf || player.layer != LayerMask.NameToLayer("Player")) errors.Add("Player root inactive or on wrong layer.");
            if (motor == null || !motor.enabled || motor.Config != config) errors.Add("PlayerMotor or PlayerConfig reference invalid.");
            if (input == null || !input.enabled || input.Actions != actions) errors.Add("PlayerInputReader or action asset reference invalid.");
            if (view == null || !view.enabled || view.PlayerCamera == null) errors.Add("PlayerView or camera reference invalid.");
            else
            {
                var cam = view.PlayerCamera;
                if (cam.transform.parent != player.transform || !cam.enabled || !cam.gameObject.activeSelf || !cam.CompareTag("MainCamera") ||
                    Quaternion.Angle(cam.transform.localRotation, Quaternion.identity) > 0.01f || cam.orthographic || cam.GetComponent<AudioListener>() == null)
                    errors.Add("Player camera hierarchy, rotation, type, tag or AudioListener invalid.");
            }
            if (cc == null || !cc.enabled || cc.height <= 2f * cc.radius || cc.stepOffset < 0.25f || cc.stepOffset >= cc.height || cc.skinWidth <= 0f || cc.slopeLimit < 15f || cc.minMoveDistance != 0f)
                errors.Add("CharacterController configuration invalid for steps/ramp.");
            if (player.GetComponent<Rigidbody>() != null) errors.Add("Unexpected Rigidbody on CharacterController player.");
            if ((player.transform.localScale - Vector3.one).sqrMagnitude > 0.000001f) errors.Add("Player scale must be 1,1,1.");
        }

        private static void ThrowErrors(List<string> errors)
        {
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
        }
    }
}
