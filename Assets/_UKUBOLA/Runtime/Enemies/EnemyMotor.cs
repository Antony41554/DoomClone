using UnityEngine;
using UnityEngine.AI;
namespace Ukubola.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        public NavMeshAgent Agent => GetComponent<NavMeshAgent>();
        public bool Ready => Agent.enabled && Agent.isOnNavMesh;
        public bool Begin(Vector3 position)
        {
            var agent=Agent; var filter=new NavMeshQueryFilter { agentTypeID=agent.agentTypeID, areaMask=agent.areaMask };
            agent.enabled=false;
            if(!NavMesh.SamplePosition(position,out var hit,.8f,filter) || Mathf.Abs(hit.position.y-position.y)>.45f) return false;
            transform.position=hit.position; agent.enabled=true;
            if (!agent.isOnNavMesh) { agent.enabled=false; return false; }
            agent.isStopped=false; return true;
        }
        public void Stop() { if(Ready) { Agent.isStopped=true; Agent.ResetPath(); } }
        public void Disable() { Stop(); Agent.enabled=false; }
        public bool Go(Vector3 point)
        {
            if(!Ready) return false;
            var filter=new NavMeshQueryFilter { agentTypeID=Agent.agentTypeID, areaMask=Agent.areaMask };
            if(!NavMesh.SamplePosition(point,out var hit,1.2f,filter) || Mathf.Abs(hit.position.y-point.y)>.65f) { Stop(); return false; }
            Agent.isStopped=false;
            if(Agent.SetDestination(hit.position)) return true;
            Stop(); return false;
        }
        public void Face(Vector3 point)
        { var dir=point-transform.position; dir.y=0; if(dir.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(dir); }
    }
}
