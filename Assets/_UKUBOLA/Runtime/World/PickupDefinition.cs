using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;

namespace Ukubola.World
{
    public enum PickupKind { Health, Armor, Ammo, Weapon, Key, Backpack, Powerup }

    public sealed class PickupDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private PickupKind kind;
        [SerializeField] private int amount;
        [SerializeField] private AmmoType ammo;
        [SerializeField] private ArmorKind armor;
        [SerializeField] private WeaponId weapon;
        [SerializeField] private KeyId key;
        [SerializeField] private Sprite sprite;
        [SerializeField] private PowerupKind powerup;
        public PowerupKind Powerup => powerup;
        public void ConfigurePowerup(PowerupKind value)=>powerup=value;
        public string DisplayName => displayName;
        public PickupKind Kind => kind;
        public int Amount => amount;
        public AmmoType Ammo => ammo;
        public ArmorKind Armor => armor;
        public WeaponId Weapon => weapon;
        public KeyId Key => key;
        public Sprite Sprite => sprite;
        public bool IsRuleValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(displayName)) return false;
                switch (kind)
                {
                    case PickupKind.Health: return amount > 0;
                    case PickupKind.Armor: return Ukubola.Combat.Armor.Capacity(armor) > 0;
                    case PickupKind.Ammo: return amount > 0 && PlayerInventory.BaseCapacity(ammo) > 0;
                    // Shared rules: old weapons plus Phase 12 arsenal.
                    case PickupKind.Weapon: return weapon==WeaponId.Chainsaw?amount==0:(weapon==WeaponId.Shotgun&&ammo==AmmoType.Shells|| (weapon==WeaponId.Pistol||weapon==WeaponId.Chaingun)&&ammo==AmmoType.Bullets||weapon==WeaponId.RocketLauncher&&ammo==AmmoType.Rockets||(weapon==WeaponId.PlasmaRifle||weapon==WeaponId.EnergyCannon)&&ammo==AmmoType.Cells)&&amount>0;
                    case PickupKind.Powerup:return amount>0&&System.Enum.IsDefined(typeof(PowerupKind),powerup);
                    case PickupKind.Key: int k = (int)key; return k > 0 && k <= 32 && (k & (k - 1)) == 0;
                    case PickupKind.Backpack: return true;
                    default: return false;
                }
            }
        }
        public bool IsValid => IsRuleValid && sprite;
        public void Configure(string label, PickupKind type, Sprite image, int count = 0, AmmoType ammoType = AmmoType.Bullets, ArmorKind armorType = ArmorKind.None, WeaponId weaponType = WeaponId.Fist, KeyId keyType = KeyId.None)
        { displayName = label; kind = type; sprite = image; amount = count; ammo = ammoType; armor = armorType; weapon = weaponType; key = keyType; }
    }
}
