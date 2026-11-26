using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Ukubola.Combat;
namespace Ukubola.World
{
    [RequireComponent(typeof(Health),typeof(BoxCollider),typeof(MeshRenderer))]
    public sealed class ExplosiveBarrel : MonoBehaviour
    {
        [SerializeField] private int mask;
        [SerializeField] private float radius=3f;
        [SerializeField] private int maxDamage=60;
        private Health hp;private BoxCollider solid;private Renderer visual;
        public bool Exploded {get;private set;} public int Mask=>mask;
        public void Configure(int value)=>mask=value;
        private void Awake(){hp=GetComponent<Health>();solid=GetComponent<BoxCollider>();visual=GetComponent<Renderer>();}
        private void OnEnable(){hp.Died+=Explode;}
        private void OnDisable(){if(hp)hp.Died-=Explode;}
        public static int BlastDamage(float distance,float range,int maximum)=>range<=0 || distance>=range?0:Mathf.Max(0,Mathf.CeilToInt(maximum*(1f-Mathf.Max(0,distance)/range)));
        private void Explode(Health _,DamageInfo damage)
        {
            if(Exploded)return;Exploded=true;
            Vector3 origin=solid.bounds.center;solid.enabled=false;visual.enabled=false;var obstacle=GetComponent<NavMeshObstacle>();if(obstacle)obstacle.enabled=false;Physics.SyncTransforms();
            var targets=new Dictionary<Health,Vector3>();
            foreach(var c in Physics.OverlapSphere(origin,radius,mask,QueryTriggerInteraction.Ignore))
            {
                var target=c.GetComponentInParent<Health>();if(!target || target==hp || target.IsDead)continue;
                var point=c.ClosestPoint(origin);
                if(!targets.TryGetValue(target,out var old) || (point-origin).sqrMagnitude<(old-origin).sqrMagnitude)targets[target]=point;
            }
            foreach(var entry in targets)
            {
                var target=entry.Key;if(!target || target.IsDead)continue;
                var delta=entry.Value-origin;float distance=delta.magnitude;
                if(distance>.001f && HitscanAttack.FirstHit(origin,delta.normalized,distance+.04f,mask,gameObject,out var hit) && hit.collider.GetComponentInParent<Health>()!=target)continue;
                int amount=BlastDamage(distance,radius,maxDamage);
                if(amount>0)target.TakeDamage(new DamageInfo(amount,damage.Source?damage.Source:gameObject,DamageType.Explosion,entry.Value));
            }
            Debug.Log("[UKUBOLA P08] Barrel exploded",this);
        }
        public void RestoreBarrel(int value)
        {
            hp.RestoreSavedHealth(value);Exploded=hp.IsDead;solid.enabled=!Exploded;visual.enabled=!Exploded;
            var obstacle=GetComponent<NavMeshObstacle>();if(obstacle)obstacle.enabled=!Exploded;
        }
        public void ResetBarrel(){hp.ResetState();Exploded=false;solid.enabled=true;visual.enabled=true;var obstacle=GetComponent<NavMeshObstacle>();if(obstacle)obstacle.enabled=true;}
    }
}
