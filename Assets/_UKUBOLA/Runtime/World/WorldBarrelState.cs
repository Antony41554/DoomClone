using System;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Persistence;

namespace Ukubola.World
{
    [RequireComponent(typeof(ExplosiveBarrel))]
    public sealed class WorldBarrelState : PersistentBehaviour
    {
        [Serializable] private sealed class Data { public int health; }
        public override string StateKind => "barrel";
        public override int RestoreOrder => 60;
        private Health Health => GetComponent<Health>();
        private void OnEnable() { Health.Died += Died; Health.Changed += Refresh; Refresh(Health); }
        private void OnDisable() { if (Health) { Health.Died -= Died; Health.Changed -= Refresh; } }
        private void Refresh(Health actor) { foreach(var renderer in GetComponentsInChildren<Renderer>(true))renderer.enabled=!actor.IsDead; }
        private void Died(Health actor, DamageInfo damage) { Refresh(actor); Emit(WorldSignal.Killed); }
        public override EntitySnapshot CaptureEntity() => State(new Data { health = Health.Current });
        public override void ValidateEntity(EntitySnapshot state)
        {
            var data = Read<Data>(state);
            if (data == null || data.health < 0 || data.health > Health.MaxHealth)
                throw new InvalidOperationException("Invalid barrel health.");
        }
        public override void RestoreEntity(EntitySnapshot state)
        {
            ValidateEntity(state);
            // RestoreBarrel changes collision/appearance without calling the explosion handler.
            GetComponent<ExplosiveBarrel>().RestoreBarrel(Read<Data>(state).health);
            Refresh(Health);
        }
    }
}
