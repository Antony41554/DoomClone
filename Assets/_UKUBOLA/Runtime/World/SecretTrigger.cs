using UnityEngine;
using Ukubola.Player;
namespace Ukubola.World
{
    [RequireComponent(typeof(BoxCollider),typeof(Rigidbody))]
    public sealed class SecretTrigger : MonoBehaviour
    {
        public bool Found {get;private set;}
        public bool TryDiscover(){if(Found)return false;Found=true;return true;}
        private void OnTriggerEnter(Collider other){var p=other.GetComponentInParent<PlayerLife>();if(p && !p.Health.IsDead && TryDiscover())Debug.Log("[UKUBOLA P08] SECRET FOUND",this);}
        public void ResetSecret()=>Found=false;
    }
}
