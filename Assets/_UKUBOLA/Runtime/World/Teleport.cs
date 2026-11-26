using UnityEngine;
using Ukubola.Player;
namespace Ukubola.World
{
    [RequireComponent(typeof(BoxCollider),typeof(Rigidbody))]
    public sealed class Teleport : MonoBehaviour
    {
        [SerializeField] private Transform destination;
        private double ready;
        private PlayerLife pending;
        public Transform Destination=>destination;
        public void Configure(Transform value)=>destination=value;
        private void OnTriggerEnter(Collider other)
        {
            var life=other.GetComponentInParent<PlayerLife>();if(!life || life.Health.IsDead || !destination || Time.timeScale<=0 || Time.timeAsDouble<ready)return;
            pending=life;
        }
        private void Update()
        {
            var life=pending;pending=null;
            if(!life || life.Health.IsDead || !destination || Time.timeScale<=0)return;
            var cc=life.GetComponent<CharacterController>();var motor=life.GetComponent<PlayerMotor>();
            if(!cc.enabled || !motor.enabled || !GetComponent<BoxCollider>().bounds.Intersects(cc.bounds))return;
            Ukubola.Release.ReleasePresentation.Teleported(life.transform.position);
            ready=Time.timeAsDouble+.5;motor.enabled=false;cc.enabled=false;
            life.transform.SetPositionAndRotation(destination.position+Vector3.up*.03f,destination.rotation);
            cc.enabled=true;motor.enabled=true;Physics.SyncTransforms();Ukubola.Release.ReleasePresentation.Teleported(life.transform.position);Debug.Log("[UKUBOLA P08] Teleported",this);
        }
        public void ResetTeleport(){ready=0;pending=null;}
    }
}
