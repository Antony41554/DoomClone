using System;
using System.Linq;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Persistence;

namespace Ukubola.World
{
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class WorldActorState:PersistentBehaviour,IEntityRestoreComplete
    {
        [Serializable] private sealed class Data
        {
            public int hp,phase;
            public Vector3 position,known;
            public float yaw,cooldown,remaining;
            public bool alerted;
            public string opponent;
        }
        [SerializeField] private EnemyBrain brain;
        private string pendingOpponent;
        private EnemyState pendingPhase;
        private float pendingRemaining;
        public EnemyBrain Brain=>brain?brain:GetComponent<EnemyBrain>();
        public Health Health=>Brain.GetComponent<Health>();
        public override string StateKind=>"enemy";
        public override int RestoreOrder=>200;
        public void Configure(EnemyBrain value){brain=value;}
        private void Awake(){if(!brain)brain=GetComponent<EnemyBrain>();}
        private void OnEnable(){Health.Died+=OnDeath;}
        private void OnDisable(){if(Health)Health.Died-=OnDeath;}
        private void OnDeath(Health hp,DamageInfo damage)=>Emit(WorldSignal.Killed);
        public override EntitySnapshot CaptureEntity()
        {
            var target=Brain.CombatTarget;var opponent=target?target.GetComponent<WorldActorState>():null;
            return State(new Data{hp=Health.Current,position=Brain.SavePosition,yaw=Mathf.Repeat(transform.eulerAngles.y,360),alerted=Brain.SaveAlerted,known=Brain.SaveLastKnown,cooldown=Brain.SaveCooldown,phase=(int)Brain.State,remaining=Brain.SavePhaseRemaining,opponent=opponent?opponent.PersistentId:""});
        }
        public override void ValidateEntity(EntitySnapshot state)
        {
            var d=Read<Data>(state);
            if(d==null||d.hp<0||d.hp>Health.MaxHealth||!MissionDefinition.Finite(d.position)||!MissionDefinition.Finite(d.known)||!SaveData.Range(d.yaw,0,360)||!SaveData.Range(d.cooldown,0,60)||!SaveData.Range(d.remaining,0,60)||!Enum.IsDefined(typeof(EnemyState),d.phase)||(d.hp==0)!=((EnemyState)d.phase==EnemyState.Dead)||d.opponent==PersistentId||!string.IsNullOrEmpty(d.opponent)&&!Guid.TryParseExact(d.opponent,"N",out _))throw new InvalidOperationException("Invalid enemy state: "+name);
            if(Context&&(!Context.Definition.Bounds.Contains(d.position)||!Context.Definition.sectors.Any(s=>s.Contains(d.position,.25f))))throw new InvalidOperationException("Enemy outside mission sectors: "+name);
            if(!string.IsNullOrEmpty(d.opponent)&&!Actors().Any(a=>a.PersistentId==d.opponent))throw new InvalidOperationException("Unknown enemy rival.");
        }
        private WorldActorState[] Actors()=>gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WorldActorState>(true)).ToArray();
        public override void RestoreEntity(EntitySnapshot state)
        {
            ValidateEntity(state);var d=Read<Data>(state);pendingOpponent=d.opponent;pendingPhase=(EnemyState)d.phase;pendingRemaining=d.remaining;
            Brain.RestoreEnemy(d.hp,d.position,d.yaw,d.alerted,d.known,d.cooldown,null);
        }
        public void FinishRestore()
        {
            var rival=string.IsNullOrEmpty(pendingOpponent)?null:Actors().FirstOrDefault(a=>a.PersistentId==pendingOpponent);
            Brain.RestoreRival(rival?rival.Health:null);Brain.RestoreCombatPhase(pendingPhase,pendingRemaining);
        }
    }
}
