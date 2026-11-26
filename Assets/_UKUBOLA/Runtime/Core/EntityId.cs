using System;
using UnityEngine;

namespace Ukubola
{
    [DisallowMultipleComponent]
    public sealed class EntityId : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Id => id;
        public bool IsValid => Guid.TryParseExact(id, "N", out Guid value) && value != Guid.Empty;

        // Reset is called when adding/resetting the component in the Editor.
        // Never regenerate IDs on Awake or OnValidate: saved games need stability.
        private void Reset()
        {
            if (!IsValid) id = Guid.NewGuid().ToString("N");
        }
    }
}
