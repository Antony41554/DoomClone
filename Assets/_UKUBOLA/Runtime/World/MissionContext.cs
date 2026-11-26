using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.World
{
    [DefaultExecutionOrder(-200)]
    public sealed class MissionContext : MonoBehaviour
    {
        [SerializeField] private MissionDefinition definition;
        private readonly Dictionary<string, IPersistentEntity> entities = new Dictionary<string, IPersistentEntity>(StringComparer.Ordinal);
        private readonly HashSet<string> flags = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> explored = new HashSet<string>(StringComparer.Ordinal);
        public MissionDefinition Definition => definition;
        public bool Restoring { get; private set; }
        public int EntityCount => entities.Count;
        public string[] Flags => flags.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        public string[] Explored => explored.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        public void Configure(MissionDefinition value) => definition = value;
        private void Awake() { RebuildRegistry(); }
        public bool Flag(string key) => flags.Contains(key);
        public void SetFlag(string key, bool value) { if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Flag key required."); if (value) flags.Add(key); else flags.Remove(key); }
        public void Explore(string key) { if (definition.sectors.Any(s => s.key == key)) explored.Add(key); }
        public void RestoreFlags(string[] values, string[] visited)
        {
            flags.Clear(); foreach (var value in values) flags.Add(value);
            explored.Clear(); foreach (var value in visited) explored.Add(value);
        }
        public void RebuildRegistry()
        {
            if (!definition) throw new InvalidOperationException("Mission definition missing."); definition.Validate(); entities.Clear();
            foreach (var entity in gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PersistentBehaviour>(true)))
            {
                if (!Guid.TryParseExact(entity.PersistentId, "N", out _) || !entities.TryAdd(entity.PersistentId, entity)) throw new InvalidOperationException("Duplicate or invalid persistent identity: " + entity.name);
                entity.BindContext(this);
            }
            var bindings = gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ActionBinding>(true)).ToArray();
            foreach (var b in bindings)
            {
                b.ValidateBinding();
                if (!entities.ContainsKey(b.Source.PersistentId) || b.Actions.Any(a => !entities.ContainsKey(a.target.PersistentId))) throw new InvalidOperationException("Binding refers outside this mission.");
            }
            var edges = bindings.GroupBy(b => b.Source.PersistentId).ToDictionary(g => g.Key, g => g.SelectMany(b => b.Actions).Select(a => a.target.PersistentId).ToArray());
            var done = new HashSet<string>(); var visiting = new HashSet<string>();
            void Visit(string id)
            {
                if (done.Contains(id)) return;
                if (!visiting.Add(id)) throw new InvalidOperationException("Action cycle detected.");
                if (edges.TryGetValue(id, out var targets)) foreach (var target in targets) Visit(target);
                visiting.Remove(id); done.Add(id);
            }
            foreach (string id in edges.Keys) Visit(id);
        }
        public EntitySnapshot[] CaptureEntities() => entities.Values.OrderBy(e => e.PersistentId, StringComparer.Ordinal).Select(e => e.CaptureEntity()).ToArray();
        public void ValidateEntities(EntitySnapshot[] states)
        {
            if (states == null || states.Length != entities.Count || states.Any(s => s == null) || states.Select(s => s.id).Distinct(StringComparer.Ordinal).Count() != states.Length) throw new InvalidOperationException("Snapshot entity set differs from mission.");
            foreach (var state in states) { if (!entities.TryGetValue(state.id, out var entity)) throw new InvalidOperationException("Unknown entity identity."); entity.ValidateEntity(state); }
        }
        public void RestoreEntities(EntitySnapshot[] states)
        {
            ValidateEntities(states); var previous = CaptureEntities(); Restoring = true;
            void Apply(EntitySnapshot[] snapshots)
            {
                foreach (var state in snapshots.OrderBy(s => entities[s.id].RestoreOrder).ThenBy(s => s.id, StringComparer.Ordinal)) entities[state.id].RestoreEntity(state);
            }
            try { Apply(states); }
            catch { Apply(previous); throw; }
            finally { Restoring = false; }
        }
    }
}
