using UnityEngine;
using UnityEngine.InputSystem;
using Ukubola.Player;
using Ukubola.Combat;
namespace Ukubola.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponController),typeof(PlayerInventory),typeof(Health))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private WeaponController weapons;
        private PlayerInventory inventory;
        private Health health;
        private InputActionAsset actions;
        private InputAction use;
        public string LastMessage {get;private set;}="E / Space: use";
        private void Awake(){weapons=GetComponent<WeaponController>();inventory=GetComponent<PlayerInventory>();health=GetComponent<Health>();}
        private void OnEnable()
        {
            var source=GetComponent<PlayerInputReader>().Actions;
            if(!source){Debug.LogError("[UKUBOLA P08] Missing input asset.",this);enabled=false;return;}
            try{actions=Instantiate(source);actions.Disable();use=actions.FindActionMap("Gameplay",true).FindAction("Use",true);use.Enable();}
            catch(System.Exception ex){Debug.LogError("[UKUBOLA P08] Use input: "+ex.Message,this);enabled=false;}
        }
        private void OnDisable(){if(actions){actions.Disable();Destroy(actions);}actions=null;use=null;}
        private void LateUpdate()
        {
            var input=GetComponent<PlayerInputReader>();
            if(use==null || !weapons.CanControl || !(input.HasQaCommands?input.QaCommands.UsePressed:use.WasPressedThisFrame()))return;
            var camera=weapons.AimCamera;
            if(HitscanAttack.FirstHit(camera.transform.position,camera.transform.forward,2.6f,weapons.HitMask,gameObject,out var hit))
            {
                var target=hit.collider.GetComponentInParent<IInteractable>();
                if(target!=null){LastMessage=target.Interact(new InteractionContext(inventory,health));Debug.Log("[UKUBOLA P08] "+LastMessage,this);return;}
            }
            LastMessage="Nothing to use";
        }
        public void ResetMessage()=>LastMessage="E / Space: use";
    }
}
