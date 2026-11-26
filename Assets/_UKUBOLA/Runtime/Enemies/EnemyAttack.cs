using System.Collections.Generic;
using UnityEngine;
using Ukubola.Combat;
namespace Ukubola.Enemies
{
    public sealed class EnemyAttack : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private LayerMask collisionMask;
        private readonly List<Projectile> launched=new List<Projectile>();
        public Projectile ProjectilePrefab => projectilePrefab;
        public int Mask => collisionMask.value;
        public void Configure(Projectile prefab,int mask) { projectilePrefab=prefab; collisionMask=mask; }
        public Vector3 Origin => transform.position+Vector3.up*1.25f;
        public bool CanSee(Health target)
        {
            if(!target || target.IsDead) return false;
            Vector3 delta=target.transform.position+Vector3.up*1.15f-Origin;
            return delta.sqrMagnitude>.0001f && HitscanAttack.FirstHit(Origin,delta.normalized,delta.magnitude+.15f,Mask,gameObject,out var hit) && object.ReferenceEquals(HitscanAttack.Damageable(hit.collider),target);
        }
        public bool Fire(EnemyDefinition def, Health target)
        {
            if(!def || !CanSee(target)) return false;
            Vector3 delta=target.transform.position+Vector3.up*1.15f-Origin;
            if(def.AttackKind==EnemyAttackKind.Melee?Vector3.Distance(transform.position,target.transform.position)>def.AttackRange:delta.magnitude>def.AttackRange) return false;
            if(def.AttackKind==EnemyAttackKind.Melee)return target.TakeDamage(new DamageInfo(def.Damage,gameObject,DamageType.Melee,target.transform.position)).Applied;
            if(def.AttackKind==EnemyAttackKind.Hitscan)
            { if(!GetComponent<EnemyFaction>())return target.TakeDamage(new DamageInfo(def.Damage,gameObject,DamageType.Hitscan,target.transform.position)).Applied;
              var direction=Quaternion.AngleAxis(Random.Range(-7f,7f),Vector3.up)*delta.normalized;
              if(HitscanAttack.FirstHit(Origin,direction,def.AttackRange,Mask,gameObject,out var hit)){var victim=HitscanAttack.Damageable(hit.collider);if(HitscanAttack.Live(victim))victim.TakeDamage(new DamageInfo(def.Damage,gameObject,DamageType.Hitscan,hit.point));}return true; }
            if(!projectilePrefab) return false;
            launched.RemoveAll(p=>!p);
            var projectile=Instantiate(projectilePrefab,Origin,Quaternion.identity);
            projectile.Launch(gameObject,delta.normalized,Mask,7f,25f,def.Damage); launched.Add(projectile); return true;
        }
        public void ClearProjectiles() { foreach(var p in launched) if(p) { p.enabled=false; Destroy(p.gameObject); } launched.Clear(); }
    }
}
