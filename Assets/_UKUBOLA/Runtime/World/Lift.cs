using UnityEngine;
using Ukubola.Player;
using Ukubola.Combat;
namespace Ukubola.World
{
    [DefaultExecutionOrder(-100)]
    public sealed class Lift : MonoBehaviour,IInteractable
    {
        [SerializeField] private Transform platform;
        [SerializeField] private CharacterController rider;
        [SerializeField] private float rise=.75f;
        private Vector3 start;
        private bool raised;
        public Transform Platform=>platform; public CharacterController Rider=>rider; public bool Raised=>raised;
        public void Configure(Transform value,CharacterController player){platform=value;rider=player;}
        private void Awake(){start=platform.localPosition;}
        public void Toggle()=>raised=!raised;
        public string Interact(InteractionContext context){if(!context.Health || context.Health.IsDead)return "Cannot use lift";Toggle();return raised?"Lift rising":"Lift lowering";}
        private void Update()
        {
            float delta=Mathf.MoveTowards(platform.localPosition.y,start.y+(raised?rise:0),Time.deltaTime*.9f)-platform.localPosition.y;
            if(Mathf.Abs(delta)<.000001f)return;
            var box=platform.GetComponent<BoxCollider>().bounds;
            bool riding=rider && rider.enabled && rider.bounds.min.y>=box.max.y-.12f && rider.bounds.min.y<=box.max.y+.18f && rider.bounds.center.x>=box.min.x && rider.bounds.center.x<=box.max.x && rider.bounds.center.z>=box.min.z && rider.bounds.center.z<=box.max.z;
            // At a rising edge carry the character first, then move the slab; downward reverse order.
            if(riding && delta>0)rider.Move(Vector3.up*delta);
            platform.localPosition+=Vector3.up*delta;Physics.SyncTransforms();
            if(riding && delta<0)rider.Move(Vector3.up*delta);
        }
        public float ElevationFraction => rise>0?Mathf.Clamp01((platform.localPosition.y-start.y)/rise):0;
        public void RestoreLift(float fraction,bool targetRaised)
        {raised=targetRaised;platform.localPosition=start+Vector3.up*(rise*Mathf.Clamp01(fraction));}
        public void ResetLift(){raised=false;platform.localPosition=start;Physics.SyncTransforms();}
    }
}
