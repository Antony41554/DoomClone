using UnityEngine;
using UnityEngine.InputSystem;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
using Ukubola.Enemies;
namespace Ukubola.Audio
{
    public enum Cue { Pistol,Shotgun,Pickup,Key,Pain,Death,Guard,Imp,EnemyDeath,Door,Lift,Explosion,Secret,Teleport,Exit,Empty }
    [DefaultExecutionOrder(210)]
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private WorldDiagnostics world;
        [SerializeField] private LevelStats stats;
        [SerializeField] private AudioSource music,local;
        [SerializeField] private AudioSource[] spatial;
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private bool diagnosticsEnabled = true;
        public bool DiagnosticsEnabled { get => diagnosticsEnabled; set => diagnosticsEnabled = value; }
        public void SetMix(float musicGain,float effectsGain)
        {
            if(music)music.volume=.24f*Mathf.Clamp01(musicGain);
            if(local)local.volume=.70f*Mathf.Clamp01(effectsGain);
            if(spatial!=null)foreach(var a in spatial)if(a)a.volume=.75f*Mathf.Clamp01(effectsGain);
        }
        private Health hp; private WeaponController weapons;
        private int shots,voice; private string lastResult; private int[] attacks; private float[] doorFractions; private int[] doorDirections; private bool[] dead,exploded;
        private bool raised,found; private Vector3 position;
        public WorldDiagnostics World=>world; public LevelStats Stats=>stats; public AudioSource Music=>music; public AudioSource Local=>local; public AudioSource[] Spatial=>spatial;public AudioClip[] Clips=>clips;
        [SerializeField] private AudioClip[] advancedClips;
        public AudioClip[] AdvancedClips=>advancedClips;
        public void ConfigureAdvancedClips(AudioClip[] value)=>advancedClips=value;
        public bool MusicEnabled{get;private set;}=true;public bool EffectsEnabled{get;private set;}=true;
        public void Configure(WorldDiagnostics w,LevelStats s,AudioSource m,AudioSource l,AudioSource[] pool,AudioClip[] effects){world=w;stats=s;music=m;local=l;spatial=pool;clips=effects;}
        private void Awake(){hp=world.Source.Player;weapons=world.Source.Weapons;attacks=new int[world.Source.Enemies.Length];dead=new bool[attacks.Length];doorFractions=new float[world.Doors.Length];doorDirections=new int[doorFractions.Length];exploded=new bool[world.Barrels.Length];}
        private void OnEnable(){hp.Damaged+=Damage;foreach(var p in world.Source.Pickups)p.Taken+=Pickup;stats.OnCompleted+=Exit;if(advancedClips!=null&&advancedClips.Length>0)Explosion.Occurred+=AdvancedExplosion;}
        private void OnDisable(){Explosion.Occurred-=AdvancedExplosion;if(hp)hp.Damaged-=Damage;if(world)foreach(var p in world.Source.Pickups)if(p)p.Taken-=Pickup;if(stats)stats.OnCompleted-=Exit;if(music)music.Stop();StopEffects();}
        private void Start(){Baseline();if(MusicEnabled)music.Play();}
        private bool ResetPressed=>diagnosticsEnabled&&Application.isFocused&&Keyboard.current!=null&&Keyboard.current.f9Key.wasPressedThisFrame;
        private void Baseline(){shots=weapons.Shots;lastResult=weapons.LastResult;position=hp.transform.position;raised=world.Lift.Raised;found=world.Secret.Found;for(int i=0;i<attacks.Length;i++){attacks[i]=world.Source.Enemies[i].Attacks;dead[i]=world.Source.Enemies[i].State==EnemyState.Dead;}for(int i=0;i<doorFractions.Length;i++){doorFractions[i]=world.Doors[i].Fraction;doorDirections[i]=0;}for(int i=0;i<exploded.Length;i++)exploded[i]=world.Barrels[i].Exploded;}
        public void RefreshAfterLoad(){StopEffects();Baseline();}
        private void StopEffects(){if(local)local.Stop();if(spatial!=null)foreach(var s in spatial)if(s)s.Stop();}
        private void PlayLocal(Cue c){if(EffectsEnabled&&!ResetPressed)local.PlayOneShot(clips[(int)c]);}
        private void PlayAt(Cue c,Vector3 p){if(!EffectsEnabled||ResetPressed)return;int index=-1;for(int i=0;i<spatial.Length;i++)if(!spatial[i].isPlaying){index=i;break;}if(index<0){index=voice;voice=(voice+1)%spatial.Length;}var s=spatial[index];s.Stop();s.transform.position=p;s.clip=clips[(int)c];s.Play();}
        private void AdvancedExplosion(Vector3 p)=>PlayAt(Cue.Explosion,p);
        private void Damage(Health _,DamageInfo d,DamageResult r){if(r.Applied)PlayLocal(r.Killed?Cue.Death:Cue.Pain);}
        private void Pickup(Pickup p)=>PlayLocal(p.Definition.Kind==PickupKind.Key?Cue.Key:Cue.Pickup);
        private void Exit(LevelStats s)=>PlayLocal(Cue.Exit);
        private void Update(){if(!diagnosticsEnabled)return;var k=Keyboard.current;if(k==null||!Application.isFocused)return;if(k.f11Key.wasPressedThisFrame){MusicEnabled=!MusicEnabled;if(MusicEnabled)music.Play();else music.Stop();}if(k.f12Key.wasPressedThisFrame){EffectsEnabled=!EffectsEnabled;if(!EffectsEnabled)StopEffects();}}
        public static int MotionDirection(float previous,float current){float d=current-previous;return d>.00001f?1:d<-.00001f?-1:0;}
        private void LateUpdate()
        {
            if(ResetPressed){StopEffects();Baseline();return;}
            if(weapons.Shots>shots){var selected=weapons.Selected;int index=selected?(int)selected.Weapon:-1;if(advancedClips!=null&&index>=0&&index<advancedClips.Length&&advancedClips[index]){if(EffectsEnabled&&!ResetPressed)local.PlayOneShot(advancedClips[index]);}else PlayLocal(selected&&selected.Weapon==WeaponId.Shotgun?Cue.Shotgun:Cue.Pistol);}shots=weapons.Shots;
            if(weapons.LastResult!=lastResult&&weapons.LastResult=="No ammo")PlayLocal(Cue.Empty);lastResult=weapons.LastResult;
            for(int i=0;i<attacks.Length;i++){var e=world.Source.Enemies[i];if(e.Attacks>attacks[i])PlayAt(e.Definition.AttackKind==EnemyAttackKind.Hitscan?Cue.Guard:Cue.Imp,e.transform.position);attacks[i]=e.Attacks;bool d=e.State==EnemyState.Dead;if(d&&!dead[i])PlayAt(Cue.EnemyDeath,e.transform.position);dead[i]=d;}
            for(int i=0;i<doorFractions.Length;i++){var d=world.Doors[i];int direction=MotionDirection(doorFractions[i],d.Fraction);if(direction!=0&&direction!=doorDirections[i])PlayAt(Cue.Door,d.transform.position);doorFractions[i]=d.Fraction;doorDirections[i]=direction;}
            if(raised!=world.Lift.Raised)PlayAt(Cue.Lift,world.Lift.transform.position);raised=world.Lift.Raised;
            for(int i=0;i<exploded.Length;i++){var b=world.Barrels[i];if(b.Exploded&&!exploded[i])PlayAt(Cue.Explosion,b.transform.position);exploded[i]=b.Exploded;}
            if(world.Secret.Found&&!found)PlayLocal(Cue.Secret);found=world.Secret.Found;
            // Observe a discontinuity arriving at the configured destination; not a general teleport event.
            var p=hp.transform.position;if(Vector3.Distance(position,p)>5f&&Vector3.Distance(p,world.Teleport.Destination.position)<.8f)PlayLocal(Cue.Teleport);position=p;
        }
    }
}
