using UnityEngine;
using UnityEngine.AI;
using Ukubola.Combat;
namespace Ukubola.Enemies {
 [RequireComponent(typeof(NavMeshAgent),typeof(Health))] public sealed class FloatingEnemyMotor:MonoBehaviour {
  private NavMeshAgent agent;private Health hp;
  private void Awake(){agent=GetComponent<NavMeshAgent>();hp=GetComponent<Health>();}
  private void OnEnable(){hp.Died+=Land;}
  private void Update(){if(Time.timeScale<=0||!agent.enabled||!agent.isOnNavMesh||hp.IsDead)return;agent.baseOffset=.35f+.12f*Mathf.Sin(Time.time*2.2f);}
  private void Land(Health _,DamageInfo damage){float height=agent.baseOffset;agent.enabled=false;agent.baseOffset=0;transform.position-=Vector3.up*height;}
  private void OnDisable(){if(hp)hp.Died-=Land;if(agent)agent.baseOffset=0;}
 }
}