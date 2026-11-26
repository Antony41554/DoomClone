using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    [DefaultExecutionOrder(-200)]
    public sealed class WorldRouteQa:MonoBehaviour,IPlayerCommands
    {
        private WorldSaveController saves;
        private Health player;
        private PlayerMotor motor;
        private PlayerInputReader input;
        private WeaponController guns;
        private WorldActorState[] actors;
        private Vector3? goal,manualAim;
        private bool combat=true;
        private int useFrame=-1,slotFrame=-1,slot,previousShots;
        private readonly HashSet<WeaponId> used=new HashSet<WeaponId>();
        private Action<string> record;
        private Action<float> sample;
        public bool Active=>QaEnvironment.Active&&isActiveAndEnabled;
        public Vector2 Move{get;private set;}
        public Vector2 Look{get;private set;}
        public bool Run=>false;
        public bool FireHeld{get;private set;}
        public bool UsePressed=>useFrame==Time.frameCount;
        public int WeaponSlot=>slotFrame==Time.frameCount?slot:0;
        private T Entity<T>(string key)where T:PersistentBehaviour
        {
            string id=PersistentBehaviour.StableId(saves.Context.Definition.missionId,key);
            return saves.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single(e=>e.PersistentId==id);
        }
        private static void Require(bool value,string why){if(!value)throw new InvalidOperationException(why);}
        private WorldActorState Threat()
        {
            return actors.Where(a=>!a.Health.IsDead&&!a.Brain.Suspended&&Vector3.Distance(a.transform.position,player.transform.position)<12&&a.GetComponent<EnemyAttack>().CanSee(player))
                .OrderBy(a=>Vector3.SqrMagnitude(a.transform.position-player.transform.position)).FirstOrDefault();
        }
        private void Update()
        {
            Move=Look=Vector2.zero;FireHeld=false;
            if(!player||player.IsDead||Time.timeScale<=0)return;
            sample?.Invoke(Time.unscaledDeltaTime*1000);
            if(guns.Shots!=previousShots){used.Add(guns.Selected.Weapon);previousShots=guns.Shots;}
            var threat=combat?Threat():null;
            Vector3? aim=manualAim??(threat?(Vector3?)threat.transform.position:goal);
            if(aim.HasValue)
            {
                Vector3 direction=aim.Value-player.transform.position;float desired=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                float delta=Mathf.DeltaAngle(player.transform.eulerAngles.y,desired);
                Look=new Vector2(Mathf.Clamp(delta,-180*Time.deltaTime,180*Time.deltaTime)/(motor.Config.MouseSensitivity*Mathf.Clamp(motor.SensitivityMultiplier,.5f,2)),0);
                FireHeld=threat&&!manualAim.HasValue&&Mathf.Abs(delta)<5;
            }
            if(goal.HasValue)
            {
                Vector3 direction=goal.Value-player.transform.position;direction.y=0;
                if(direction.magnitude>.12f){direction=direction.normalized*Mathf.Min(1,direction.magnitude*2);Move=new Vector2(Vector3.Dot(player.transform.right,direction),Vector3.Dot(player.transform.forward,direction));}
            }
            if(guns.Selected&&guns.Selected.UsesAmmo&&player.GetComponent<PlayerInventory>().Count(guns.Selected.Ammo)==0){slot=2;slotFrame=Time.frameCount;}
        }
        private void Alive()
        {
            Require(!player.IsDead,"Route player died at "+player.transform.position+" after "+guns.Shots+" shots.");
        }
        private IEnumerator Go(float x,float z,float timeout=18)
        {
            goal=new Vector3(x,0,z);manualAim=null;combat=true;float deadline=Time.unscaledTime+timeout;
            while(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(x,z))>.22f)
            {Alive();Require(Time.unscaledTime<deadline,"Route blocked on the way to "+goal+"; actual="+player.transform.position);yield return null;}
            goal=null;yield return new WaitForSeconds(.15f);
        }
        private IEnumerator Fight()
        {
            goal=manualAim=null;combat=true;float deadline=Time.unscaledTime+25;
            yield return new WaitForFixedUpdate();yield return null;
            while(Threat()){Alive();Require(Time.unscaledTime<deadline,"Combat failed to resolve with normal ammunition at "+player.transform.position);yield return null;}
            yield return new WaitForSeconds(.15f);
        }
        private IEnumerator Use(Transform target)
        {
            goal=null;combat=false;manualAim=target.position;float deadline=Time.unscaledTime+4;
            while(true)
            {
                Alive();Vector3 direction=target.position-player.transform.position;float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                if(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y,yaw))<1)break;
                Require(Time.unscaledTime<deadline,"Could not face interaction target "+target.name);yield return null;
            }
            yield return null;useFrame=Time.frameCount;yield return null;yield return null;manualAim=null;combat=true;
        }
        private IEnumerator Wait(Func<bool> condition,string message,float timeout=6)
        {
            float deadline=Time.unscaledTime+timeout;
            while(!condition()){Alive();Require(Time.unscaledTime<deadline,message);yield return null;}
        }
        private void Stage(string label)
        {
            record(label+" | pos="+player.transform.position+" hp="+player.Current+" shots="+guns.Shots+" hits="+guns.DamageHits);
        }
        private IEnumerator Route()
        {
            bool reset=false;yield return saves.Restart((ok,text)=>reset=ok);Require(reset,"Fresh mission restart failed.");
            input.enabled=motor.enabled=true;input.SetQaCommands(this);player.GetComponent<Powerup>().Clear();
            Stage("Live route start: fresh gear, active enemies, normal damage");
            yield return Fight();Require(used.Contains(WeaponId.Pistol),"Entry combat never fired the pistol.");
            yield return Go(1,4);Require(player.GetComponent<PlayerInventory>().HasWeapon(WeaponId.Shotgun),"Walking over the shotgun did not collect it.");
            slot=3;slotFrame=Time.frameCount;yield return null;yield return Go(2,4);yield return Go(0,7.1f);
            var door=Entity<WorldMover>("reception-door");yield return Use(door.Panel);yield return Wait(()=>door.AtFinalStop,"Reception door did not open through Use.");
            yield return Go(0,11.5f);yield return Go(3,14);yield return Fight();yield return Go(6,15);
            Require(player.GetComponent<PlayerInventory>().HasKeys(KeyId.BlueCard),"Warehouse card cannot be collected by walking.");Stage("Warehouse combat, shotgun and blue card");
            yield return Go(0,11);yield return Go(-4.3f,10);yield return Go(-7,10);yield return Go(-7,11.5f);yield return Go(-7,17);
            Require(player.transform.position.y>2.2f,"The normal motor could not climb the service stairs.");yield return Go(-7,24);Stage("Upper walkway reached without jumping");
            yield return Go(-7,17);yield return Go(-7,11.3f);yield return Go(-5,10);yield return Go(-5,18);yield return Go(-7,20);
            var grille=Entity<WorldMover>("maintenance-grille");yield return Use(grille.Panel);yield return Wait(()=>grille.AtFinalStop,"Maintenance panel did not open.");yield return Go(-7,23.7f);
            Require(Entity<WorldSecret>("maintenance-secret").Found&&player.GetComponent<PlayerInventory>().HasBackpack,"Secret access or reward is blocked.");Stage("Maintenance secret entered and reward collected");
            yield return Go(-7,20);yield return Go(0,20);yield return Go(2.8f,22.5f);yield return Fight();
            yield return Use(Entity<WorldRelay>("transfer-switch").transform);var transfer=Entity<WorldMover>("transfer-door");yield return Wait(()=>transfer.AtFinalStop,"Remote switch did not open transfer access.");
            yield return Go(6,24);var lift=Entity<WorldMover>("workshop-lift");yield return Use(Entity<WorldRelay>("lift-lower-call").transform);
            yield return Wait(()=>lift.AtFinalStop,"Cargo lift did not reach its upper stop.");Require(player.transform.position.y>2.2f,"Live lift left its passenger below.");
            yield return Go(6,27.35f);Require(player.Armor.Points>0,"Upper lift reward is unreachable.");yield return Go(6,24.5f);yield return Use(Entity<WorldRelay>("lift-upper-call").transform);
            yield return Wait(()=>!lift.Moving&&lift.TargetStop==0,"Upper call did not return the platform.");Require(player.transform.position.y<.2f,"Lift passenger failed to descend.");Stage("Both lift calls, ride and armor reward");
            Require(saves.Save(),saves.LastStatus);bool loaded=false;yield return saves.LoadStored((ok,text)=>loaded=ok);Require(loaded,"Live route save/load failed.");
            yield return Go(0,25);yield return Go(0,34.8f);yield return Go(-6,35.2f);yield return Go(-6,38.2f);yield return Fight();
            yield return Use(Entity<WorldRelay>("coolant-pump").transform);yield return Wait(()=>Entity<WorldHazard>("coolant-basin").Safe,"Pump failed to drain coolant.");Stage("Safe depot route and physical pump interaction");
            yield return Go(-5,41.5f);yield return Go(0,41.3f);var blue=Entity<WorldMover>("control-door");yield return Use(blue.Panel);yield return Wait(()=>blue.AtFinalStop,"Blue authorization door refused the collected card.");
            yield return Go(0,44.4f);yield return Fight();yield return Go(-5,44.3f);yield return Use(Entity<WorldRelay>("power-generator").transform);
            yield return Wait(()=>Entity<WorldExit>("transfer-exit").Ready,"Control encounter or power objective did not finish.",12);
            Require(actors.All(a=>a.Health.IsDead)&&used.Contains(WeaponId.Shotgun)&&guns.DamageHits>0,"Live combat left enemies alive or did not use both weapons.");Stage("Control ambush resolved using normal weapons");
            var final=Entity<WorldMover>("exit-door");yield return Wait(()=>final.AtFinalStop,"Exit panel did not clear the route.");yield return Go(0,47.2f);yield return Go(0,50);
            yield return Wait(()=>Entity<WorldExit>("transfer-exit").Completed,"Walking into the exit did not finish the mission.");Stage("Live mission completed through its exit trigger");
            player.TakeDamage(new DamageInfo(10000,null,DamageType.Environmental,player.transform.position));
            Require(player.IsDead&&!motor.enabled&&!player.GetComponent<CharacterController>().enabled,"Death did not stop player movement/collision.");
            reset=false;yield return saves.Restart((ok,text)=>reset=ok);Require(reset&&!player.IsDead&&motor.enabled&&player.GetComponent<CharacterController>().enabled,"Death retry failed to restore a playable fresh mission.");
            Stage("Death and fresh retry restored player, objectives and encounter");
        }
        public static IEnumerator Execute(WorldSaveController saves,Action<string> record,Action<float> sample,Action<Exception> done)
        {
            if(!QaEnvironment.Active){done(new InvalidOperationException("Live route requires isolated QA."));yield break;}
            while(!saves.Ready)yield return null;
            var go=new GameObject("UKUBOLA live route QA");var driver=go.AddComponent<WorldRouteQa>();driver.saves=saves;driver.player=saves.Player;
            driver.input=saves.Player.GetComponent<PlayerInputReader>();driver.motor=saves.Player.GetComponent<PlayerMotor>();driver.guns=saves.Player.GetComponent<WeaponController>();
            driver.actors=FindObjectsByType<WorldActorState>(FindObjectsSortMode.None);driver.record=record;driver.sample=sample;
            Exception problem=null;
            try{yield return WorldRuntimeQa.Safe(driver.Route(),error=>problem=error);}
            finally{driver.input.SetQaCommands(null);Destroy(go);}
            done(problem);
        }
    }
}
