using System;
using UnityEditor;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;

namespace Ukubola.EditorTools
{
    public static class UkubolaCombatTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 05/Run Combat Self-Tests", false, 63)]
        public static void Run()
        {
            if (!UkubolaCombatSetup.CanRun()) return;
            int count = 0; GameObject go = null;
            try
            {
                void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); count++; }
                var random = new System.Random(1903);
                for (int i = 0; i < 1000; i++)
                {
                    int damage = HitscanAttack.RollDamage(random); Check(damage == 5 || damage == 10 || damage == 15, "Damage roll");
                    float spread = HitscanAttack.RollSpread(random, 5.5f); Check(spread >= -5.5f && spread <= 5.5f, "Spread bounds");
                    var direction = HitscanAttack.SpreadDirection(Vector3.forward, spread);
                    Check(Mathf.Abs(direction.magnitude - 1f) < .0001f && Mathf.Abs(direction.y) < .0001f, "Spread direction");
                }
                Check(HitscanAttack.SpreadDirection(Vector3.forward, 0) == Vector3.forward, "Pistol straight ray");
                Check(!HitscanAttack.Ready(1, 1.1) && HitscanAttack.Ready(1.1, 1.1) && HitscanAttack.Ready(2, 1.1), "Cooldown boundary");
                Check(Math.Abs(HitscanAttack.NextShot(10,.4f)-10.4) < .000001, "Pistol interval");
                Check(Math.Abs(HitscanAttack.NextShot(10,.85f)-10.85) < .000001, "Shotgun interval");
                foreach (float depth in new[]{1f, 2f, 5f, 10f, 50f})
                foreach (float pitch in new[]{-40f,-30f,-20f,-5f,0f,5f,20f,30f,40f})
                {
                    float height = depth * Mathf.Tan(pitch * Mathf.Deg2Rad);
                    bool valid = AutoAim.TryDirection(Vector3.forward, depth, height, out var dir);
                    Check(valid == (Mathf.Abs(pitch) <= 30f), "Autoaim angle");
                    if (valid) Check(Mathf.Abs(dir.x) < .0001f && Mathf.Abs(dir.y - Mathf.Sin(pitch * Mathf.Deg2Rad)) < .0001f && dir.z > 0f, "Vertical-only aim");
                }
                Check(!AutoAim.TryDirection(Vector3.forward, 0, 1, out _), "Reject zero depth");
                Check(!AutoAim.TryDirection(Vector3.up, 5, 1, out _), "Reject vertical heading");
                foreach (int fps in new[]{30,60,120})
                {
                    float remaining = 30f, traveled = 0f;
                    for (int frame = 0; frame < fps * 3; frame++) { float distance = Projectile.Travel(12f, 1f / fps, remaining); Check(distance >= 0f && distance <= remaining, "Projectile step bounds"); remaining -= distance; traveled += distance; }
                    Check(Mathf.Abs(traveled - 30f) < .001f && remaining < .001f, "Projectile range cap");
                }
                Check(Projectile.Travel(12,0,30) == 0 && Projectile.Travel(12,1,1) == 1, "Projectile zero dt and final step");
                go = new GameObject("UKUBOLA P05 transient test") { hideFlags = HideFlags.HideAndDontSave }; go.SetActive(false);
                var inv = go.AddComponent<PlayerInventory>(); var hp = go.AddComponent<Health>(); hp.Configure(100,null);
                Check(inv.Count(AmmoType.Bullets) == 50 && inv.TryConsumeAmmo(AmmoType.Bullets,1) && inv.Count(AmmoType.Bullets)==49, "Pistol cost");
                Check(!inv.HasWeapon(WeaponId.Shotgun) && inv.TryGrantWeapon(WeaponId.Shotgun) && inv.AddAmmo(AmmoType.Shells,20)==20 && inv.TryConsumeAmmo(AmmoType.Shells,1) && inv.Count(AmmoType.Shells)==19, "Shotgun cost per blast");
                Check(inv.TryConsumeAmmo(AmmoType.Shells,19) && !inv.TryConsumeAmmo(AmmoType.Shells,1) && inv.Count(AmmoType.Shells)==0, "Empty ammo");
                for (int i=0;i<7;i++) hp.TakeDamage(new DamageInfo(10,go,DamageType.Hitscan));
                Check(hp.Current==30, "Seven pellet damage");
                Check(hp.TakeDamage(new DamageInfo(25,go,DamageType.Projectile)).Applied && hp.Current==5, "Projectile damage contract");
                Check(hp.TakeDamage(new DamageInfo(15,go,DamageType.Hitscan)).Killed && !hp.TakeDamage(new DamageInfo(15)).Applied, "Death once");
                Debug.Log("[UKUBOLA P05] COMBAT SELF-TESTS PASSED | " + count + " checks.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P05] SELF-TESTS FAILED after " + count + " checks: " + ex.Message); }
            finally { if (go) UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
