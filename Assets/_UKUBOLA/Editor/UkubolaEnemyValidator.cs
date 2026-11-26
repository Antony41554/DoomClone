using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;
using Setup=Ukubola.EditorTools.UkubolaEnemySetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaEnemyValidator
    {
        private static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
        [MenuItem("Tools/UKUBOLA/Phase 06/Validate Enemy Lab",false,72)]
        public static void Validate()
        {
            if(!Setup.CanRun())return;
            try{if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;CheckScene(Setup.OpenInternal());Debug.Log("[UKUBOLA P06] VALIDATION PASSED | 0 errors. Play checks still required.");}
            catch(Exception ex){Debug.LogError("[UKUBOLA P06] VALIDATION FAILED: "+ex.Message);}
        }
        internal static void CheckScene(Scene scene)
        {
            Check(scene.IsValid()&&scene.isLoaded&&scene.path==Setup.Lab,"Incorrect EnemyLab scene.");
            var weapon=Setup.Find<WeaponController>(scene).Single();var player=weapon.GetComponent<Health>();var motor=weapon.GetComponent<PlayerMotor>();var display=Setup.Find<RetroDisplay>(scene).Single();
            Check(player&&player.Armor&&motor&&motor.Config&&motor.Config.IsValid&&weapon.GetComponent<PlayerInputReader>()&&weapon.GetComponent<PlayerLife>()&&weapon.GetComponent<PlayerInventory>(),"Player components missing.");
            UkubolaCombatValidator.CheckDefinition(weapon.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(weapon.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
            Check(display.WorldCamera&&display.WorldCamera==weapon.AimCamera&&display.Target&&display.Target.width==320&&display.Target.height==200&&display.WorldCamera.targetTexture==display.Target&&display.Image&&display.Image.texture==display.Target&&weapon.View&&weapon.View.Image,"Retro display or weapon overlay missing.");
            Check(Setup.Find<Camera>(scene).Count(c=>c.isActiveAndEnabled)==2&&Setup.Find<AudioListener>(scene).Count(a=>a.isActiveAndEnabled)==1,"Unexpected camera/listener count.");
            var nav=Setup.Find<EnemyNavigation>(scene).Single();var noise=Setup.Find<NoiseEmitter>(scene).Single();var diag=Setup.Find<EnemyDiagnostics>(scene).Single();var actors=Setup.Find<EnemyBrain>(scene);
            Check(nav.Data&&EditorUtility.IsPersistent(nav.Data)&&nav.isActiveAndEnabled,"Baked navigation asset missing.");
            Check(noise.Weapons==weapon&&diag.Player==player&&diag.Weapons==weapon&&diag.Noise==noise&&diag.Spawn&&diag.Readout&&diag.Readout.font&&diag.Readout.transform.IsChildOf(display.Image.transform),"Diagnostics references invalid.");
            Check(actors.Length==2&&diag.Enemies!=null&&diag.Enemies.Length==2&&actors.All(a=>diag.Enemies.Contains(a))&&actors.Select(a=>a.Definition? a.Definition.AttackKind : (EnemyAttackKind)(-1)).Distinct().Count()==2,"Expected two enemy types.");
            Check(Setup.Find<CombatDiagnostics>(scene).Length==0&&Setup.Find<Health>(scene).Length==3,"Old combat diagnostics or unexpected Health components.");
            int enemyLayer=LayerMask.NameToLayer("Enemy"),playerLayer=LayerMask.NameToLayer("Player"),worldLayer=LayerMask.NameToLayer("World");
            Check(enemyLayer>=0&&playerLayer>=0&&worldLayer>=0,"Required layers missing.");
            int required=(1<<enemyLayer)|(1<<playerLayer)|(1<<worldLayer);
            Check((weapon.HitMask&required)==required&&(weapon.TargetMask&(1<<enemyLayer))!=0&&(display.WorldCamera.cullingMask&(1<<enemyLayer))!=0,"Enemy layer absent from combat or camera mask.");
            // Register temporarily to run actual Unity navigation queries in Edit mode.
            var instance=NavMesh.AddNavMeshData(nav.Data);
            try
            {
                Check(instance.valid,"Cannot register baked NavMesh for validation.");
                foreach(var a in actors)
                {
                    string prefix=a.name+": ";var def=a.Definition;var health=a.GetComponent<Health>();var agent=a.GetComponent<NavMeshAgent>();var body=a.GetComponent<CapsuleCollider>();var attack=a.GetComponent<EnemyAttack>();var sprite=a.GetComponent<EnemySpriteView>();
                    Check(def&&def.IsValid&&EditorUtility.IsPersistent(def),prefix+"invalid definition.");
                    Check(a.isActiveAndEnabled&&a.gameObject.layer==enemyLayer&&a.Target==player&&a.Viewer==display.WorldCamera&&a.Noise==noise&&a.Navigation==nav,prefix+"invalid scene references.");
                    Check(health&&health.MaxHealth==def.Health&&!health.Armor&&body&&body.enabled&&!body.isTrigger&&a.GetComponent<EntityId>(),prefix+"invalid health/collider/ID.");
                    Check(agent&&!agent.enabled&&agent.speed==def.Speed&&agent.radius>0,prefix+"invalid agent; must start disabled.");
                    Check(attack&&attack.ProjectilePrefab&&PrefabUtility.IsPartOfPrefabAsset(attack.ProjectilePrefab)&&(attack.Mask&required)==required,prefix+"invalid attack configuration.");
                    Check(sprite&&sprite.IsValid&&a.GetComponentInChildren<SpriteRenderer>()&&a.GetComponentInChildren<SpriteRenderer>().sharedMaterial&&a.GetComponentInChildren<SpriteRenderer>().sharedMaterial.shader.isSupported,prefix+"invalid sprite resources.");
                    var filter=new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};
                    Check(NavMesh.SamplePosition(a.transform.position,out var start,.8f,filter)&&Mathf.Abs(start.position.y-a.transform.position.y)<.45f,prefix+"spawn not on floor NavMesh.");
                    Check(NavMesh.SamplePosition(diag.Spawn.Position,out var end,.8f,filter)&&Mathf.Abs(end.position.y-diag.Spawn.Position.y)<.45f,prefix+"player spawn not on floor NavMesh.");
                    var path=new NavMeshPath();Check(NavMesh.CalculatePath(start.position,end.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete,prefix+"no complete navigation route to player spawn.");
                }
            }
            finally{if(instance.valid)instance.Remove();}
            var context=Setup.Find<LevelContext>(scene).Single();Check(context.LevelId==Setup.LevelId,"Wrong LevelContext ID.");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
            Check(Setup.Find<GameSession>(scene).Length==1&&Setup.Find<LevelExit>(scene).Single().Stats==Setup.Find<LevelStats>(scene).Single(),"Level flow missing.");
            foreach(var t in Setup.Find<Transform>(scene))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
        }
    }
}
