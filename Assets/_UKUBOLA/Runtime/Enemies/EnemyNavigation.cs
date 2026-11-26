using UnityEngine;
using UnityEngine.AI;
namespace Ukubola.Enemies
{
    // Register only this scene's baked data. Agents are disabled in the asset until Start.
    [DisallowMultipleComponent]
    public sealed class EnemyNavigation : MonoBehaviour
    {
        [SerializeField] private NavMeshData data;
        private NavMeshDataInstance instance;
        public NavMeshData Data => data;
        public bool Ready => instance.valid;
        public void Configure(NavMeshData value) => data=value;
        private void OnEnable()
        { if (data) instance=NavMesh.AddNavMeshData(data); if (!instance.valid) Debug.LogError("[UKUBOLA P06] Navigation unavailable.",this); }
        private void OnDisable() { if(instance.valid) instance.Remove(); }
    }
}
