using UnityEngine;

namespace Ukubola.Player
{
    // Recorded intent follows the normal motor, weapon and interaction paths.
    public interface IPlayerCommands
    {
        bool Active{get;}
        Vector2 Move{get;}
        Vector2 Look{get;}
        bool Run{get;}
        bool FireHeld{get;}
        bool UsePressed{get;}
        int WeaponSlot{get;}
    }
}
