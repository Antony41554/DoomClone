using UnityEngine;

namespace Ukubola.Combat
{
    public enum ArmorKind { None, Green, Blue }

    [DisallowMultipleComponent]
    public sealed class Armor : MonoBehaviour
    {
        public int Points { get; private set; }
        public ArmorKind Kind { get; private set; }
        public static int Capacity(ArmorKind kind) => kind == ArmorKind.Green ? 100 : kind == ArmorKind.Blue ? 200 : 0;
        public static int CalculateAbsorption(int amount, int points, ArmorKind kind)
        {
            if (amount <= 0 || points <= 0) return 0;
            int absorbed = kind == ArmorKind.Green ? amount / 3 : kind == ArmorKind.Blue ? amount / 2 : 0;
            return Mathf.Min(points, absorbed);
        }
        public bool TryEquip(ArmorKind kind)
        {
            int capacity = Capacity(kind);
            if (capacity <= Points) return false;
            Points = capacity; Kind = kind; return true;
        }
        internal int Absorb(int amount)
        {
            int used = CalculateAbsorption(amount, Points, Kind);
            Points -= used;
            if (Points == 0) Kind = ArmorKind.None;
            return used;
        }
        public void RestoreState(ArmorKind kind, int points)
        {
            Points = Mathf.Clamp(points, 0, Capacity(kind)); Kind = Points > 0 ? kind : ArmorKind.None;
        }
        public void ResetState() { Points = 0; Kind = ArmorKind.None; }
    }
}
