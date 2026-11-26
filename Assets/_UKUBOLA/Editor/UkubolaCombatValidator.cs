using System;
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
using Setup = Ukubola.EditorTools.UkubolaCombatSetup;

namespace Ukubola.EditorTools
{
    public static class UkubolaCombatValidator
    {
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        [MenuItem("Tools/UKUBOLA/Phase 05/Validate Combat Lab", false, 62)]
        public static void Validate()
        {
            if (!Setup.CanRun()) return;
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                CheckScene(Setup.OpenInternal()); Debug.Log("[UKUBOLA P05] VALIDATION PASSED | 0 errors. Play checks still required.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P05] VALIDATION FAILED: " + ex.Message); }
        }
        internal static void CheckScene(Scene scene)
        {
            Check(scene.IsValid() && scene.isLoaded && scene.path == Setup.Lab, "Incorrect CombatLab scene.");
            var motor = Setup.Find<PlayerMotor>(scene).Single(); var input = motor.GetComponent<PlayerInputReader>();
            var hp = motor.GetComponent<Health>(); var inv = motor.GetComponent<PlayerInventory>(); var life = motor.GetComponent<PlayerLife>();
            Check(motor.isActiveAndEnabled && motor.Config && motor.Config.IsValid && input && input.isActiveAndEnabled && input.Actions && motor.GetComponent<CharacterController>() && hp && hp.Armor && inv && life && life.isActiveAndEnabled, "Previous player components invalid.");
            var map = input.Actions.FindActionMap("Gameplay");
            Check(map != null && map.FindAction("Fire") != null && map.FindAction("WeaponSlot") != null && map.FindAction("WeaponSlot").bindings.Count >= 2, "Missing Fire or WeaponSlot actions.");
            var controller = Setup.Find<WeaponController>(scene).Single();
            Check(controller.gameObject == motor.gameObject && controller.isActiveAndEnabled, "Weapon controller missing from player.");
            CheckDefinition(controller.Pistol, WeaponId.Pistol, AmmoType.Bullets, 1);
            CheckDefinition(controller.Shotgun, WeaponId.Shotgun, AmmoType.Shells, 7);
            var display = Setup.Find<RetroDisplay>(scene).Single(); var view = Setup.Find<WeaponView>(scene).Single();
            Check(display.isActiveAndEnabled && display.WorldCamera && display.OutputCamera && display.Canvas && display.Image && display.Target && display.Target.width == 320 && display.Target.height == 200 && display.Target.filterMode == FilterMode.Point && display.WorldCamera.targetTexture == display.Target && display.Image.texture == display.Target && controller.AimCamera == display.WorldCamera && motor.GetComponent<PlayerView>().PlayerCamera == display.WorldCamera, "Retro display or camera references invalid.");
            Check(view.isActiveAndEnabled && view == controller.View && view.GetComponent<RectMask2D>() && view.Image && view.Image.transform.parent == view.transform && view.Image.texture == controller.Pistol.Idle && view.transform.parent == display.Image.transform && !view.Image.raycastTarget, "Weapon overlay references invalid.");
            var diag = Setup.Find<CombatDiagnostics>(scene).Single();
            Check(diag.isActiveAndEnabled && diag.Weapons == controller && diag.Player == hp && diag.Inventory == inv && diag.Target && diag.Target != hp && diag.Target.MaxHealth == 100 && !diag.Target.Armor && diag.TargetVisual && diag.TargetVisual.gameObject == diag.Target.gameObject && diag.TargetVisual.sharedMaterial && diag.TargetVisual.sharedMaterial.shader.isSupported, "Diagnostic references invalid.");
            Check(Setup.Find<SurvivalDiagnostics>(scene).Length == 0 && Setup.Find<Health>(scene).Length == 2, "Previous diagnostics not removed or extra health components.");
            Check(diag.Readout && diag.Readout.font && diag.Readout.transform.IsChildOf(display.Image.transform) && diag.Readout.isActiveAndEnabled, "Readout missing.");
            int world = LayerMask.NameToLayer("World"), enemy = LayerMask.NameToLayer("Enemy"), player = LayerMask.NameToLayer("Player"), projectile = LayerMask.NameToLayer("Projectile"), interact = LayerMask.NameToLayer("Interactable");
            Check(world >= 0 && enemy >= 0 && player >= 0 && projectile >= 0 && interact >= 0, "Missing layer.");
            int required = (1 << world) | (1 << enemy) | (1 << player) | (1 << interact) | 1;
            Check((controller.HitMask & required) == required && controller.TargetMask == (1 << enemy) && diag.Target.gameObject.layer == enemy && diag.Target.GetComponent<BoxCollider>() && diag.Target.GetComponent<BoxCollider>().enabled && !diag.Target.GetComponent<BoxCollider>().isTrigger, "Hit masks or target collider invalid.");
            var p = diag.ProjectilePrefab;
            Check(p && p.gameObject.layer == projectile && p.GetComponent<Renderer>() && p.GetComponent<Renderer>().sharedMaterial && p.GetComponent<Renderer>().sharedMaterial.shader.isSupported && !p.GetComponent<Collider>() && PrefabUtility.IsPartOfPrefabAsset(p) && (display.WorldCamera.cullingMask & (1 << projectile)) != 0, "Projectile prefab invalid.");
            Check(Setup.Find<Camera>(scene).Count(c => c.isActiveAndEnabled) == 2 && Setup.Find<AudioListener>(scene).Count(a => a.isActiveAndEnabled) == 1, "Unexpected camera/listener count.");
            var context = Setup.Find<LevelContext>(scene).Single(); Check(context.LevelId == Setup.LevelId && context.isActiveAndEnabled, "Invalid level context.");
            var errors = context.RebuildRegistry(); Check(errors.Count == 0, string.Join("\n", errors));
            Check(Setup.Find<GameSession>(scene).Count(s => s.isActiveAndEnabled) == 1 && Setup.Find<LevelExit>(scene).Single().Stats == Setup.Find<LevelStats>(scene).Single(), "Session or level flow invalid.");
            Check(scene.GetRootGameObjects().Count(r => r.name == "Level Geometry") == 1, "Level geometry missing.");
            foreach (var t in Setup.Find<Transform>(scene)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script: " + t.name);
        }
        internal static void CheckDefinition(WeaponDefinition def, WeaponId id, AmmoType ammo, int pellets)
        {
            string prefix = "Invalid definition: " + id + " | ";
            Check(def, prefix + "missing or unloaded WeaponDefinition reference.");
            Check(def.Weapon == id, prefix + "Weapon=" + def.Weapon + ", expected " + id);
            Check(def.Ammo == ammo && PlayerInventory.BaseCapacity(def.Ammo) > 0, prefix + "Ammo=" + def.Ammo + ", expected " + ammo);
            Check(def.AmmoCost == 1, prefix + "AmmoCost=" + def.AmmoCost);
            Check(def.Pellets == pellets, prefix + "Pellets=" + def.Pellets + ", expected " + pellets);
            Check(def.Interval >= .05f && def.Interval <= 10f, prefix + "Interval=" + def.Interval);
            Check(def.Spread >= 0f && def.Spread <= 30f, prefix + "Spread=" + def.Spread);
            Check(def.Range > 0f && def.Range <= 200f, prefix + "Range=" + def.Range);
            Check(def.Idle, prefix + "Idle texture missing or unloaded.");
            Check(def.Firing, prefix + "Firing texture missing or unloaded.");
            Check(def.IsValid, prefix + "IsValid returned false.");
            foreach (var t in new[]{def.Idle, def.Firing}) Check(t && t.width == 96 && t.height == 64 && t.filterMode == FilterMode.Point && t.mipmapCount == 1, prefix + "Invalid sprite: " + t.name + " (" + t.width + "x" + t.height + ", " + t.filterMode + ", mipmaps=" + t.mipmapCount + ").");
        }
    }
}
