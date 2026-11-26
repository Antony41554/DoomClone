using UnityEngine;
using Ukubola.Combat;
using Ukubola.Rendering;
namespace Ukubola.Enemies
{
    [RequireComponent(typeof(Health),typeof(EnemyMotor),typeof(EnemyAttack))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private Health target;
        [SerializeField] private Camera viewer;
        [SerializeField] private NoiseEmitter noise;
        [SerializeField] private EnemyNavigation navigation;
        private Health health; private EnemyMotor motor; private EnemyAttack attack; private IEnemyPresentation visual; private CapsuleCollider body;
        private Vector3 spawn,lastKnown; private Quaternion spawnRotation;
        private bool alerted, started; private Health rival;
        public Health CombatTarget=>rival&&!rival.IsDead?rival:target; private int heard;
        private float stateUntil,nextAttack,nextPath;
        public EnemyDefinition Definition => definition; public Health Target => target; public Camera Viewer => viewer; public NoiseEmitter Noise => noise; public EnemyNavigation Navigation => navigation;
        public EnemyState State {get;private set;}=EnemyState.Dormant;
        public bool Suspended {get;set;}
        public int Attacks {get;private set;}
        public void SetDefinition(EnemyDefinition value)=>definition=value;
        public void Bind(Health player,Camera camera,NoiseEmitter emitter,EnemyNavigation nav) { target=player; viewer=camera; noise=emitter; navigation=nav; }
        private void Awake() { health=GetComponent<Health>(); motor=GetComponent<EnemyMotor>(); attack=GetComponent<EnemyAttack>(); foreach(var component in GetComponents<MonoBehaviour>()) if(component is IEnemyPresentation presentation){visual=presentation;break;} body=GetComponent<CapsuleCollider>(); spawn=transform.position; spawnRotation=transform.rotation; }
        private void OnEnable() { health.Damaged+=OnDamage; health.Died+=OnDeath; }
        private void OnDisable() { if(health) {health.Damaged-=OnDamage;health.Died-=OnDeath;} if(motor)motor.Disable(); }
        private void Start()
        {
            if(!definition || !definition.IsValid || !target || !viewer || !noise || !navigation || !navigation.Ready || visual==null || !visual.IsValid) { Debug.LogError("[UKUBOLA P06] Invalid enemy references: "+name,this); enabled=false; return; }
            visual.Bind(viewer); started=true; ResetEnemy();
        }
        public static bool Detect(float distance,float radius,bool visible,bool inCone)=>distance<=radius && visible && inCone;
        public static bool Hear(float distance,float radius,bool newPulse)=>newPulse && distance<=radius;
        public static bool CanAttack(float distance,float range,bool visible,float now,float ready)=>visible && distance<=range && now>=ready;
        public void ResetEnemy()
        {
            if(!started)return;
            attack.ClearProjectiles(); motor.Disable(); transform.SetPositionAndRotation(spawn,spawnRotation); health.ResetState(); body.enabled=true;
            rival=null; alerted=false; heard=noise.Sequence; State=EnemyState.Dormant; stateUntil=nextAttack=nextPath=0; Attacks=0;
            if(!motor.Begin(spawn)) { Debug.LogError("[UKUBOLA P06] Enemy not on NavMesh: "+name,this); enabled=false; return; } motor.Stop();
        }
        public bool SaveAlerted => alerted;
        public Vector3 SaveLastKnown => lastKnown;
        public float SaveCooldown => Mathf.Max(0,nextAttack-Time.time);
        public Vector3 SavePosition => transform.position - Vector3.up*(motor.Agent.enabled?motor.Agent.baseOffset:0f);
        public void RestoreEnemy(int value,Vector3 position,float yaw,bool awake,Vector3 known,float cooldown,Health opponent)
        {
            if(!started)throw new System.InvalidOperationException("Enemy not initialized: "+name);
            attack.ClearProjectiles();motor.Disable();motor.Agent.baseOffset=0;health.RestoreSavedHealth(value);
            transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));body.enabled=!health.IsDead;
            rival=opponent&&opponent!=target&&opponent!=health&&!opponent.IsDead?opponent:null;
            alerted=awake||rival!=null;lastKnown=known;heard=noise.Sequence;stateUntil=nextPath=0;nextAttack=Time.time+cooldown;Attacks=0;
            if(health.IsDead){State=EnemyState.Dead;return;}
            if(!motor.Begin(position))throw new System.InvalidOperationException("Saved enemy position not on NavMesh: "+name);
            motor.Stop();State=alerted?EnemyState.Chase:EnemyState.Dormant;
        }
        public void RestoreRival(Health opponent)
        {rival=opponent&&opponent!=target&&opponent!=health&&!opponent.IsDead&&!health.IsDead?opponent:null;}
        private void OnDamage(Health _,DamageInfo info,DamageResult result)
        {
            if(!started || result.Killed)return;
            var faction=GetComponent<EnemyFaction>();if(faction&&faction.Retaliates(info.Source)){var attacker=info.Source.GetComponent<Health>();if(attacker&&!attacker.IsDead){rival=attacker;alerted=true;lastKnown=attacker.transform.position;}}
            if(info.Source==target.gameObject) { rival=null;alerted=true;lastKnown=target.transform.position; }
            if(alerted) { State=EnemyState.Pain; stateUntil=Time.time+definition.Pain; motor.Stop(); }
        }
        private void OnDeath(Health _,DamageInfo info) { State=EnemyState.Dead; motor.Disable(); body.enabled=false; }
        private void Update()
        {
            if(Time.timeScale<=0f || !started || health.IsDead || !motor.Ready)return;
            if(Suspended || target.IsDead) { motor.Stop(); if(State==EnemyState.Attack)State=EnemyState.Chase; return; }
            if(rival&&rival.IsDead){rival=null;lastKnown=target.transform.position;nextPath=0;State=EnemyState.Chase;}
            var opponent=CombatTarget;
            float now=Time.time, distance=Vector3.Distance(transform.position,opponent.transform.position);
            Vector3 flat=opponent.transform.position-transform.position;flat.y=0;
            bool visible=distance<=definition.Sight && attack.CanSee(opponent);
            bool newPulse=heard!=noise.Sequence; heard=noise.Sequence;
            if(!rival && newPulse && noise.CanHear(transform.position,definition.Hearing)) { alerted=true;lastKnown=noise.Position; }
            if(Detect(distance,definition.Sight,visible,flat.sqrMagnitude<.001f || Vector3.Dot(transform.forward,flat.normalized)>=0f)) alerted=true;
            if(!alerted) { State=EnemyState.Dormant; motor.Stop();return; }
            if(visible)lastKnown=opponent.transform.position;
            if(State==EnemyState.Pain && now<stateUntil)return;
            if(State==EnemyState.Attack)
            {
                motor.Face(lastKnown);
                if(now<stateUntil)return;
                if(visible && distance<=definition.AttackRange && attack.Fire(definition,opponent))Attacks++;
                nextAttack=now+definition.Cooldown; State=EnemyState.Chase; nextPath=0; return;
            }
            if(CanAttack(distance,definition.AttackRange,visible,now,nextAttack))
            { State=EnemyState.Attack; stateUntil=now+definition.Windup; motor.Stop();motor.Face(lastKnown);return; }
            State=EnemyState.Chase;
            if(visible && distance<=definition.AttackRange) { motor.Stop();motor.Face(lastKnown);return; }
            if(!visible && Vector3.Distance(transform.position,lastKnown)<.8f) { motor.Stop();return; }
            if(now>=nextPath) { motor.Go(lastKnown);nextPath=now+.25f; }
        }
        private void LateUpdate() { if(started)visual.Present(State,motor.Ready && motor.Agent.velocity.sqrMagnitude>.04f,Time.time); }
    }
}
