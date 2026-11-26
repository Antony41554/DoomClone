using System;
using UnityEditor;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;

namespace Ukubola.EditorTools
{
    public static class UkubolaSurvivalTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 04/Run Survival Self-Tests", false, 53)]
        public static void Run()
        {
            if (!UkubolaSurvivalSetup.CanRun()) return;
            GameObject go = null;
            int count = 0;
            try
            {
                void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); count++; }
                // Hidden, temporary test object, never saved into any scene or prefab.
                go = new GameObject("UKUBOLA P04 transient test") { hideFlags = HideFlags.HideAndDontSave };
                go.SetActive(false);
                var armor = go.AddComponent<Armor>(); var health = go.AddComponent<Health>(); health.Configure(100, armor);
                var inv = go.AddComponent<PlayerInventory>();
                int deaths = 0, revives = 0;
                health.Died += (_, __) => deaths++; health.Revived += _ => revives++;
                Check(health.Current == 100 && !health.IsDead && armor.Points == 0, "Initial health/armor");
                Check(!health.TakeDamage(new DamageInfo(-5)).Applied && !health.TakeDamage(new DamageInfo(0)).Applied && health.Current == 100, "Nonpositive damage");
                var r = health.TakeDamage(new DamageInfo(20)); Check(r.HealthLost == 20 && r.Absorbed == 0 && health.Current == 80, "Unarmored damage");
                Check(health.Heal(25) == 20 && health.Current == 100 && health.Heal(1) == 0 && health.Heal(-1) == 0, "Heal caps");
                Check(armor.TryEquip(ArmorKind.Green) && !armor.TryEquip(ArmorKind.Green), "Green equip");
                r = health.TakeDamage(new DamageInfo(20)); Check(r.Absorbed == 6 && r.HealthLost == 14 && armor.Points == 94 && health.Current == 86, "Green damage rounding");
                Check(armor.TryEquip(ArmorKind.Blue) && !armor.TryEquip(ArmorKind.Green) && armor.Points == 200, "Blue equip/no downgrade");
                r = health.TakeDamage(new DamageInfo(60)); Check(r.Absorbed == 30 && r.HealthLost == 30 && health.Current == 56 && armor.Points == 170, "Blue damage");
                r = health.TakeDamage(new DamageInfo(int.MaxValue)); Check(r.Absorbed == 170 && r.HealthLost == 56 && r.Killed && health.IsDead && deaths == 1 && armor.Kind == ArmorKind.None, "Lethal/depletion/overflow");
                Check(!health.TakeDamage(new DamageInfo(100)).Applied && health.Heal(100) == 0 && deaths == 1, "Dead immunity/no healing");
                health.ResetState(); Check(health.Current == 100 && armor.Points == 0 && revives == 1, "Explicit revive");
                health.ResetState(); Check(revives == 1 && deaths == 1, "Reset alive no revive event");
                Check(inv.Count(AmmoType.Bullets) == 50 && inv.HasWeapon(WeaponId.Fist) && inv.HasWeapon(WeaponId.Pistol) && !inv.HasWeapon(WeaponId.Shotgun) && inv.Keys == KeyId.None, "Initial inventory");
                foreach (AmmoType type in Enum.GetValues(typeof(AmmoType)))
                {
                    int cap = PlayerInventory.BaseCapacity(type), initial = inv.Count(type);
                    Check(inv.Capacity(type) == cap, "Capacity " + type);
                    Check(inv.AddAmmo(type, int.MaxValue) == cap - initial && inv.Count(type) == cap, "Saturate " + type);
                    Check(inv.AddAmmo(type, 1) == 0 && inv.AddAmmo(type, -1) == 0, "Reject extra " + type);
                    Check(!inv.TryConsumeAmmo(type, cap + 1) && inv.Count(type) == cap, "No partial spend " + type);
                    Check(inv.TryConsumeAmmo(type, cap) && inv.Count(type) == 0, "Spend all " + type);
                    Check(!inv.TryConsumeAmmo(type, 1) && !inv.TryConsumeAmmo(type, 0) && !inv.TryConsumeAmmo(type, -1), "No negative/empty spend " + type);
                }
                Check(inv.TryGrantBackpack() && !inv.TryGrantBackpack() && inv.Capacity(AmmoType.Bullets) == 400 && inv.Capacity(AmmoType.Cells) == 600, "Backpack once");
                Check(inv.TryGrantWeapon(WeaponId.Shotgun) && inv.HasWeapon(WeaponId.Shotgun) && !inv.TryGrantWeapon(WeaponId.Shotgun), "Weapon possession");
                Check(inv.TryGrantKeys(KeyId.BlueCard) && inv.TryGrantKeys(KeyId.RedSkull) && inv.HasKeys(KeyId.BlueCard | KeyId.RedSkull) && !inv.HasKeys(KeyId.YellowCard) && !inv.TryGrantKeys(KeyId.BlueCard), "Keys");
                Check(!inv.TryGrantKeys((KeyId)128) && !inv.TryGrantWeapon((WeaponId)100) && inv.AddAmmo((AmmoType)99, 1) == 0 && !inv.TryConsumeAmmo((AmmoType)(-1), 1), "Unknown enums");
                inv.ClearKeys(); Check(inv.Keys == KeyId.None && inv.HasKeys(KeyId.None), "Clear keys");
                inv.ResetState(); Check(inv.Count(AmmoType.Bullets) == 50 && !inv.HasBackpack && !inv.HasWeapon(WeaponId.Shotgun), "Reset inventory");
                foreach (ArmorKind kind in new[]{ArmorKind.None, ArmorKind.Green, ArmorKind.Blue})
                for (int amount = 0; amount <= 250; amount++)
                for (int points = 0; points <= 200; points += 10)
                {
                    int expected = Math.Min(points, kind == ArmorKind.Green ? amount / 3 : kind == ArmorKind.Blue ? amount / 2 : 0);
                    Check(Armor.CalculateAbsorption(amount, points, kind) == expected, "Absorption grid");
                }
                Debug.Log("[UKUBOLA P04] SURVIVAL SELF-TESTS PASSED | " + count + " checks.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P04] SELF-TESTS FAILED after " + count + " checks: " + ex.Message); }
            finally { if (go) UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
