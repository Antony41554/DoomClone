using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;
namespace Ukubola.World
{
    [RequireComponent(typeof(BoxCollider),typeof(Rigidbody))]
    public sealed class DamageZone : MonoBehaviour
    {
        [SerializeField] private int damage=10;
        [SerializeField] private float interval=1f;
        private double next;
        public int Damage=>damage; public float Interval=>interval;
        private void OnTriggerStay(Collider other)
        {
            var life=other.GetComponentInParent<PlayerLife>();if(!isActiveAndEnabled || !life || life.Health.IsDead || Time.timeScale<=0)return;
            double now=Time.timeAsDouble;if(!Due(now,next))return;next=now+interval;
            life.Health.TakeDamage(new DamageInfo(damage,gameObject,DamageType.Environmental,other.transform.position));
        }
        public static bool Due(double now,double ready)=>now>=ready;
        public void ResetZone()=>next=0;
    }
}
