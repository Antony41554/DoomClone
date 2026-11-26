using UnityEngine;

namespace Ukubola.Combat
{
    public enum DamageType { Generic, Hitscan, Projectile, Melee, Environmental, Explosion }

    public readonly struct DamageInfo
    {
        public int Amount { get; }
        public GameObject Source { get; }
        public DamageType Type { get; }
        public Vector3 Point { get; }
        public DamageInfo(int amount, GameObject source = null, DamageType type = DamageType.Generic, Vector3 point = default)
        { Amount = amount; Source = source; Type = type; Point = point; }
    }

    public readonly struct DamageResult
    {
        public int Absorbed { get; }
        public int HealthLost { get; }
        public bool Killed { get; }
        public bool Applied => Absorbed > 0 || HealthLost > 0;
        public DamageResult(int absorbed, int healthLost, bool killed)
        { Absorbed = absorbed; HealthLost = healthLost; Killed = killed; }
    }

    public interface IDamageable
    {
        bool IsDead { get; }
        DamageResult TakeDamage(DamageInfo damage);
    }
}
