using System;
using UnityEngine;

namespace Ukubola.Player
{
    public enum AmmoType { Bullets, Shells, Rockets, Cells }
    public enum WeaponId { Fist, Chainsaw, Pistol, Shotgun, Chaingun, RocketLauncher, PlasmaRifle, EnergyCannon }
    [Flags] public enum KeyId { None = 0, BlueCard = 1, YellowCard = 2, RedCard = 4, BlueSkull = 8, YellowSkull = 16, RedSkull = 32 }

    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        private readonly int[] ammo = new int[4];
        private readonly bool[] weapons = new bool[8];
        private bool initialized;
        private bool backpack;
        private KeyId keys;
        public event Action<PlayerInventory> Changed;
        public bool HasBackpack { get { EnsureInitialized(); return backpack; } }
        public KeyId Keys { get { EnsureInitialized(); return keys; } }
        private void Awake() => EnsureInitialized();
        private void EnsureInitialized() { if (!initialized) Initialize(); }
        private void Initialize()
        {
            Array.Clear(ammo, 0, ammo.Length); Array.Clear(weapons, 0, weapons.Length);
            ammo[(int)AmmoType.Bullets] = 50; weapons[(int)WeaponId.Fist] = true; weapons[(int)WeaponId.Pistol] = true;
            backpack = false; keys = KeyId.None; initialized = true;
        }
        public static int BaseCapacity(AmmoType type)
        {
            switch (type) { case AmmoType.Bullets: return 200; case AmmoType.Shells: return 50; case AmmoType.Rockets: return 50; case AmmoType.Cells: return 300; default: return 0; }
        }
        public int Capacity(AmmoType type) { EnsureInitialized(); return BaseCapacity(type) * (backpack ? 2 : 1); }
        public int Count(AmmoType type) { EnsureInitialized(); return BaseCapacity(type) > 0 ? ammo[(int)type] : 0; }
        public int AddAmmo(AmmoType type, int amount)
        {
            EnsureInitialized(); int cap = Capacity(type);
            if (cap == 0 || amount <= 0) return 0;
            int index = (int)type, gained = Mathf.Min(amount, cap - ammo[index]);
            if (gained <= 0) return 0;
            ammo[index] += gained; Changed?.Invoke(this); return gained;
        }
        public bool TryConsumeAmmo(AmmoType type, int amount)
        {
            EnsureInitialized();
            if (BaseCapacity(type) == 0 || amount <= 0 || ammo[(int)type] < amount) return false;
            ammo[(int)type] -= amount; Changed?.Invoke(this); return true;
        }
        public bool TryGrantBackpack()
        {
            EnsureInitialized(); if (backpack) return false;
            backpack = true; Changed?.Invoke(this); return true;
        }
        public bool HasWeapon(WeaponId weapon) { EnsureInitialized(); int i = (int)weapon; return i >= 0 && i < weapons.Length && weapons[i]; }
        public bool TryGrantWeapon(WeaponId weapon)
        {
            EnsureInitialized(); int i = (int)weapon;
            if (i < 0 || i >= weapons.Length || weapons[i]) return false;
            weapons[i] = true; Changed?.Invoke(this); return true;
        }
        private const KeyId AllKeys = KeyId.BlueCard | KeyId.YellowCard | KeyId.RedCard | KeyId.BlueSkull | KeyId.YellowSkull | KeyId.RedSkull;
        private static bool ValidKeys(KeyId value) => value != KeyId.None && (value & ~AllKeys) == KeyId.None;
        public bool HasKeys(KeyId required) { EnsureInitialized(); return required == KeyId.None || (ValidKeys(required) && (keys & required) == required); }
        public bool TryGrantKeys(KeyId value)
        {
            EnsureInitialized(); if (!ValidKeys(value) || HasKeys(value)) return false;
            keys |= value; Changed?.Invoke(this); return true;
        }
        public void ClearKeys() { EnsureInitialized(); if (keys == KeyId.None) return; keys = KeyId.None; Changed?.Invoke(this); }
        public void ResetState() { Initialize(); Changed?.Invoke(this); }
    }
}
