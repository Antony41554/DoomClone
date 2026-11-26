using UnityEngine;
using Ukubola.Enemies;

namespace Ukubola.Rendering
{
    public sealed class EnemyModelView : MonoBehaviour,IEnemyPresentation,IEnemyRestorePresentation
    {
        [SerializeField] private Animation animationView;
        private EnemyBrain brain;
        private Transform[] joints;
        private Vector3[] positions,scales;
        private Quaternion[] rotations;
        private Renderer[] heldWeapon;
        private EnemyState last=(EnemyState)(-1);
        private string clip;
        private float started,firedAt=float.NegativeInfinity;
        private int seenAttacks;
        public bool IsValid=>animationView&&animationView.GetClip("Idle")&&animationView.GetClip("Walk")&&animationView.GetClip("Prepare")&&animationView.GetClip("Attack")&&animationView.GetClip("Pain")&&animationView.GetClip("Death");
        public void Configure(Animation value)=>animationView=value;
        public void Bind(Camera viewer){}
        private void Awake()
        {
            brain=GetComponent<EnemyBrain>();joints=animationView.GetComponentsInChildren<Transform>(true);
            heldWeapon=System.Array.FindAll(animationView.GetComponentsInChildren<Renderer>(true),r=>r.name=="Rifle"||r.name=="RifleMuzzle");
            positions=new Vector3[joints.Length];rotations=new Quaternion[joints.Length];scales=new Vector3[joints.Length];
            for(int i=0;i<joints.Length;i++){positions[i]=joints[i].localPosition;rotations[i]=joints[i].localRotation;scales[i]=joints[i].localScale;}
        }
        private void ResetPose()
        {
            if(joints==null)return;
            for(int i=0;i<joints.Length;i++){joints[i].localPosition=positions[i];joints[i].localRotation=rotations[i];joints[i].localScale=scales[i];}
        }
        public void Present(EnemyState state,bool moving,float time)
        {
            if(!IsValid)return;
            if(state!=last&&heldWeapon!=null)foreach(var renderer in heldWeapon)renderer.enabled=state!=EnemyState.Dead;
            if(brain&&brain.Attacks!=seenAttacks){seenAttacks=brain.Attacks;firedAt=time;}
            string next=state==EnemyState.Dead?"Death":state==EnemyState.Pain?"Pain":time-firedAt<animationView.GetClip("Attack").length?"Attack":state==EnemyState.Attack?"Prepare":moving?"Walk":"Idle";
            if(state!=EnemyState.Dead&&last==EnemyState.Dead)ResetPose();
            if(next!=clip||state!=last){animationView.Stop();clip=next;started=next=="Attack"?firedAt:time;animationView.Play(clip);animationView.enabled=false;}
            last=state;Sample(time);
        }
        private void Sample(float time)
        {
            var track=animationView[clip];float elapsed=Mathf.Max(0,time-started);
            if(clip=="Prepare"&&brain)elapsed=elapsed/Mathf.Max(.001f,brain.Definition.Windup)*track.length;
            elapsed=Mathf.Floor(elapsed*12)/12;
            track.time=clip=="Walk"||clip=="Idle"?Mathf.Repeat(elapsed,track.length):Mathf.Min(elapsed,track.length);
            track.enabled=true;track.weight=1;animationView.Sample();
        }
        public void RestoreState(EnemyState state,float phaseRemaining,float now)
        {
            ResetPose();seenAttacks=brain?brain.Attacks:0;firedAt=float.NegativeInfinity;clip=null;last=(EnemyState)(-1);
            Present(state,false,now);
            if(state==EnemyState.Dead)started=now-animationView.GetClip("Death").length-.1f;
            else if(state==EnemyState.Attack&&brain)started=now-Mathf.Max(0,brain.Definition.Windup-phaseRemaining);
            else if(state==EnemyState.Pain&&brain)started=now-Mathf.Max(0,brain.Definition.Pain-phaseRemaining);
            Sample(now);
        }
    }
}
