using System;
using UnityEngine;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorldTrigger : PersistentBehaviour
    {
        [Serializable] private sealed class Data { public bool entered; }
        private bool entered;
        public override string StateKind => "trigger";
        public bool Entered => entered;
        private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
        private void OnTriggerEnter(Collider actor)
        {
            var player = actor.GetComponentInParent<PlayerLife>();
            if (entered || !player || player.Health.IsDead || Context && Context.Restoring) return;
            entered = true; Emit(WorldSignal.Entered);
        }
        public override EntitySnapshot CaptureEntity() => State(new Data { entered = entered });
        public override void ValidateEntity(EntitySnapshot state)
        { if (Read<Data>(state) == null) throw new InvalidOperationException("Missing trigger state."); }
        public override void RestoreEntity(EntitySnapshot state)
        { ValidateEntity(state); entered = Read<Data>(state).entered; }
    }
}
