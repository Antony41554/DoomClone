using System;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.World
{
    public sealed class WorldRelay : PersistentBehaviour, IInteractable, IWorldActionTarget
    {
        [Serializable] private sealed class Data { public bool active, powered, used; }
        [SerializeField] private bool active, powered = true, singleUse;
        private bool used;
        public bool Active => active;
        public bool Powered => powered;
        public override string StateKind => "relay";
        public event Action Changed;
        public void Configure(bool initial, bool hasPower, bool once) { active = initial; powered = hasPower; singleUse = once; }
        public string Interact(InteractionContext context)
        {
            if (!context.Health || context.Health.IsDead) return "Unavailable";
            if (!powered) return "NO POWER";
            if (singleUse && used) return "ALREADY ACTIVATED";
            used = true; Execute(WorldVerb.On, 0); return "ACTIVATED";
        }
        public bool Accepts(WorldVerb verb) => verb == WorldVerb.On || verb == WorldVerb.Off || verb == WorldVerb.Toggle;
        public void Execute(WorldVerb verb, float value)
        {
            if (!Accepts(verb)) throw new InvalidOperationException("Unsupported relay command.");
            bool next = verb == WorldVerb.Toggle ? !active : verb == WorldVerb.On;
            if (active == next) return; active = next; Changed?.Invoke(); Emit(WorldSignal.Activated);
        }
        public void SetPower(bool value) { powered = value; Changed?.Invoke(); }
        public override EntitySnapshot CaptureEntity() => State(new Data { active = active, powered = powered, used = used });
        public override void ValidateEntity(EntitySnapshot state) { if (Read<Data>(state) == null) throw new InvalidOperationException("Missing relay state."); }
        public override void RestoreEntity(EntitySnapshot state) { ValidateEntity(state); var d = Read<Data>(state); active = d.active; powered = d.powered; used = d.used; Changed?.Invoke(); }
    }
}
