using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ukubola
{
    [DisallowMultipleComponent]
    public sealed class LevelContext : MonoBehaviour
    {
        [SerializeField] private string levelId = "TEST_ARENA";
        private readonly Dictionary<string, EntityId> entities = new Dictionary<string, EntityId>(StringComparer.OrdinalIgnoreCase);
        public string LevelId => levelId;
        public int RegisteredCount => entities.Count;

        private void Start()
        {
            foreach (string error in RebuildRegistry()) Debug.LogError("[UKUBOLA] " + error, this);
        }

        // Scoped to this scene, including inactive objects. Future spawning systems
        // can register/unregister explicitly; this is the Phase 00 snapshot only.
        public List<string> RebuildRegistry()
        {
            entities.Clear();
            var errors = new List<string>();
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            {
                errors.Add("LevelContext does not belong to a loaded scene.");
                return errors;
            }
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var entity in root.GetComponentsInChildren<EntityId>(true))
                {
                    if (!entity.IsValid) { errors.Add("Invalid EntityId: " + entity.name); continue; }
                    if (entities.ContainsKey(entity.Id)) { errors.Add("Duplicate EntityId: " + entity.Id + " (" + entity.name + ")"); continue; }
                    entities.Add(entity.Id, entity);
                }
            return errors;
        }

        public bool TryGetEntity(string id, out EntityId entity)
        {
            entity = null;
            return !string.IsNullOrEmpty(id) && entities.TryGetValue(id, out entity) && entity != null;
        }
    }
}
