using System;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.World
{
    [Serializable] public sealed class WorldAction
    {
        public PersistentBehaviour target;
        public WorldVerb verb;
        public float value;
        public string conditionFlag;
    }
    public sealed class ActionBinding : PersistentBehaviour
    {
        [Serializable] private sealed class Data { public bool fired; }
        [SerializeField] private PersistentBehaviour source;
        [SerializeField] private WorldSignal signal;
        [SerializeField] private bool once = true;
        [SerializeField] private WorldAction[] actions = Array.Empty<WorldAction>();
        private bool fired, dispatching;
        public override string StateKind => "binding";
        public PersistentBehaviour Source => source;
        public WorldAction[] Actions => actions;
        public bool Fired => fired;
        public void Configure(PersistentBehaviour origin, WorldSignal eventKind, bool oneShot, WorldAction[] targets)
        {
            if (source) source.Signalled -= Handle;
            source = origin; signal = eventKind; once = oneShot; actions = targets;
            if (isActiveAndEnabled && source) source.Signalled += Handle;
        }
        private void OnEnable() { if (source) source.Signalled += Handle; }
        private void OnDisable() { if (source) source.Signalled -= Handle; }
        public void ValidateBinding()
        {
            if (!source || actions == null || actions.Length == 0) throw new InvalidOperationException("Disconnected action binding: " + name);
            foreach (var a in actions) if (a == null || !a.target || !(a.target is IWorldActionTarget target) || !target.Accepts(a.verb) || !MissionDefinition.Finite(a.value)) throw new InvalidOperationException("Invalid action destination: " + name);
        }
        private void Handle(WorldSignal incoming)
        {
            if (incoming != signal || dispatching || once && fired || Context && Context.Restoring) return;
            ValidateBinding(); dispatching = true;
            try
            {
                foreach (var a in actions)
                {
                    if (!string.IsNullOrEmpty(a.conditionFlag) && (!Context || !Context.Flag(a.conditionFlag))) continue;
                    ((IWorldActionTarget)a.target).Execute(a.verb, a.value);
                }
                fired = true;
            }
            finally { dispatching = false; }
        }
        public override EntitySnapshot CaptureEntity() => State(new Data { fired = fired });
        public override void ValidateEntity(EntitySnapshot state) { if (Read<Data>(state) == null) throw new InvalidOperationException("Missing binding state."); }
        public override void RestoreEntity(EntitySnapshot state) { ValidateEntity(state); fired = Read<Data>(state).fired; }
    }
}
