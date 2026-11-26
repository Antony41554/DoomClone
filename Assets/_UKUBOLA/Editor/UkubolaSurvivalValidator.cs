using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;
using Setup = Ukubola.EditorTools.UkubolaSurvivalSetup;

namespace Ukubola.EditorTools
{
    public static class UkubolaSurvivalValidator
    {
        [MenuItem("Tools/UKUBOLA/Phase 04/Validate Survival Lab", false, 52)]
        public static void Validate()
        {
            if (!Setup.CanRun()) return;
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                CheckScene(Setup.OpenInternal());
                Debug.Log("[UKUBOLA P04] VALIDATION PASSED | 0 errors. Play checks still required.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P04] VALIDATION FAILED: " + ex.Message); }
        }
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        internal static void CheckScene(Scene scene)
        {
            Check(scene.IsValid() && scene.isLoaded && scene.path == Setup.Lab, "Incorrect SurvivalLab scene.");
            var motor = Setup.Find<PlayerMotor>(scene).Single();
            var hp = motor.GetComponent<Health>(); var armor = motor.GetComponent<Armor>(); var bag = motor.GetComponent<PlayerInventory>(); var life = motor.GetComponent<PlayerLife>();
            var input = motor.GetComponent<PlayerInputReader>(); var cc = motor.GetComponent<CharacterController>(); var view = motor.GetComponent<PlayerView>();
            Check(motor.isActiveAndEnabled && motor.Config && motor.Config.IsValid && input && input.isActiveAndEnabled && cc && cc.enabled && view && view.PlayerCamera, "Player controls invalid.");
            Check(hp && hp.isActiveAndEnabled && hp.MaxHealth == 100 && armor && armor.isActiveAndEnabled && hp.Armor == armor && bag && bag.isActiveAndEnabled && life && life.isActiveAndEnabled, "Player survival components missing or invalid.");
            var diag = Setup.Find<SurvivalDiagnostics>(scene).Single();
            var target = diag.Target;
            Check(diag.isActiveAndEnabled && diag.Player == hp && diag.Inventory == bag && target && target != hp && target.isActiveAndEnabled && target.MaxHealth == 100 && !target.Armor && Setup.Find<Health>(scene).Length == 2, "Diagnostic health references invalid.");
            Check(diag.TargetVisual && diag.TargetVisual.gameObject == target.gameObject && diag.TargetVisual.enabled && diag.TargetVisual.sharedMaterial && diag.TargetVisual.sharedMaterial.shader && diag.TargetVisual.sharedMaterial.shader.isSupported && target.GetComponent<BoxCollider>() && target.GetComponent<BoxCollider>().enabled && !target.GetComponent<BoxCollider>().isTrigger && target.gameObject.layer == LayerMask.NameToLayer("Enemy"), "Target material, collider or layer invalid.");
            Check(target.GetComponent<EntityId>() && target.GetComponent<EntityId>().IsValid, "Damage target has no valid EntityId.");
            var display = Setup.Find<RetroDisplay>(scene).Single();
            Check(display.isActiveAndEnabled && display.WorldCamera == view.PlayerCamera && display.WorldCamera && display.OutputCamera && display.WorldCamera != display.OutputCamera && display.Canvas && display.Image && display.Target && display.Target.width == 320 && display.Target.height == 200 && display.Target.filterMode == FilterMode.Point && display.WorldCamera.targetTexture == display.Target && display.Image.texture == display.Target, "Retro display invalid.");
            Check(diag.Readout && diag.Readout.isActiveAndEnabled && diag.Readout.font && diag.Readout.transform.IsChildOf(display.Canvas.transform) && diag.Readout.transform.parent && diag.Readout.transform.parent.gameObject.activeInHierarchy, "Diagnostic overlay missing.");
            Check(Setup.Find<Camera>(scene).Count(c => c.isActiveAndEnabled) == 2 && Setup.Find<AudioListener>(scene).Count(a => a.isActiveAndEnabled) == 1, "Unexpected active camera or listener count.");
            int enemy = LayerMask.NameToLayer("Enemy"), player = LayerMask.NameToLayer("Player");
            Check(enemy >= 0 && player >= 0 && !Physics.GetIgnoreLayerCollision(enemy, player) && (display.WorldCamera.cullingMask & (1 << enemy)) != 0, "Target layer is not visible or collidable.");
            var context = Setup.Find<LevelContext>(scene).Single();
            Check(context.isActiveAndEnabled && context.LevelId == Setup.LevelId, "Incorrect level context.");
            var errors = context.RebuildRegistry(); Check(errors.Count == 0, string.Join("\n", errors));
            Check(Setup.Find<GameSession>(scene).Count(s => s.isActiveAndEnabled) == 1, "Missing GameSession.");
            var stats = Setup.Find<LevelStats>(scene).Single(); var exit = Setup.Find<LevelExit>(scene).Single(); var spawn = Setup.Find<PlayerSpawn>(scene).Single();
            Check(stats.isActiveAndEnabled && exit.isActiveAndEnabled && exit.Stats == stats && spawn.isActiveAndEnabled && (motor.transform.position - (spawn.Position + Vector3.up * .03f)).sqrMagnitude < .000001f, "Previous level flow or spawn invalid.");
            Check(scene.GetRootGameObjects().Count(r => r.name == "Level Geometry") == 1, "Missing level geometry.");
            foreach (var t in Setup.Find<Transform>(scene)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script: " + t.name);
        }
    }
}
