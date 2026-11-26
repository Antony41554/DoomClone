using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Ukubola.UI;
using Ukubola.Audio;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;
using Setup=Ukubola.EditorTools.UkubolaMvpSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaMvpValidator
    {
        internal static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
        internal static void CheckBuildList()
        {
            var scenes=EditorBuildSettings.scenes;
            foreach(string path in new[]{Setup.Title,Setup.Mission})Check(scenes.Count(x=>x.path==path)==1&&scenes.Any(x=>x.path==path&&x.enabled),"MVP scene not uniquely enabled: "+path);
        }
        private static MenuFlow Common(Scene scene,string path,bool title,int cameras)
        {
            Check(scene.IsValid()&&scene.isLoaded&&scene.path==path,"Wrong scene: "+path);
            var flow=Setup.Find<MenuFlow>(scene).Single();Check(flow&&flow.isActiveAndEnabled&&flow.TitleScene==title&&flow.Typeface&&flow.RouteTitle==Setup.Title&&flow.RouteGame==Setup.Mission&&flow.MissionLabel==Setup.Label,"Invalid menu route or font.");
            Check(Setup.Find<Camera>(scene).Count(c=>c.isActiveAndEnabled)==cameras&&Setup.Find<AudioListener>(scene).Count(a=>a.isActiveAndEnabled)==1,"Wrong active camera/listener count.");
            Check(Setup.Find<EventSystem>(scene).Length==0,"Duplicate EventSystem (created by MenuFlow at runtime).");
            foreach(var t in Setup.Find<Transform>(scene))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
            return flow;
        }
        internal static void CheckTitle(Scene scene)
        {
            var f=Common(scene,Setup.Title,true,1);Check(!f.Hud&&f.TitleMusic&&f.TitleMusic.clip&&f.TitleMusic.loop&&!f.TitleMusic.playOnAwake,"Invalid title music.");
            Check(Setup.Find<PlayerMotor>(scene).Length==0,"Title contains player.");
            var camera=Setup.Find<Camera>(scene).Single();Check(camera.CompareTag("MainCamera")&&camera.targetTexture==null&&camera.cullingMask==0,"Invalid title camera.");
        }
        internal static void CheckMission(Scene scene)
        {
            var flow=Common(scene,Setup.Mission,false,2);var hud=Setup.Find<ClassicHud>(scene).Single();var world=Setup.Find<WorldDiagnostics>(scene).Single();var audio=Setup.Find<GameAudio>(scene).Single();var stats=Setup.Find<LevelStats>(scene).Single();var exit=Setup.Find<LevelExit>(scene).Single();var mission=Setup.Find<MissionObjective>(scene).Single();var source=world.Source;
            Check(flow.Hud==hud&&!flow.TitleMusic&&hud.World==world&&hud.Audio==audio&&hud.Stats==stats&&audio.World==world&&audio.Stats==stats,"Invalid HUD/audio/flow connections.");
            Check(source&&source.Player&&source.Player.Armor&&source.Weapons&&source.Noise&&source.Spawn&&source.Player.GetComponent<PlayerMotor>()&&source.Player.GetComponent<PlayerInputReader>()&&source.Player.GetComponent<PlayerInventory>(),"Player systems missing.");
            Check(!source.enabled&&!world.enabled&&!hud.DiagnosticsEnabled&&!audio.DiagnosticsEnabled&&source.Readout&&!source.Readout.transform.parent.gameObject.activeSelf,"Diagnostic input must be disabled.");
            Check(exit.Stats==stats&&mission.GetComponent<LevelExit>()==exit&&mission.Stats==stats&&mission.Health==source.Player&&mission.Inventory==source.Player.GetComponent<PlayerInventory>()&&mission.ObjectiveLabel&&mission.ObjectiveLabel.font,"Broken mission exit links.");
            Check(world.Doors.Length==4&&world.Doors.All(d=>d)&&world.Switches.Length==2&&world.Switches.All(s=>s)&&world.Lift&&world.Zone&&world.Teleport&&world.Teleport.Destination&&world.Secret&&world.Barrels.Length==2&&world.Barrels.All(b=>b),"World mechanisms missing.");
            Check(mission.RemoteDoor&&mission.RemoteDoor.SwitchOnly&&world.Doors.Contains(mission.RemoteDoor)&&world.Switches.Count(s=>s.Door==mission.RemoteDoor)==1,"Remote door/switch link invalid.");
            Check(exit.GetComponent<BoxCollider>().isTrigger&&exit.GetComponent<Rigidbody>().isKinematic&&exit.Indicator,"Exit trigger invalid.");
            var display=Setup.Find<RetroDisplay>(scene).Single();Check(display.WorldCamera==source.Weapons.AimCamera&&display.Target&&display.Target.width==320&&display.Target.height==200&&display.Image&&display.Image.texture==display.Target&&display.WorldCamera.targetTexture==display.Target&&mission.ObjectiveLabel.transform.IsChildOf(display.Image.transform),"Broken retro frame.");
            Check(hud.Labels.Length==9&&hud.Labels.All(t=>t&&t.font)&&hud.Faces.Length==HudRules.FaceCount&&hud.Faces.All(t=>t)&&audio.Music&&audio.Music.clip&&audio.Local&&audio.Spatial.Length==8&&audio.Spatial.All(a=>a),"HUD/audio resources missing.");
            UkubolaCombatValidator.CheckDefinition(source.Weapons.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(source.Weapons.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
            var actors=Setup.Find<EnemyBrain>(scene);var items=Setup.Find<Pickup>(scene);var nav=Setup.Find<EnemyNavigation>(scene).Single();var level=Setup.Require<LevelDefinition>(UkubolaLevelSetup.Data);MvpLayout.Check(level);
            Check(actors.Length==MvpLayout.Enemies.Length&&source.Enemies.Length==actors.Length&&actors.All(a=>source.Enemies.Contains(a)),"Enemy array differs from scene.");
            Check(items.Length==MvpLayout.Items.Length&&source.Pickups.Length==items.Length&&items.All(p=>source.Pickups.Contains(p)),"Pickup array differs from scene.");
            Check(items.Count(p=>p.Definition&&p.Definition.Kind==PickupKind.Key&&p.Definition.Key==KeyId.BlueCard)==1,"Exactly one mandatory blue card required.");
            foreach(var spec in MvpLayout.Items)
            {
                var p=items.Single(x=>x.name==spec.Name);Check(p.Definition&&p.Definition.IsValid&&AssetDatabase.GetAssetPath(p.Definition)==UkubolaPickupSetup.Data+"/"+spec.Prefab+".asset"&&p.Visual&&p.GetComponent<EntityId>()&&p.GetComponent<SphereCollider>().isTrigger&&p.GetComponent<Rigidbody>().isKinematic,"Invalid pickup: "+spec.Name);
                Check(Vector3.Distance(p.transform.position,level.Center(spec.Cell))<.02f,"Wrong pickup position: "+spec.Name);
            }
            Check(nav.Data&&EditorUtility.IsPersistent(nav.Data)&&nav.isActiveAndEnabled&&source.Noise.Weapons==source.Weapons,"Navigation/noise invalid.");
            foreach(var spec in MvpLayout.Enemies)
            {
                var a=actors.Single(x=>x.name==spec.Name);var agent=a.GetComponent<NavMeshAgent>();var hp=a.GetComponent<Health>();var body=a.GetComponent<CapsuleCollider>();var view=a.GetComponent<EnemySpriteView>();var attack=a.GetComponent<EnemyAttack>();
                Check(a.Definition&&a.Definition.IsValid&&AssetDatabase.GetAssetPath(a.Definition)==UkubolaEnemySetup.Data+"/"+spec.Prefab+".asset"&&a.Target==source.Player&&a.Viewer==display.WorldCamera&&a.Noise==source.Noise&&a.Navigation==nav,"Invalid enemy links: "+spec.Name);
                Check(agent&&!agent.enabled&&body&&body.enabled&&!body.isTrigger&&hp&&hp.MaxHealth==a.Definition.Health&&view&&view.IsValid&&attack&&attack.ProjectilePrefab&&a.GetComponent<EntityId>(),"Invalid enemy components: "+spec.Name);
                Check(Vector3.Distance(a.transform.position,level.Center(spec.Cell))<.02f,"Wrong enemy position: "+spec.Name);
            }
            CheckBaseRoutes(nav.Data,actors,source.Spawn.Position);
            var context=Setup.Find<LevelContext>(scene).Single();Check(context.LevelId==Setup.LevelId,"Incorrect mission ID.");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
        }
        // Test the baked surface, not the scene's closed-door carving. The translated
        // instance is temporary and has no effect on the gameplay navigation asset.
        internal static void CheckBaseRoutes(NavMeshData data,EnemyBrain[] actors,Vector3 spawn)
        {
            Check(data&&actors!=null&&actors.Length>0,"Missing base navigation data or actors.");
            var offset=new Vector3(1024f,128f,1024f);
            var instance=NavMesh.AddNavMeshData(data,offset,Quaternion.identity);
            try
            {
                Check(instance.valid,"Cannot register isolated NavMesh for validation.");
                foreach(var a in actors)
                {
                    var agent=a.GetComponent<NavMeshAgent>();
                    Check(agent,"Missing agent: "+a.name);
                    var filter=new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};
                    Vector3 from=a.transform.position+offset,to=spawn+offset;
                    Check(NavMesh.SamplePosition(from,out var start,.8f,filter)&&Mathf.Abs(start.position.y-from.y)<.45f,
                        "Enemy not on isolated base NavMesh: "+a.name+" at "+a.transform.position);
                    Check(NavMesh.SamplePosition(to,out var end,.8f,filter)&&Mathf.Abs(end.position.y-to.y)<.45f,
                        "Player spawn not on isolated base NavMesh: "+spawn);
                    var path=new NavMeshPath();
                    bool calculated=NavMesh.CalculatePath(start.position,end.position,filter,path);
                    Check(calculated&&path.status==NavMeshPathStatus.PathComplete,
                        "Isolated base NavMesh route failed: "+a.name+" | status="+path.status+" | from="+(start.position-offset)+" | to="+(end.position-offset)+". Run Repair MVP Navigation.");
                }
            }
            finally{if(instance.valid)instance.Remove();}
        }
        [MenuItem("Tools/UKUBOLA/Phase 11/Validate MVP",false,113)]
        public static void Validate()
        {
            if(!Setup.CanRun())return;try{if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;CheckBuildList();CheckMission(EditorSceneManager.OpenScene(Setup.Mission,OpenSceneMode.Single));CheckTitle(EditorSceneManager.OpenScene(Setup.Title,OpenSceneMode.Single));Debug.Log("[UKUBOLA P11] VALIDATION PASSED | 0 errors. Play checks still required.");}catch(Exception ex){Debug.LogError("[UKUBOLA P11] VALIDATION FAILED: "+ex);}
        }
    }
}
