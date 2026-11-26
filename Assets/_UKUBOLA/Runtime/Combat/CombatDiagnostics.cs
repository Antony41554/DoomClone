using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Ukubola.Player;

namespace Ukubola.Combat
{
    // Test-only controls. The gameplay weapon system does not depend on this component.
    public sealed class CombatDiagnostics : MonoBehaviour
    {
        [SerializeField] private WeaponController weapons;
        [SerializeField] private Health player, target;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Renderer targetVisual;
        [SerializeField] private Text readout;
        [SerializeField] private Projectile projectilePrefab;
        private readonly List<Projectile> launched = new List<Projectile>();
        private MaterialPropertyBlock block;
        private bool? previousDead;
        public WeaponController Weapons => weapons;
        public Health Player => player;
        public Health Target => target;
        public PlayerInventory Inventory => inventory;
        public Renderer TargetVisual => targetVisual;
        public Text Readout => readout;
        public Projectile ProjectilePrefab => projectilePrefab;
        public void Configure(WeaponController controller, Health hp, Health dummy, PlayerInventory bag, Renderer visual, Text text, Projectile projectile)
        { weapons = controller; player = hp; target = dummy; inventory = bag; targetVisual = visual; readout = text; projectilePrefab = projectile; }
        private void Start()
        {
            if (!weapons || !player || !target || !inventory || !targetVisual || !readout || !projectilePrefab)
            { Debug.LogError("[UKUBOLA P05] Invalid diagnostics references.", this); enabled = false; return; }
            block = new MaterialPropertyBlock(); ResetLab();
        }
        private void ResetLab()
        {
            foreach (var p in launched) if (p) { p.enabled = false; Destroy(p.gameObject); }
            launched.Clear();
            player.ResetState(); target.ResetState(); inventory.ResetState(); inventory.TryGrantWeapon(WeaponId.Shotgun); inventory.AddAmmo(AmmoType.Shells, 20);
            weapons.ResetDiagnostic(); previousDead = null;
        }
        private void Update()
        {
            var k = Keyboard.current;
            if (k == null || !Application.isFocused) return;
            if (k.f9Key.wasPressedThisFrame) ResetLab();
            else if (k.f10Key.wasPressedThisFrame) readout.transform.parent.gameObject.SetActive(!readout.transform.parent.gameObject.activeSelf);
            else if (k.f2Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(60));
            else if (!player.IsDead)
            {
                if (k.f4Key.wasPressedThisFrame)
                {
                    launched.RemoveAll(p => !p);
                    var projectile = weapons.LaunchDiagnostic(projectilePrefab); if (projectile) launched.Add(projectile);
                }
                if (k.f6Key.wasPressedThisFrame) { inventory.AddAmmo(AmmoType.Bullets, 40); inventory.AddAmmo(AmmoType.Shells, 20); }
                if (k.f7Key.wasPressedThisFrame && weapons.Selected)
                { var ammo = weapons.Selected.Ammo; int count = inventory.Count(ammo); if (count > 0) inventory.TryConsumeAmmo(ammo, count); }
            }
        }
        private void LateUpdate()
        {
            if (previousDead != target.IsDead)
            {
                targetVisual.GetPropertyBlock(block); block.SetColor("_BaseColor", target.IsDead ? new Color(.18f,.18f,.18f) : new Color(.95f,.32f,.15f)); targetVisual.SetPropertyBlock(block); previousDead = target.IsDead;
            }
            readout.text = $"COMBAT LAB | {(player.IsDead ? "DEAD" : "ALIVE")} | HP {player.Current}\n{(weapons.Selected ? weapons.Selected.Weapon.ToString() : "-")} | Bullets {inventory.Count(AmmoType.Bullets)} | Shells {inventory.Count(AmmoType.Shells)}\nTarget HP {target.Current} | Shots {weapons.Shots} | Hits {weapons.DamageHits}\n2 pistol | 3 shotgun | F4 projectile\nF2 damage | F6 ammo | F7 empty | F9 reset | F10 hide\n{weapons.LastResult}";
        }
    }
}
