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
        private static IEnumerator Safe(IEnumerator work,Action<Exception> completed)
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
            lift.MoveTo(1);lift.Tick(.8f);Require(player.transform.position.y>originalY+.65f,"Lift failed to carry the actual CharacterController.");
            var riding=saves.Capture();lift.Tick(4);yield return Restore(saves,riding);
            Require(Vector3.Distance(player.transform.position,riding.position)<.03f,"Passenger position lost during a moving-lift load.");
            lift.MoveTo(0);lift.Tick(4);Require(player.transform.position.y<originalY+.15f,"Lift failed to return its passenger safely.");
            record("Actual passenger transport, descent and mid-lift restore");yield return Restore(saves,initial);

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
        }
    }
}
