using System;
using System.Linq;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.World
{
    // Members exist, have IDs and initialize at scene load. An event releases them;
    // it never instantiates another copy of an enemy when a saved encounter resumes.
    public sealed class WorldEncounter : PersistentBehaviour, IWorldActionTarget, IEntityRestoreComplete
    {
        [Serializable] private sealed class Data { public bool active, completed; }
        [SerializeField] private WorldActorState[] members = Array.Empty<WorldActorState>();
        [SerializeField] private bool active;
        private bool completed;
        public override string StateKind => "encounter";
        public override int RestoreOrder => 250;
        public bool Active => active;
        public bool Completed => completed;
        public void Configure(WorldActorState[] actors, bool initiallyActive)
        {
            members = actors; active = initiallyActive; completed = false;
            ValidateConfiguration(); Apply();
        }
        private void ValidateConfiguration()
        {
            if (members == null || members.Length == 0 || members.Any(a => !a) || members.Distinct().Count() != members.Length)
                throw new InvalidOperationException("Encounter requires unique existing actors.");
        }
        private void Awake() { ValidateConfiguration(); Apply(); }
        private void Apply() { foreach (var actor in members) actor.Brain.Suspended = !active || completed; }
        private void Update()
        {
            if (!active || completed || Context && Context.Restoring || Time.timeScale <= 0) return;
            if (!members.All(a => a.Health.IsDead)) return;
            completed = true; Apply(); Emit(WorldSignal.Arrived);
        }
        public bool Accepts(WorldVerb verb) => verb == WorldVerb.StartEncounter;
        public void Execute(WorldVerb verb, float value)
        {
            if (!Accepts(verb)) throw new InvalidOperationException("Unsupported encounter command.");
            if (active || completed) return;
            active = true; Apply(); Emit(WorldSignal.Activated);
        }
        public override EntitySnapshot CaptureEntity() => State(new Data { active = active, completed = completed });
        public override void ValidateEntity(EntitySnapshot state)
        {
            var data = Read<Data>(state);
            if (data == null || data.completed && !data.active) throw new InvalidOperationException("Invalid encounter state.");
        }
        public override void RestoreEntity(EntitySnapshot state)
        {
            ValidateEntity(state); var data = Read<Data>(state);
            active = data.active; completed = data.completed; Apply();
        }
        public void FinishRestore()
        {
            if (completed && members.Any(a => !a.Health.IsDead))
                throw new InvalidOperationException("Completed encounter contains a living actor.");
            Apply();
        }
    }
}
