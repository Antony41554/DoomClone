using System;
using System.Linq;
using UnityEngine;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorldExit:PersistentBehaviour,IInteractable
    {
        [Serializable] private sealed class Data{public bool completed;}
        [SerializeField] private string[] requiredFlags=Array.Empty<string>();
        private bool completed;
        public override string StateKind=>"exit";
        public bool Completed=>completed;
        public bool Ready=>Context&&requiredFlags.All(Context.Flag);
        public event Action Finished;
        public void Configure(string[] requirements){requiredFlags=requirements;ValidateConfiguration();}
        public void ValidateConfiguration(){if(requiredFlags==null||requiredFlags.Length==0||requiredFlags.Any(string.IsNullOrWhiteSpace)||requiredFlags.Distinct(StringComparer.Ordinal).Count()!=requiredFlags.Length)throw new InvalidOperationException("Exit requires explicit unique objective flags.");}
        private void Awake(){ValidateConfiguration();GetComponent<BoxCollider>().isTrigger=true;}
        private void OnTriggerEnter(Collider other){var player=other.GetComponentInParent<PlayerLife>();if(player&&!player.Health.IsDead)TryComplete();}
        public bool TryComplete()
        {
            if(completed||!Ready)return false;completed=true;GetComponent<BoxCollider>().enabled=false;
            Emit(WorldSignal.Entered);Finished?.Invoke();return true;
        }
        public string Interact(InteractionContext actor)
        {if(!actor.Health||actor.Health.IsDead)return "UNAVAILABLE";return completed?"MISSION COMPLETE":TryComplete()?"MISSION COMPLETE":"RESTORE POWER AND DRAIN COOLANT";}
        public override EntitySnapshot CaptureEntity()=>State(new Data{completed=completed});
        public override void ValidateEntity(EntitySnapshot state){if(Read<Data>(state)==null)throw new InvalidOperationException("Missing exit state.");}
        public override void RestoreEntity(EntitySnapshot state){ValidateEntity(state);completed=Read<Data>(state).completed;GetComponent<BoxCollider>().enabled=!completed;}
    }
}
