using System;
using UnityEngine;

namespace Ukubola.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField] private Armor armor;
        private int current;
        private bool initialized;
        public int MaxHealth => Mathf.Max(1, maxHealth);
        public Armor Armor => armor;
        public int Current { get { EnsureInitialized(); return current; } }
        public bool IsDead => Current == 0;
        public event Action<Health, DamageInfo, DamageResult> Damaged;
        public event Action<Health> Changed;
        public event Action<Health, DamageInfo> Died;
        public event Action<Health> Revived;

        // Editor configuration only. Runtime state is never stored in a shared asset.
        public void Configure(int maximum, Armor protection)
        {
            maxHealth = Mathf.Max(1, maximum); armor = protection;
            if (!initialized) current = MaxHealth;
        }
        private void Awake() => EnsureInitialized();
        private void EnsureInitialized()
        {
            if (initialized) return;
            current = MaxHealth; initialized = true;
        }
        public DamageResult TakeDamage(DamageInfo damage)
        {
            EnsureInitialized();
            if (current == 0 || damage.Amount <= 0) return default;
            var power=GetComponent<Powerup>(); if(power&&power.Invulnerable)return default;
            int absorbed = armor ? armor.Absorb(damage.Amount) : 0;
            int lost = Mathf.Min(current, damage.Amount - absorbed);
            current -= lost;
            var result = new DamageResult(absorbed, lost, current == 0);
            Damaged?.Invoke(this, damage, result);
            Changed?.Invoke(this);
            if (result.Killed) Died?.Invoke(this, damage);
            return result;
        }
        public int Heal(int amount)
        {
            EnsureInitialized();
            if (current == 0 || amount <= 0) return 0;
            int gained = Mathf.Min(amount, MaxHealth - current);
            if (gained <= 0) return 0;
            current += gained; Changed?.Invoke(this); return gained;
        }
        // Campaign entry restores a living snapshot without producing damage or death events.
        public void RestoreLiving(int value)
        {
            EnsureInitialized(); if (IsDead) throw new InvalidOperationException("Restore requires a living player.");
            current = Mathf.Clamp(value, 1, MaxHealth); Changed?.Invoke(this);
        }
        // Save restoration must not fire death/damage events (especially barrel explosions).
        public void RestoreSavedHealth(int value)
        {
            EnsureInitialized(); current = Mathf.Clamp(value, 0, MaxHealth); Changed?.Invoke(this);
        }
        // Explicit reset for diagnostics/future respawn, not a normal healing operation.
        public void ResetState()
        {
            var power=GetComponent<Powerup>();if(power)power.Clear();
            bool dead = IsDead;
            if (armor) armor.ResetState();
            current = MaxHealth; Changed?.Invoke(this);
            if (dead) Revived?.Invoke(this);
        }
    }
}
