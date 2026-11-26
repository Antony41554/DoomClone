using UnityEngine;
using Ukubola.World;

namespace Ukubola.Rendering
{
    /// <summary>The pickup owns rules/collider; this component owns only its 3D appearance.</summary>
    public sealed class PickupPresentation : MonoBehaviour
    {
        [SerializeField] private Pickup pickup;
        [SerializeField] private Transform model;
        private Vector3 origin;
        public void Configure(Pickup item, Transform visual) { pickup = item; model = visual; if (model) origin = model.localPosition; }
        private void Awake() { if (model) origin = model.localPosition; }
        private void LateUpdate()
        {
            if (!pickup || !model) return;
            if (pickup.Visual) pickup.Visual.enabled = false;
            model.gameObject.SetActive(!pickup.Collected);
            if (pickup.Collected) return;
            model.localPosition = origin + Vector3.up * (.06f * Mathf.Sin(Time.time * 2f));
            model.localRotation = Quaternion.Euler(0, Time.time * 35f, 0);
        }
    }
}
