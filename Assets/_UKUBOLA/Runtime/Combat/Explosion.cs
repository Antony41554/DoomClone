using System.Collections.Generic;
using UnityEngine;
namespace Ukubola.Combat {
 public static class Explosion {
  public static event System.Action<Vector3> Occurred;
  public static int Damage(float distance,float radius,int maximum)=>radius<=0||distance>=radius||maximum<=0?0:Mathf.CeilToInt(maximum*(1-Mathf.Max(0,distance)/radius));
  public static void Apply(Vector3 origin,float radius,int maximum,int mask,GameObject owner){
   if(radius<=0||maximum<=0)return;Occurred?.Invoke(origin);
   var targets=new Dictionary<Health,Vector3>();
   foreach(var c in Physics.OverlapSphere(origin,radius,mask,QueryTriggerInteraction.Ignore)){
    var hp=c.GetComponentInParent<Health>();if(!hp||hp.IsDead)continue;var point=c.ClosestPoint(origin);
    if(!targets.TryGetValue(hp,out var previous)||(point-origin).sqrMagnitude<(previous-origin).sqrMagnitude)targets[hp]=point;
   }
   foreach(var item in targets){if(!item.Key||item.Key.IsDead)continue;var delta=item.Value-origin;float dist=delta.magnitude;
    if(dist>.001f&&HitscanAttack.FirstHit(origin,delta.normalized,dist+.04f,mask,null,out var hit)&&hit.collider.GetComponentInParent<Health>()!=item.Key)continue;
    int amount=Damage(dist,radius,maximum);if(amount>0)item.Key.TakeDamage(new DamageInfo(amount,owner,DamageType.Explosion,item.Value));
   }
  }
 }
}