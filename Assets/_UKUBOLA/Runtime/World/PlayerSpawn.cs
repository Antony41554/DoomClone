using UnityEngine;

namespace Ukubola.World
{
    [DisallowMultipleComponent]
    public sealed class PlayerSpawn : MonoBehaviour
    {
        // Marker only. The Editor positions the Phase 01 player; no runtime teleport yet.
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * .3f, .3f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }
    }
}
