using UnityEngine;
using Ukubola.Player;
using Ukubola.Combat;
namespace Ukubola.World
{
    public readonly struct InteractionContext
    {
        public readonly PlayerInventory Inventory;
        public readonly Health Health;
        public InteractionContext(PlayerInventory inventory, Health health) { Inventory=inventory; Health=health; }
    }
    public interface IInteractable { string Interact(InteractionContext context); }
    public static class KeyRequirement
    {
        public static bool Allows(KeyId owned, KeyId required, bool any)
        { return required==KeyId.None || (any ? (owned & required)!=0 : (owned & required)==required); }
    }
}
