using Ukubola.Combat;
using Ukubola.Player;

namespace Ukubola.World
{
    // All state changes go through Phase 04. No duplicated health/ammo state.
    public static class PickupEffect
    {
        public static bool TryApply(PickupDefinition definition, Health health, PlayerInventory inventory)
        {
            if (!definition || !definition.IsRuleValid || !health || health.IsDead || !inventory || inventory.gameObject != health.gameObject) return false;
            switch (definition.Kind)
            {
                case PickupKind.Powerup: var power=health.GetComponent<Powerup>();if(!power)return false;bool granted=power.Grant(definition.Powerup,definition.Amount);int healed=definition.Powerup==PowerupKind.Berserk?health.Heal(100):0;return granted||healed>0;
                case PickupKind.Health: return health.Heal(definition.Amount) > 0;
                case PickupKind.Armor: return health.Armor && health.Armor.TryEquip(definition.Armor);
                case PickupKind.Ammo: return inventory.AddAmmo(definition.Ammo, definition.Amount) > 0;
                case PickupKind.Key: return inventory.TryGrantKeys(definition.Key);
                case PickupKind.Weapon:
                    bool weaponGranted = inventory.TryGrantWeapon(definition.Weapon);
                    int ammoGranted = inventory.AddAmmo(definition.Ammo, definition.Amount);
                    return weaponGranted || ammoGranted > 0;
                case PickupKind.Backpack:
                    bool expanded = inventory.TryGrantBackpack();
                    // Classic backpack includes one small unit of each ammunition type.
                    int gained = inventory.AddAmmo(AmmoType.Bullets, 10);
                    gained += inventory.AddAmmo(AmmoType.Shells, 4);
                    gained += inventory.AddAmmo(AmmoType.Rockets, 1);
                    gained += inventory.AddAmmo(AmmoType.Cells, 20);
                    return expanded || gained > 0;
                default: return false;
            }
        }
    }
}
