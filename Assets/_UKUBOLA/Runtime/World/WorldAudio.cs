using System;
using System.Linq;
using UnityEngine;
using Ukubola.Audio;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    [DefaultExecutionOrder(210)]
    public sealed class WorldAudio : MonoBehaviour
    {
        [SerializeField] private MissionContext context;
        [SerializeField] private Health player;
        [SerializeField] private WeaponController weapons;
        [SerializeField] private AudioClip[] cues;
        [SerializeField] private AudioClip musicClip;
        private AudioSource music, local, status;
        private AudioSource[] voices;
        private Pickup[] pickups;
        private WorldSecret[] secrets;
        private WorldExit exit;
        private WorldMover[] movers;
        private WorldBarrelState[] barrels;
        private WorldActorState[] actors;
        private int shots, voice;
        private string lastResult;
        private int[] attacks;
        private bool paused;
        public void Configure(MissionContext world,Health life,WeaponController guns,AudioClip[] effects,AudioClip musicTrack)
        {context=world;player=life;weapons=guns;cues=effects;musicClip=musicTrack;}
        private T[] InScene<T>() where T:Component=>gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        private AudioSource Source(string label,bool spatial)
        {
            var root=new GameObject(label);root.transform.SetParent(transform,false);var sound=root.AddComponent<AudioSource>();
            sound.playOnAwake=false;sound.spatialBlend=spatial?1:0;sound.volume=spatial?.65f:.7f;sound.rolloffMode=AudioRolloffMode.Linear;
            sound.minDistance=2;sound.maxDistance=24;sound.dopplerLevel=0;sound.priority=spatial?128:64;return sound;
        }
        private void Awake()
        {
            if(!context||!player||!weapons||cues==null||cues.Length!=Enum.GetValues(typeof(Cue)).Length||cues.Any(c=>!c))
                throw new InvalidOperationException("Incomplete mission audio references.");
            music=Source("Mission music",false);music.loop=true;music.volume=.2f;music.clip=musicClip;
            local=Source("Player sound",false);status=Source("Status sound",false);status.ignoreListenerPause=true;voices=Enumerable.Range(0,12).Select(i=>Source("World voice "+i,true)).ToArray();
            pickups=InScene<Pickup>();secrets=InScene<WorldSecret>();exit=InScene<WorldExit>().Single();movers=InScene<WorldMover>();barrels=InScene<WorldBarrelState>();actors=InScene<WorldActorState>();attacks=new int[actors.Length];
        }
        private void OnEnable()
        {
            player.Damaged+=Damage;foreach(var item in pickups)item.Taken+=Pickup;foreach(var secret in secrets)secret.Discovered+=Secret;
            exit.Finished+=Exit;foreach(var mover in movers)mover.Signalled+=Mover;foreach(var barrel in barrels)barrel.Signalled+=Barrel;
            foreach(var actor in actors)actor.Health.Died+=EnemyDeath;
        }
        private void Start(){Baseline();if(music.clip)music.Play();}
        private void OnDisable()
        {
            if(player)player.Damaged-=Damage;if(pickups!=null)foreach(var item in pickups)if(item)item.Taken-=Pickup;
            if(secrets!=null)foreach(var secret in secrets)if(secret)secret.Discovered-=Secret;if(exit)exit.Finished-=Exit;
            if(movers!=null)foreach(var mover in movers)if(mover)mover.Signalled-=Mover;if(barrels!=null)foreach(var barrel in barrels)if(barrel)barrel.Signalled-=Barrel;
            if(actors!=null)foreach(var actor in actors)if(actor)actor.Health.Died-=EnemyDeath;
            if(music)music.Stop();StopEffects();
        }
        private void Baseline(){shots=weapons.Shots;lastResult=weapons.LastResult;for(int i=0;i<actors.Length;i++)attacks[i]=actors[i].Brain.Attacks;}
        private void StopEffects(){if(local)local.Stop();if(status)status.Stop();if(voices!=null)foreach(var source in voices)if(source)source.Stop();}
        private bool Audible=>context&&!context.Restoring&&Time.timeScale>0;
        private void Here(Cue cue)
        {
            if(!context||context.Restoring)return;
            if(cue==Cue.Death||cue==Cue.Exit)status.PlayOneShot(cues[(int)cue]);
            else if(Audible)local.PlayOneShot(cues[(int)cue]);
        }
        private void At(Cue cue,Vector3 position)
        {
            if(!Audible)return;var source=voices.FirstOrDefault(s=>!s.isPlaying)??voices[voice++%voices.Length];
            source.Stop();source.transform.position=position;source.clip=cues[(int)cue];source.Play();
        }
        private void Damage(Health actor,DamageInfo damage,DamageResult result){if(result.Applied)Here(result.Killed?Cue.Death:Cue.Pain);}
        private void Pickup(Pickup item)=>Here(item.Definition.Kind==PickupKind.Key?Cue.Key:Cue.Pickup);
        private void Secret()=>Here(Cue.Secret);
        private void Exit()=>Here(Cue.Exit);
        // The signal carries no source payload; use one bounded local mechanical cue.
        // Positional per-device loops are a P20/P24 refinement, not a growing source list.
        private void Mover(WorldSignal signal){if(signal==WorldSignal.Activated)Here(Cue.Door);}
        private void Barrel(WorldSignal signal){if(signal==WorldSignal.Killed)Here(Cue.Explosion);}
        private void EnemyDeath(Health actor,DamageInfo damage)=>At(Cue.EnemyDeath,actor.transform.position);
        private void LateUpdate()
        {
            bool stop=Time.timeScale<=0;
            if(paused!=stop){paused=stop;foreach(var source in voices.Concat(new[]{music,local}))if(stop)source.Pause();else source.UnPause();}
            if(context.Restoring){StopEffects();Baseline();return;}
            if(!Audible)return;
            if(weapons.Shots>shots)Here(weapons.Selected.Weapon==WeaponId.Shotgun?Cue.Shotgun:Cue.Pistol);shots=weapons.Shots;
            if(weapons.LastResult!=lastResult&&weapons.LastResult=="No ammo")Here(Cue.Empty);lastResult=weapons.LastResult;
            for(int i=0;i<actors.Length;i++){var brain=actors[i].Brain;if(brain.Attacks>attacks[i])At(brain.Definition.AttackKind==EnemyAttackKind.Hitscan?Cue.Guard:Cue.Imp,brain.transform.position);attacks[i]=brain.Attacks;}
        }
    }
}
