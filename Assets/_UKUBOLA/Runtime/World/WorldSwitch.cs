using UnityEngine;
namespace Ukubola.World
{
    public sealed class WorldSwitch : MonoBehaviour,IInteractable
    {
        [SerializeField] private Door door;
        [SerializeField] private Lift lift;
        [SerializeField] private Renderer indicator;
        private bool active;
        public Door Door=>door; public Lift Lift=>lift; public Renderer Indicator=>indicator;
        public void Configure(Door d,Lift l,Renderer r){door=d;lift=l;indicator=r;}
        public string Interact(InteractionContext context)
        {
            if(!context.Health || context.Health.IsDead)return "Cannot use switch";
            if(door)door.Toggle();else if(lift)lift.Toggle();else return "Switch not connected";
            active=!active;Paint();return name+" activated";
        }
        private void Paint(){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",active?Color.green:new Color(1f,.3f,.1f));indicator.SetPropertyBlock(block);}
        public bool Active => active;
        public void RestoreSwitch(bool value){active=value;Paint();}
        public void ResetSwitch(){active=false;Paint();}
    }
}
