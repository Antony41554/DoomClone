using System;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Rendering;

namespace Ukubola.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupDefinition definition;
        [SerializeField] private SpriteRenderer visual;
        private bool collected, applying;
        public PickupDefinition Definition => definition;
        public SpriteRenderer Visual => visual;
        public bool Collected => collected;
        public event Action<Pickup> Taken;
        public void Configure(PickupDefinition value, SpriteRenderer renderer) { definition = value; visual = renderer; }
        public void BindViewer(Camera camera) { if (visual && visual.TryGetComponent<Billboard>(out var billboard)) billboard.Configure(camera); }
        private void OnTriggerEnter(Collider other) => TryCollider(other);
        private void OnTriggerStay(Collider other) => TryCollider(other);
        private void TryCollider(Collider other)
        {
            if (!isActiveAndEnabled || collected || !other || !other.enabled || Time.timeScale <= 0f) return;
            var player = other.GetComponentInParent<PlayerInventory>();
            if (!player || !player.GetComponent<PlayerLife>()) return;
            TryCollect(player.GetComponent<Health>(), player);
        }
        public bool TryCollect(Health health, PlayerInventory inventory)
        {
            if (collected || applying || !isActiveAndEnabled || !definition || !definition.IsRuleValid) return false;
            applying = true;
            try
            {
                if (!PickupEffect.TryApply(definition, health, inventory)) return false;
                collected = true;
                GetComponent<SphereCollider>().enabled = false;
                if (visual) visual.enabled = false;
            }
            finally { applying = false; }
            Taken?.Invoke(this);
            return true;
        }
        public void RestoreCollected(bool value)
        {
            collected=value; applying=false; GetComponent<SphereCollider>().enabled=!value;
            if(visual)visual.enabled=!value;
        }
        public void ResetPickup()
        {
            collected = false;
            GetComponent<SphereCollider>().enabled = true;
            if (visual) visual.enabled = true;
        }
    }
}
