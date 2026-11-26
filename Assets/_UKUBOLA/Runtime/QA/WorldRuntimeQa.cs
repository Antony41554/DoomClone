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
    // Component-runtime invariants. This does not replace a complete combat route.
    public static class WorldRuntimeQa
    {
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        private static T Entity<T>(MissionContext context,string key) where T:PersistentBehaviour
        {
            string id=PersistentBehaviour.StableId(context.Definition.missionId,key);
            return context.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single(e=>e.PersistentId==id);
        }
        private static void Pose(Health player,Vector3 position)
        {
            var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;
            Physics.SyncTransforms();body.Move(Vector3.down*.02f);
        }
        internal static IEnumerator Safe(IEnumerator work,Action<Exception> completed)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);Exception problem=null;
            try
            {
                while(stack.Count>0)
                {
                    bool more=false;object current=null;
                    try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
                    catch(Exception error){problem=error;}
                    if(problem!=null)break;
                    if(!more)
                    {
                        try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){problem=error;}
                        if(problem!=null)break;continue;
                    }
                    if(current is IEnumerator nested)stack.Push(nested);else yield return current;
                }
            }
            finally
            {
                while(stack.Count>0)
                {
                    try{(stack.Pop() as IDisposable)?.Dispose();}
                    catch(Exception error){problem=problem==null?error:new AggregateException(problem,error);}
                }
            }
            completed?.Invoke(problem);
        }
        public static IEnumerator Run(WorldSaveController saves,Action<string> checkedCase,Action<Exception> completed)
        {
            if(!QaEnvironment.Active){completed?.Invoke(new InvalidOperationException("Runtime QA requires isolated storage."));yield break;}
            while(!saves.Ready)yield return null;
            var initial=saves.Capture();var player=saves.Player;var input=player.GetComponent<PlayerInputReader>();var motor=player.GetComponent<PlayerMotor>();
            bool inputEnabled=input.enabled,motorEnabled=motor.enabled;float scale=Time.timeScale;
            var actors=saves.Context.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EnemyBrain>(true)).ToArray();var suspended=actors.Select(a=>a.Suspended).ToArray();
            foreach(var actor in actors)actor.Suspended=true;input.enabled=false;motor.enabled=false;Time.timeScale=0;
            Exception problem=null,rollback=null;
            try
            {
                yield return Safe(Checks(saves,initial,checkedCase),error=>problem=error);
                yield return Safe(saves.Restore(initial,(ok,text)=>{if(!ok)rollback=new InvalidOperationException(text);}),error=>{if(error!=null)rollback=error;});
            }
            finally
            {
                Time.timeScale=scale;input.enabled=inputEnabled;motor.enabled=motorEnabled;
                for(int i=0;i<actors.Length;i++)if(actors[i])actors[i].Suspended=suspended[i];
            }
            completed?.Invoke(rollback==null?problem:problem==null?rollback:new AggregateException(problem,rollback));
        }
        private static IEnumerator Restore(WorldSaveController saves,MissionSnapshot data)
        {
            bool success=false;string message=null;
            yield return saves.Restore(data,(ok,text)=>{success=ok;message=text;});Require(success,"Restore failed: "+message);
        }
        private static IEnumerator Checks(WorldSaveController saves,MissionSnapshot initial,Action<string> record)
        {
            var world=saves.Context;var player=saves.Player;var bag=player.GetComponent<PlayerInventory>();int count=world.EntityCount;
            var door=Entity<WorldMover>(world,"control-door");
            door.Interact(new InteractionContext(bag,player));Require(!door.Moving,"Locked door opened without a card.");
            bag.TryGrantKeys(KeyId.BlueCard);door.Interact(new InteractionContext(bag,player));door.Tick(.4f);
            float fraction=door.Fraction;Vector3 panel=door.Panel.position;Require(door.Moving&&fraction>0&&fraction<1,"Key did not release the rigid door.");
            Require(saves.Save(),saves.LastStatus);Require(saves.Repository.TryRead(out var moving,out _,out var reason),reason);
            door.Tick(10);yield return Restore(saves,moving);
            Require(Mathf.Abs(door.Fraction-fraction)<.001f&&Vector3.Distance(door.Panel.position,panel)<.001f,"Door movement did not resume from its saved pose.");
            Require(bag.HasKeys(KeyId.BlueCard),"Card missing after load.");record("Locked door and mid-travel v2 disk round trip");
            yield return Restore(saves,initial);

            var lift=Entity<WorldMover>(world,"workshop-lift");Pose(player,new Vector3(6,.04f,24));float originalY=player.transform.position.y;
            var riderBody=player.GetComponent<CharacterController>();var deckBounds=lift.Panel.GetComponent<BoxCollider>().bounds;
            string riderBefore=" body="+riderBody.bounds+", enabled="+riderBody.enabled+", layer="+riderBody.gameObject.layer+", trigger="+riderBody.isTrigger+", deckBounds="+deckBounds;
            lift.MoveTo(1);lift.Tick(.8f);Require(Mathf.Abs(player.transform.position.y-originalY-.8f)<.12f,"Lift passenger delta differs from deck: before="+originalY+", after="+player.transform.position.y+", deck="+lift.Panel.position+", riders="+lift.LastRiderCount+", blocked="+lift.LastSafetyBlock+riderBefore);
            var riding=saves.Capture();lift.Tick(4);yield return Restore(saves,riding);
            Require(Vector3.Distance(player.transform.position,riding.position)<.03f,"Passenger position lost during a moving-lift load.");
            lift.MoveTo(0);lift.Tick(4);Require(player.transform.position.y<originalY+.15f,"Lift failed to return its passenger safely.");
            record("Actual passenger transport, descent and mid-lift restore");yield return Restore(saves,initial);

            Pose(player,new Vector3(6,.04f,24));var ceiling=new GameObject("QA lift head clearance");ceiling.layer=LayerMask.NameToLayer("World");ceiling.transform.position=new Vector3(6,2.25f,24);ceiling.AddComponent<BoxCollider>().size=new Vector3(2,.2f,2);Physics.SyncTransforms();
            try{lift.MoveTo(1);lift.Tick(.8f);Require(lift.Fraction==0&&lift.LastRiderCount==1&&!string.IsNullOrEmpty(lift.LastSafetyBlock),"Lift ignored an occupied head clearance.");}
            finally{UnityEngine.Object.Destroy(ceiling);}
            yield return null;lift.Tick(4);Pose(player,new Vector3(6,.04f,24));float raisedY=lift.Panel.position.y;lift.MoveTo(0);lift.Tick(4);
            Require(Mathf.Abs(lift.Panel.position.y-raisedY)<.001f&&lift.LastSafetyBlock=="person below platform","Lift descended onto a person beneath it.");
            record("Lift head clearance and protection beneath the raised deck");yield return Restore(saves,initial);

            var normal=Entity<WorldMover>(world,"reception-door");normal.MoveTo(1);normal.Tick(4);Pose(player,new Vector3(0,.04f,9));
            Time.timeScale=1;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Time.timeScale=0;
            normal.MoveTo(0);normal.Tick(4);Require(normal.TargetStop==1&&normal.LastSafetyBlock=="occupied doorway","Closing door did not reverse for its occupant.");
            record("Door obstruction reverses without crushing the actual player");yield return Restore(saves,initial);

            var hazard=Entity<WorldHazard>(world,"coolant-basin");player.GetComponent<Powerup>().Clear();player.RestoreSavedHealth(100);
            Pose(player,new Vector3(0,-.58f,39));Time.timeScale=1;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Require(player.Current==90,"Toxic volume did not damage the actual overlapping player once.");
            yield return new WaitForSeconds(1.05f);Time.timeScale=0;Require(player.Current==80,"Toxic exposure cadence differs from one second.");
            Pose(player,new Vector3(-6,.03f,38.3f));var pump=Entity<WorldRelay>(world,"coolant-pump");pump.Interact(new InteractionContext(bag,player));hazard.Tick(1.5f);
            var draining=saves.Capture();float level=hazard.Level;hazard.Tick(5);yield return Restore(saves,draining);
            Require(Mathf.Abs(hazard.Level-level)<.001f&&!hazard.Safe,"Drain state was not restored.");
            int rewards=0;var flag=Entity<WorldFlag>(world,"coolant_drained");Action<WorldSignal> reward=s=>{if(s==WorldSignal.Activated)rewards++;};flag.Signalled+=reward;
            try
            {
                hazard.Tick(level*4+.01f);Require(hazard.Safe&&world.Flag("coolant_drained")&&rewards==1,"Drain did not complete exactly once.");
                var drained=saves.Capture();yield return Restore(saves,drained);hazard.Tick(1);Require(rewards==1,"Loading replayed a drain reward.");
            }
            finally{flag.Signalled-=reward;}
            record("Physical toxic exposure, drain continuation and silent objective restore");yield return Restore(saves,initial);

            var enemy=Entity<WorldActorState>(world,"warehouse-imp");int deaths=0;Action<Health,DamageInfo> death=(hp,damage)=>deaths++;enemy.Health.Died+=death;
            try
            {
                enemy.Health.TakeDamage(new DamageInfo(10000,player.gameObject,DamageType.Hitscan,enemy.transform.position));
                Require(enemy.Health.IsDead&&enemy.Brain.State==EnemyState.Dead&&deaths==1,"Actor death did not update its combat phase.");var dead=saves.Capture();
                yield return Restore(saves,initial);Require(!enemy.Health.IsDead,"Living actor did not restore.");
                yield return Restore(saves,dead);Require(enemy.Health.IsDead&&!enemy.GetComponent<CapsuleCollider>().enabled&&deaths==1,"Corpse restore repeated death or restored collision.");
                Require(world.EntityCount==count,"Restore duplicated persistent actors.");
            }
            finally{enemy.Health.Died-=death;}
            record("Living/dead actor round trip without repeated death or duplicate identities");
            yield return Restore(saves,initial);

            var barrelA=Entity<WorldBarrelState>(world,"storage-barrel-a");var barrelB=Entity<WorldBarrelState>(world,"storage-barrel-b");
            barrelA.GetComponent<Health>().TakeDamage(new DamageInfo(100,player.gameObject,DamageType.Hitscan,barrelA.transform.position));
            Require(barrelA.GetComponent<ExplosiveBarrel>().Exploded&&barrelB.GetComponent<ExplosiveBarrel>().Exploded,"Authored barrel pair did not produce its intended chain reaction.");
            var exploded=saves.Capture();yield return Restore(saves,initial);yield return Restore(saves,exploded);
            Require(barrelA.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled)&&barrelB.GetComponent<ExplosiveBarrel>().Exploded,"Loaded barrels restored decoration or lost destruction.");
            record("Barrel chain reaction and persistent destruction of all visual parts");yield return Restore(saves,initial);

            var backpack=Entity<WorldPickupState>(world,"secret-backpack").Pickup;var secret=Entity<WorldSecret>(world,"maintenance-secret");
            int grants=0;Action<Pickup> grant=item=>grants++;backpack.Taken+=grant;
            try
            {
                Require(backpack.TryCollect(player,bag)&&bag.HasBackpack&&secret.Discover(),"Optional secret reward was unavailable.");var discovered=saves.Capture();
                yield return Restore(saves,initial);yield return Restore(saves,discovered);
                Require(backpack.Collected&&bag.HasBackpack&&secret.Found&&!backpack.TryCollect(player,bag)&&!secret.Discover()&&grants==1,"Secret load duplicated a reward or discovery.");
            }
            finally{backpack.Taken-=grant;}
            record("Secret and optional reward restore without duplicate grants");
            yield return Restore(saves,initial);

            var exit=Entity<WorldExit>(world,"transfer-exit");Require(!exit.Ready&&!exit.TryComplete(),"Exit ignored missing objectives.");
            Entity<WorldRelay>(world,"coolant-pump").Interact(new InteractionContext(bag,player));hazard.Tick(4);
            Require(!exit.Ready&&!Entity<WorldMover>(world,"exit-door").Moving,"One objective prematurely opened the exit.");
            Entity<WorldRelay>(world,"power-generator").Interact(new InteractionContext(bag,player));
            Require(!exit.Ready,"Power and coolant bypassed the encounter objective.");
            var encounter=Entity<WorldEncounter>(world,"control-encounter");encounter.Execute(WorldVerb.StartEncounter,0);
            var guard=Entity<WorldActorState>(world,"control-guard");var imp=Entity<WorldActorState>(world,"control-imp");
            guard.Health.TakeDamage(new DamageInfo(10000,player.gameObject,DamageType.Hitscan,guard.transform.position));var partialEncounter=saves.Capture();
            yield return Restore(saves,initial);yield return Restore(saves,partialEncounter);
            Require(encounter.Active&&!encounter.Completed&&guard.Health.IsDead&&!imp.Health.IsDead&&world.EntityCount==count,"Mid-encounter load changed membership or progress.");
            imp.Health.TakeDamage(new DamageInfo(10000,player.gameObject,DamageType.Hitscan,imp.transform.position));
            Time.timeScale=1;yield return null;yield return null;Time.timeScale=0;
            Require(encounter.Completed&&exit.Ready&&Entity<WorldMover>(world,"exit-door").Moving,"Three completed objectives did not release the exit.");
            int finishes=0;Action finish=()=>finishes++;exit.Finished+=finish;
            try
            {
                Require(exit.TryComplete()&&!exit.TryComplete()&&finishes==1,"Exit completed more than once.");var finished=saves.Capture();
                yield return Restore(saves,initial);yield return Restore(saves,finished);
                Require(exit.Completed&&!exit.TryComplete()&&finishes==1&&world.EntityCount==count,"Loading replayed mission completion or duplicated encounter members.");
            }
            finally{exit.Finished-=finish;}
            record("Three objective conditions, mid-encounter load and exactly-once mission completion");
        }
    }
}
