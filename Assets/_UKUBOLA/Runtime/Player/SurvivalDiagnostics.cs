using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Ukubola.Combat;

namespace Ukubola.Player
{
    // Test-only controls in SurvivalLab. Not a gameplay HUD, weapon or pickup system.
    [DisallowMultipleComponent]
    public sealed class SurvivalDiagnostics : MonoBehaviour
    {
        [SerializeField] private Health player;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Health target;
        [SerializeField] private Renderer targetVisual;
        [SerializeField] private Text readout;
        public Health Player => player;
        public PlayerInventory Inventory => inventory;
        public Health Target => target;
        public Renderer TargetVisual => targetVisual;
        public Text Readout => readout;
        private string last = "Ready";
        private MaterialPropertyBlock block;
        public void Configure(Health hp, PlayerInventory bag, Health dummy, Renderer visual, Text text)
        { player = hp; inventory = bag; target = dummy; targetVisual = visual; readout = text; }
        private void Start()
        {
            if (!player || !player.Armor || !inventory || !target || !targetVisual || !readout)
            { Debug.LogError("[UKUBOLA P04] Missing diagnostic references.", this); enabled = false; return; }
            block = new MaterialPropertyBlock(); Refresh();
        }
        private void Update()
        {
            var k = Keyboard.current;
            if (k == null || !Application.isFocused) return;
            bool changed = true;
            if (k.f9Key.wasPressedThisFrame) { player.ResetState(); inventory.ResetState(); target.ResetState(); last = "Diagnostic reset (position unchanged)"; }
            else if (k.f10Key.wasPressedThisFrame) { readout.transform.parent.gameObject.SetActive(!readout.transform.parent.gameObject.activeSelf); last = "Overlay toggled"; }
            else if (k.f1Key.wasPressedThisFrame) last = Describe(player.TakeDamage(new DamageInfo(20)));
            else if (k.f2Key.wasPressedThisFrame) last = Describe(player.TakeDamage(new DamageInfo(60)));
            else if (k.f3Key.wasPressedThisFrame) last = "Health gained: " + player.Heal(25);
            else if (player.IsDead) changed = false;
            else if (k.f4Key.wasPressedThisFrame) last = "Green armor: " + player.Armor.TryEquip(ArmorKind.Green);
            else if (k.f5Key.wasPressedThisFrame) last = "Blue armor: " + player.Armor.TryEquip(ArmorKind.Blue);
            else if (k.f6Key.wasPressedThisFrame) last = "Bullets gained: " + inventory.AddAmmo(AmmoType.Bullets, 40);
            else if (k.f7Key.wasPressedThisFrame) last = "Consume 10 bullets: " + inventory.TryConsumeAmmo(AmmoType.Bullets, 10);
            else if (k.f8Key.wasPressedThisFrame) last = "Target: " + Describe(target.TakeDamage(new DamageInfo(25, player.gameObject)));
            else changed = false;
            if (changed) { Refresh(); Debug.Log("[UKUBOLA P04] " + last + " | HP " + player.Current + " | Armor " + player.Armor.Points + " | Bullets " + inventory.Count(AmmoType.Bullets)); }
        }
        private static string Describe(DamageResult result) => "HP lost " + result.HealthLost + ", absorbed " + result.Absorbed + (result.Killed ? " | DEAD" : "");
        private void Refresh()
        {
            targetVisual.GetPropertyBlock(block);
            block.SetColor("_BaseColor", target.IsDead ? new Color(.18f,.18f,.18f) : new Color(.95f,.32f,.15f));
            targetVisual.SetPropertyBlock(block);
            readout.text = $"SURVIVAL LAB  |  {(player.IsDead ? "DEAD" : "ALIVE")}\nHP {player.Current}/{player.MaxHealth}   ARMOR {player.Armor.Points} ({player.Armor.Kind})\nBULLETS {inventory.Count(AmmoType.Bullets)}/{inventory.Capacity(AmmoType.Bullets)}   TARGET {target.Current}/{target.MaxHealth}\nF1 dmg 20 | F2 dmg 60 | F3 heal 25\nF4 green | F5 blue | F6 ammo +40 | F7 use 10\nF8 target damage | F9 reset | F10 hide/show\n{last}";
        }
    }
}
