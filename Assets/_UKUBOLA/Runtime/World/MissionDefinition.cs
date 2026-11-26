using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ukubola.World
{
    [Serializable] public sealed class MissionSector
    {
        public string key, label;
        public Vector2 center, size = new Vector2(12, 12);
        public float floor, ceiling = 4.5f;
        public int plant;
        public bool Contains(Vector3 p, float margin = 0) => Mathf.Abs(p.x - center.x) <= size.x / 2 + margin && Mathf.Abs(p.z - center.y) <= size.y / 2 + margin && p.y >= floor - .5f && p.y <= ceiling + margin;
    }
    [Serializable] public sealed class MissionPortal
    {
        public string key, from, to, requiredFlag;
        public Vector3 position;
        public float width = 3;
    }
    [Serializable] public sealed class MissionPlacement
    {
        public string key, prefab, sector;
        public Vector3 position;
        public float yaw;
        public bool optional;
    }
    [CreateAssetMenu(menuName = "UKUBOLA/Evolution/Mission")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public string missionId, label;
        public int revision = 1, sequence;
        public Vector3 spawn;
        public float spawnYaw;
        public MissionSector[] sectors = Array.Empty<MissionSector>();
        public MissionPortal[] portals = Array.Empty<MissionPortal>();
        public MissionPlacement[] placements = Array.Empty<MissionPlacement>();
        public string[] objectives = Array.Empty<string>();
        public Bounds Bounds
        {
            get
            {
                var bounds = new Bounds(spawn, Vector3.one);
                foreach (var s in sectors) bounds.Encapsulate(new Bounds(new Vector3(s.center.x, (s.floor + s.ceiling) * .5f, s.center.y), new Vector3(s.size.x, s.ceiling - s.floor, s.size.y)));
                bounds.Expand(2f); return bounds;
            }
        }
        public void Validate()
        {
            void Need(bool value, string why) { if (!value) throw new InvalidOperationException(why); }
            Need(!string.IsNullOrWhiteSpace(missionId) && revision > 0 && sequence >= 0 && sequence <= 2, "Invalid mission identity/revision.");
            Need(sectors != null && sectors.Length > 0 && portals != null && placements != null && objectives != null, "Missing mission collections.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in sectors)
            {
                Need(s != null && !string.IsNullOrWhiteSpace(s.key) && keys.Add(s.key), "Duplicate/missing sector key.");
                Need(Finite(s.center.x) && Finite(s.center.y) && Finite(s.size.x) && Finite(s.size.y) && s.size.x >= 2.4f && s.size.y >= 2.4f && Finite(s.floor) && Finite(s.ceiling) && s.ceiling - s.floor >= 2.4f, "Invalid sector volume: " + s.key);
            }
            Need(Finite(spawn) && sectors.Any(s => s.Contains(spawn)), "Spawn outside sectors.");
            var portalKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in portals) Need(p != null && !string.IsNullOrWhiteSpace(p.key) && portalKeys.Add(p.key) && keys.Contains(p.from) && keys.Contains(p.to) && p.from != p.to && Finite(p.position) && Finite(p.width) && p.width >= 1.2f, "Invalid portal.");
            var placementKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in placements) Need(p != null && !string.IsNullOrWhiteSpace(p.key) && placementKeys.Add(p.key) && !string.IsNullOrWhiteSpace(p.prefab) && keys.Contains(p.sector) && Finite(p.position) && Finite(p.yaw) && sectors.Single(s => s.key == p.sector).Contains(p.position, .25f), "Invalid placement.");
            var reached = new HashSet<string> { sectors[0].key }; var frontier = new Queue<string>(); frontier.Enqueue(sectors[0].key);
            while (frontier.Count > 0) { string current = frontier.Dequeue(); foreach (var p in portals.Where(p => p.from == current || p.to == current)) { string other = p.from == current ? p.to : p.from; if (reached.Add(other)) frontier.Enqueue(other); } }
            Need(reached.Count == sectors.Length, "Disconnected mission sectors.");
            Need(objectives.All(x => !string.IsNullOrWhiteSpace(x)) && objectives.Distinct(StringComparer.Ordinal).Count() == objectives.Length, "Invalid objective keys.");
        }
        public static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        public static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    }
}
