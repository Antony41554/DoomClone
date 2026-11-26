using UnityEngine;
using UnityEngine.AI;
using Ukubola.Player;
using Ukubola.Combat;
namespace Ukubola.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshObstacle))]
    public sealed class Door : MonoBehaviour,IInteractable
    {
        [SerializeField] private Transform panel;
        [SerializeField] private KeyId required;
        [SerializeField] private bool anyKey, switchOnly, staysOpen;
        [SerializeField] private float height=3.2f;
        private NavMeshObstacle obstacle;
        private BoxCollider solid;
        private Vector3 baseScale;
        private float fraction, closeAt;
        private bool opening;
        public Transform Panel=>panel; public KeyId Required=>required; public bool SwitchOnly=>switchOnly;
        public bool IsOpen=>fraction>=.999f; public float Fraction=>fraction;
        public void Configure(Transform visual,KeyId key,bool any,bool remote,bool permanent,float h)
        {panel=visual;required=key;anyKey=any;switchOnly=remote;staysOpen=permanent;height=h;}
        private void Awake(){obstacle=GetComponent<NavMeshObstacle>();solid=panel.GetComponent<BoxCollider>();baseScale=panel.localScale;}
        public static float RemainingHeight(float h,float f)=>h*(1f-Mathf.Clamp01(f));
        public string Interact(InteractionContext context)
        {
            if(switchOnly)return "Door controlled by switch";
            if(!context.Inventory || !context.Health || context.Health.IsDead)return "Cannot use door";
            if(!KeyRequirement.Allows(context.Inventory.Keys,required,anyKey))return "Requires "+required;
            Open();return name+" opening";
        }
        public void Open(){opening=true;closeAt=Time.time+4f;}
        public void Toggle(){if(opening){opening=false;}else Open();}
        private bool Occupied()
        {
            var half=new Vector3(baseScale.x*.5f,height*.5f,baseScale.z*.5f)+new Vector3(.35f,0,.35f);
            foreach(var c in Physics.OverlapBox(transform.position+Vector3.up*height*.5f,half,transform.rotation,~0,QueryTriggerInteraction.Ignore))
            {if(c.GetComponentInParent<Health>())return true;}
            return false;
        }
        private void Update()
        {
            if(!staysOpen && opening && IsOpen && Time.time>=closeAt)opening=false;
            if(!opening && fraction>0f && Occupied()){opening=true;closeAt=Time.time+4f;}
            float next=Mathf.MoveTowards(fraction,opening?1f:0f,Time.deltaTime*1.2f);
            if(next==fraction)return;
            fraction=next;Apply();Physics.SyncTransforms();
        }
        private void Apply()
        {
            float h=RemainingHeight(height,fraction);
            panel.localPosition=Vector3.up*(height-h*.5f);
            panel.localScale=new Vector3(baseScale.x,Mathf.Max(.001f,h),baseScale.z);
            solid.enabled=!IsOpen;panel.GetComponent<Renderer>().enabled=!IsOpen;
            obstacle.enabled=!IsOpen;
        }
        public bool Opening => opening;
        public float CloseRemaining => Mathf.Max(0,closeAt-Time.time);
        public void RestoreDoor(float value,bool targetOpen,float remaining)
        {fraction=Mathf.Clamp01(value);opening=targetOpen;closeAt=Time.time+Mathf.Max(0,remaining);Apply();}
        public void ResetDoor(){opening=false;fraction=0;closeAt=0;Apply();}
    }
}
