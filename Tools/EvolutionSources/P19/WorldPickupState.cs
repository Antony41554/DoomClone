using System;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.World
{
    [RequireComponent(typeof(Pickup))]
    public sealed class WorldPickupState:PersistentBehaviour
    {
        [Serializable] private sealed class Data{public bool collected;}
        private Pickup pickup;
        public Pickup Pickup=>pickup?pickup:GetComponent<Pickup>();
        public override string StateKind=>"pickup";
        private void Awake(){pickup=GetComponent<Pickup>();}
        private void OnEnable(){Pickup.Taken+=OnTaken;}
        private void OnDisable(){if(Pickup)Pickup.Taken-=OnTaken;}
        private void OnTaken(Pickup item)=>Emit(WorldSignal.Collected);
        public override EntitySnapshot CaptureEntity()=>State(new Data{collected=Pickup.Collected});
        public override void ValidateEntity(EntitySnapshot state){if(Read<Data>(state)==null)throw new InvalidOperationException("Missing pickup state.");}
        public override void RestoreEntity(EntitySnapshot state){ValidateEntity(state);Pickup.RestoreCollected(Read<Data>(state).collected);}
    }
}
