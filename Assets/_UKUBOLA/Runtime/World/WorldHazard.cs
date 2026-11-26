using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    public enum WorldHazardKind { Toxic, HotMetal, Electricity }
    public enum WorldHazardPhase { Disabled, Active, Changing, Warning, Discharge, Rest }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorldHazard : PersistentBehaviour, IWorldActionTarget
    {
        [Serializable] private sealed class ActorTimer { public string id;public float remaining;public int lastCycle=-1; }
        [Serializable] private sealed class Data
        {public bool active,changing;public float level,phase;public int cycle;public ActorTimer[] actors;}
        [SerializeField] private WorldHazardKind kind;
        [SerializeField] private BoxCollider volume;
        [SerializeField] private Transform surface;
        [SerializeField] private Renderer display;
        [SerializeField] private NavMeshObstacle avoid;
        [SerializeField] private Vector3 fullSize,fullCenter,surfaceOrigin;
        [SerializeField] private bool active=true;
        private float level=1,phase,clock,grace;
        private int cycle;
        private bool changing;
        private readonly Dictionary<string,float> cooldowns=new Dictionary<string,float>(StringComparer.Ordinal);
        private readonly Dictionary<string,int> discharges=new Dictionary<string,int>(StringComparer.Ordinal);
        private readonly Collider[] overlaps=new Collider[64];
        private readonly HashSet<Health> exposed=new HashSet<Health>();
        private MaterialPropertyBlock properties;
        public override string StateKind=>"hazard";
        public override int RestoreOrder=>40;
        public WorldHazardKind Kind=>kind;
        public float Level=>level;
        public bool Safe=>!active||level<=0;
        public float CycleTime=>phase;
        public WorldHazardPhase Phase=>Safe?WorldHazardPhase.Disabled:kind!=WorldHazardKind.Electricity?(changing?WorldHazardPhase.Changing:WorldHazardPhase.Active):grace>0||phase<.75f?WorldHazardPhase.Warning:phase<1.5f?WorldHazardPhase.Discharge:WorldHazardPhase.Rest;
        public event Action Changed;
        public void Configure(WorldHazardKind type,BoxCollider bounds,Transform visibleSurface,Renderer renderer,NavMeshObstacle avoidance=null)
        {
            kind=type;volume=bounds;surface=visibleSurface;display=renderer;avoid=avoidance;
            fullSize=bounds.size;fullCenter=bounds.center;surfaceOrigin=surface?surface.localPosition:Vector3.zero;
            active=true;level=1;phase=clock=grace=0;cycle=0;changing=false;cooldowns.Clear();discharges.Clear();
            ValidateConfiguration();ApplyView();
        }
        public void ValidateConfiguration()
        {
            if(!volume||volume.gameObject!=gameObject||!MissionDefinition.Finite(fullSize)||fullSize.x<=0||fullSize.y<=0||fullSize.z<=0||!MissionDefinition.Finite(fullCenter))throw new InvalidOperationException("Invalid hazard volume: "+name);
            if(!Enum.IsDefined(typeof(WorldHazardKind),kind))throw new InvalidOperationException("Unknown hazard type.");
        }
        private void Awake(){ValidateConfiguration();ApplyView();}
        private void FixedUpdate()
        {
            if(Time.timeScale<=0)return;Tick(Time.fixedDeltaTime);
            if(Safe||Phase==WorldHazardPhase.Warning||Phase==WorldHazardPhase.Rest)return;
            var box=volume.bounds;int count=Physics.OverlapBoxNonAlloc(box.center,box.extents,overlaps,Quaternion.identity,LayerMask.GetMask("Player","Enemy"),QueryTriggerInteraction.Ignore);
            exposed.Clear();
            for(int i=0;i<count;i++)
            {
                var hp=overlaps[i].GetComponentInParent<Health>();
                if(!hp||hp.IsDead||!exposed.Add(hp))continue;
                string id=ActorId(hp);if(id!=null)ApplyToActor(hp,id);
            }
        }
        private static string ActorId(Health hp)
        {
            if(hp.GetComponent<PlayerLife>())return "player";
            if(!hp.GetComponent<EnemyBrain>())return null;
            var entity=hp.GetComponents<MonoBehaviour>().OfType<IPersistentEntity>().FirstOrDefault(e=>e.StateKind=="enemy");
            return entity?.PersistentId;
        }
        public void Tick(float seconds)
        {
            if(!MissionDefinition.Finite(seconds)||seconds<0||seconds>3600)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(grace>0){grace=Mathf.Max(0,grace-seconds);ApplyView();return;}
            clock+=seconds;if(Safe)return;
            if(changing)
            {
                level=Mathf.Max(0,level-seconds/(kind==WorldHazardKind.Toxic?4f:6f));
                if(level==0){active=false;changing=false;ApplyView();Changed?.Invoke();Emit(WorldSignal.Arrived);return;}
            }
            if(kind==WorldHazardKind.Electricity)
            {phase+=seconds;while(phase>=3){phase-=3;cycle++;}}
            ApplyView();Changed?.Invoke();
        }
        public bool ApplyToActor(Health actor,string id)
        {
            if(!ValidActorId(id))throw new ArgumentException("Stable hazard actor identity required.");
            if(!actor||actor.IsDead||Safe||grace>0)return false;
            int damage;
            if(kind==WorldHazardKind.Electricity)
            {
                if(Phase!=WorldHazardPhase.Discharge||discharges.TryGetValue(id,out int hit)&&hit==cycle)return false;
                discharges[id]=cycle;damage=15;
            }
            else
            {
                if(cooldowns.TryGetValue(id,out float ready)&&clock<ready)return false;
                cooldowns[id]=clock+1;damage=kind==WorldHazardKind.Toxic?10:20;
            }
            return actor.TakeDamage(new DamageInfo(damage,gameObject,DamageType.Environmental,actor.transform.position)).Applied;
        }
        public bool Accepts(WorldVerb verb)=>verb==WorldVerb.On||verb==WorldVerb.Off||verb==WorldVerb.Toggle||kind==WorldHazardKind.Toxic&&verb==WorldVerb.Drain||kind==WorldHazardKind.HotMetal&&verb==WorldVerb.Cool;
        public void Execute(WorldVerb verb,float value)
        {
            if(!Accepts(verb))throw new InvalidOperationException("Unsupported hazard command.");
            if(verb==WorldVerb.Drain||verb==WorldVerb.Cool)
            {if(Safe||changing)return;changing=true;}
            else
            {
                bool next=verb==WorldVerb.Toggle?!active:verb==WorldVerb.On;
                if(next==active)return;active=next;changing=false;
                if(active){level=1;phase=0;cycle++;grace=kind==WorldHazardKind.Electricity?.75f:0;}
            }
            ApplyView();Changed?.Invoke();Emit(WorldSignal.Activated);
        }
        private void ApplyView()
        {
            if(!volume)return;volume.isTrigger=true;volume.enabled=!Safe;
            if(kind==WorldHazardKind.Toxic)
            {
                volume.size=new Vector3(fullSize.x,Mathf.Max(.001f,fullSize.y*level),fullSize.z);
                volume.center=fullCenter-Vector3.up*fullSize.y*(1-level)*.5f;
                if(surface)surface.localPosition=surfaceOrigin-Vector3.up*fullSize.y*(1-level);
            }
            else{volume.size=fullSize;volume.center=fullCenter;}
            if(avoid)avoid.enabled=!Safe;
            if(display)
            {
                if(properties==null)properties=new MaterialPropertyBlock();display.GetPropertyBlock(properties);
                Color color=Safe?new Color(.18f,.24f,.27f):kind==WorldHazardKind.Toxic?new Color(.2f,.65f,.12f):kind==WorldHazardKind.HotMetal?Color.Lerp(new Color(.18f,.24f,.27f),new Color(1,.2f,.025f),level):Phase==WorldHazardPhase.Discharge?new Color(.18f,.55f,1):Phase==WorldHazardPhase.Warning?new Color(1,.55f,.06f):new Color(.06f,.14f,.2f);
                float strength=Safe?0:kind==WorldHazardKind.Electricity?(Phase==WorldHazardPhase.Discharge?4:Phase==WorldHazardPhase.Warning?1:0):level*.7f;
                properties.SetColor("_BaseColor",color);properties.SetColor("_EmissionColor",color*strength);display.SetPropertyBlock(properties);
            }
        }
        private static bool ValidActorId(string id)=>id=="player"||Guid.TryParseExact(id,"N",out _);
        public override EntitySnapshot CaptureEntity()
        {
            var ids=cooldowns.Keys.Concat(discharges.Keys).Distinct(StringComparer.Ordinal).OrderBy(s=>s,StringComparer.Ordinal);
            return State(new Data{active=active,changing=changing,level=level,phase=phase,cycle=cycle,actors=ids.Select(id=>new ActorTimer{id=id,remaining=cooldowns.TryGetValue(id,out float end)?Mathf.Max(0,end-clock):0,lastCycle=discharges.TryGetValue(id,out int hit)?hit:-1}).ToArray()});
        }
        public override void ValidateEntity(EntitySnapshot state)
        {
            var d=Read<Data>(state);
            if(d==null||!SaveData.Range(d.level,0,1)||!SaveData.Range(d.phase,0,3)||d.phase>=3||d.cycle<0||d.actors==null||d.actors.Length>4096||d.actors.Any(a=>a==null||!ValidActorId(a.id)||!SaveData.Range(a.remaining,0,1.001f)||a.lastCycle< -1||a.lastCycle>d.cycle)||d.actors.Select(a=>a.id).Distinct(StringComparer.Ordinal).Count()!=d.actors.Length||d.changing&&(kind==WorldHazardKind.Electricity||!d.active||d.level<=0)||d.active&&d.level<=0)throw new InvalidOperationException("Invalid hazard snapshot: "+name);
        }
        public override void RestoreEntity(EntitySnapshot state)
        {
            ValidateEntity(state);var d=Read<Data>(state);active=d.active;changing=d.changing;level=d.level;phase=d.phase;cycle=d.cycle;clock=0;
            cooldowns.Clear();discharges.Clear();foreach(var actor in d.actors){if(actor.remaining>0)cooldowns[actor.id]=actor.remaining;if(actor.lastCycle>=0)discharges[actor.id]=actor.lastCycle;}
            grace=active&&kind==WorldHazardKind.Electricity?.75f:0;ApplyView();Changed?.Invoke();
        }
    }
}
