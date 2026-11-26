using System;
using UnityEngine;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorldSecret:PersistentBehaviour
    {
        [Serializable] private sealed class Data{public bool found;}
        private bool found;
        public bool Found=>found;
        public override string StateKind=>"secret";
        public event Action Discovered;
        private void Awake(){GetComponent<BoxCollider>().isTrigger=true;}
        private void OnTriggerEnter(Collider other){var player=other.GetComponentInParent<PlayerLife>();if(player&&!player.Health.IsDead)Discover();}
        public bool Discover()
        {
            if(found)return false;found=true;GetComponent<BoxCollider>().enabled=false;
            Discovered?.Invoke();Emit(WorldSignal.Entered);return true;
        }
        public override EntitySnapshot CaptureEntity()=>State(new Data{found=found});
        public override void ValidateEntity(EntitySnapshot state){if(Read<Data>(state)==null)throw new InvalidOperationException("Missing secret state.");}
        public override void RestoreEntity(EntitySnapshot state){ValidateEntity(state);found=Read<Data>(state).found;GetComponent<BoxCollider>().enabled=!found;}
    }
}
