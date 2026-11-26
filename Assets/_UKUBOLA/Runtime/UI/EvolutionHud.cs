using UnityEngine;
using UnityEngine.UI;
using Ukubola.Combat;
using Ukubola.Player;

namespace Ukubola.UI
{
    public sealed class EvolutionHud : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private WeaponController weapons;
        [SerializeField] private RawImage[] ammo, hp, armor;
        [SerializeField] private Text message, keys;
        public void Configure(Health life, WeaponController guns, RawImage[] bullets, RawImage[] healthDigits, RawImage[] armorDigits, Text prompt, Text cards)
        { health = life; weapons = guns; ammo = bullets; hp = healthDigits; armor = armorDigits; message = prompt; keys = cards; }
        private static void Digits(RawImage[] images, int value)
        {
            string text = Mathf.Clamp(value, 0, 999).ToString().PadLeft(images.Length, ' ');
            for (int i = 0; i < images.Length; i++) { images[i].enabled = text[i] != ' '; if (images[i].enabled) images[i].uvRect = new Rect((text[i] - '0') / 16f, 0, 1f / 16f, 1); }
        }
        private void LateUpdate()
        {
            Digits(hp, health.Current); Digits(armor, health.Armor ? health.Armor.Points : 0);
            var bag = health.GetComponent<PlayerInventory>(); var gun = weapons.Selected;
            Digits(ammo, gun && gun.UsesAmmo ? bag.Count(gun.Ammo) : 0);
            if (keys) keys.text = "ACCESS\n" + ((bag.Keys & KeyId.BlueCard) != 0 ? "[B] " : "[ ] ") + ((bag.Keys & KeyId.YellowCard) != 0 ? "[Y] " : "[ ] ") + ((bag.Keys & KeyId.RedCard) != 0 ? "[R]" : "[ ]");
            var interaction = health.GetComponent<Ukubola.World.PlayerInteractor>();
            if (message) message.text = interaction ? interaction.LastMessage : "E / SPACE : USE";
        }
    }
}
