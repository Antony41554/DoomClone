using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Enemies;
using Ukubola.World;
using Ukubola.Rendering;
using Ukubola.UI;
using Setup=Ukubola.EditorTools.UkubolaCampaignSetup;
using MVP=Ukubola.EditorTools.UkubolaMvpSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaCampaignValidator
    {
        private static void Check(bool ok,string text)=>Setup.Check(ok,text);
        private static void Near(Vector3 actual,Vector3 expected,string name)=>Check(Vector3.Distance(actual,expected)<.025f,"Wrong position: "+name+" at "+actual+" expected "+expected);
        private static MenuFlow Common(Scene s,int index)
        {
            bool title=index<0;string path=title?Setup.Title:Setup.Missions[index];Check(s.isLoaded&&s.path==path,"Wrong campaign scene: "+s.path);var f=Setup.Find<MenuFlow>(s).Single();var stage=Setup.Find<CampaignStage>(s).Single();
            Check(f.isActiveAndEnabled&&f.Typeface&&f.TitleScene==title&&f.RouteTitle==Setup.Title&&f.RouteGame==(title?Setup.Missions[0]:path),"Incorrect campaign flow");
            Check(stage.gameObject==f.gameObject&&stage.Index==index&&stage.CurrentPath==path&&(stage.NextPath??"") == (index>=0&&index<2?Setup.Missions[index+1]:""),"Incorrect campaign transition");
            Check(Setup.Find<Camera>(s).Count(x=>x.isActiveAndEnabled)==(title?1:2)&&Setup.Find<AudioListener>(s).Count(x=>x.isActiveAndEnabled)==1&&Setup.Find<EventSystem>(s).Length==0,"Camera, listener or EventSystem mismatch");
            foreach(var t in Setup.Find<Transform>(s))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script on "+t.name);return f;
        }
        internal static void CheckRegistered(){foreach(var path in Setup.Paths)Check(EditorBuildSettings.scenes.Count(x=>x.enabled&&x.path==path)==1,"Campaign scene not registered: "+path);}
        internal static void CheckTitle(Scene s){var f=Common(s,-1);Check(!f.Hud&&f.TitleMusic&&f.TitleMusic.clip&&f.TitleMusic.loop&&!f.TitleMusic.playOnAwake&&Setup.Find<PlayerMotor>(s).Length==0,"Invalid campaign title");}
        internal static void CheckMission(Scene s,int index)
        {
            var f=Common(s,index);var hud=f.Hud;Check(f.MissionLabel==Setup.Labels[index]&&hud&&hud.World&&hud.Audio&&hud.Stats&&!hud.DiagnosticsEnabled&&!hud.Audio.DiagnosticsEnabled,"Invalid HUD, audio or flow");
            var world=hud.World;var source=world.Source;Check(source&&source.Player&&source.Weapons&&source.Noise&&source.Spawn&&source.Readout&&!source.enabled&&!world.enabled,"Missing source or enabled diagnostics");
            var hp=source.Player;var guns=source.Weapons;var display=Setup.Find<RetroDisplay>(s).Single();var bag=hp.GetComponent<PlayerInventory>();Check(bag&&hp.Armor&&hp.GetComponent<Powerup>()&&display.WorldCamera==guns.AimCamera&&display.Target&&display.Target.width==320&&display.Target.height==200,"Player or retro display invalid");
            UkubolaCombatValidator.CheckDefinition(guns.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(guns.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
            Check(guns.Advanced!=null&&guns.Advanced.Length==6&&guns.Advanced.All(x=>x&&x.IsValid)&&guns.Advanced.Select(x=>x.Weapon).Distinct().Count()==6&&guns.AdvancedProjectile&&hud.Audio.AdvancedClips!=null&&hud.Audio.AdvancedClips.Length==8,"Advanced weapons or clips invalid");
            var actors=Setup.Find<EnemyBrain>(s);var items=Setup.Find<Pickup>(s);int expectedActors=6+index*2,expectedItems=index==0?25:24;
            Check(actors.Length==expectedActors&&source.Enemies.Length==expectedActors&&actors.All(x=>source.Enemies.Contains(x)),"Enemy count or HUD references invalid");
            Check(items.Length==expectedItems&&source.Pickups.Length==expectedItems&&items.All(x=>source.Pickups.Contains(x)),"Pickup count or HUD references invalid");
            var nav=Setup.Find<EnemyNavigation>(s).Single();Check(nav.Data&&EditorUtility.IsPersistent(nav.Data),"Missing base NavMesh");
            foreach(var a in actors){var agent=a.GetComponent<NavMeshAgent>();var h=a.GetComponent<Health>();Check(a.Target==hp&&a.Viewer==guns.AimCamera&&a.Noise==source.Noise&&a.Navigation==nav&&a.Definition&&a.Definition.IsValid&&h&&h.MaxHealth==a.Definition.Health&&agent&&!agent.enabled&&a.GetComponent<EnemyFaction>()&&a.GetComponent<EnemyAttack>()&&a.GetComponent<EnemySpriteView>().IsValid&&a.GetComponent<EntityId>()&&a.GetComponentInChildren<SectorLighting>(),"Invalid actor: "+a.name);}
            foreach(var p in items)Check(p.Definition&&p.Definition.IsValid&&p.Visual&&p.Visual.GetComponent<SectorLighting>()&&p.GetComponent<EntityId>()&&p.GetComponent<SphereCollider>().isTrigger&&p.GetComponent<Rigidbody>().isKinematic,"Invalid pickup: "+p.name);
            Check(items.Count(p=>p.Definition.Kind==PickupKind.Key&&p.Definition.Key==KeyId.BlueCard)==1,"Expected exactly one blue card");
            var exit=Setup.Find<LevelExit>(s).Single();var objective=Setup.Find<MissionObjective>(s).Single();Check(exit.Stats==hud.Stats&&exit.Indicator&&objective.Health==hp&&objective.Inventory==bag&&objective.Stats==hud.Stats&&objective.RemoteDoor&&objective.RemoteDoor.SwitchOnly&&world.Switches.Any(x=>x.Door==objective.RemoteDoor)&&objective.ObjectiveLabel,"Objective or exit references invalid");
            Check(world.Doors.Length==4&&world.Switches.Length==2&&world.Secret&&world.Lift&&world.Zone&&world.Teleport&&world.Teleport.Destination&&world.Barrels.Length==2&&world.Interactor,"Missing world mechanisms");
            var map=index==0?MVP.Require<LevelDefinition>(UkubolaLevelSetup.Data):MVP.Require<LevelDefinition>(Setup.LevelPath(index));var auto=Setup.Find<Automap>(s).Single();var noise=source.Noise.GetComponent<SectorNoise>();Check(auto.Level==map&&auto.Player==hp&&auto.Map&&!auto.Map.enabled&&noise&&noise.Level==map,"Map and sector noise references invalid");
            var geometry=s.GetRootGameObjects().Single(g=>g.name=="Level Geometry");foreach(string group in LevelBuilder.GroupNames){var r=geometry.transform.Find(group).GetComponent<MeshRenderer>();Check(r.sharedMaterial&&r.sharedMaterial.shader.name=="UKUBOLA/SectorUnlit"&&r.sharedMaterial.shader.isSupported,"Missing sector material");}
            if(index>0)
            {
                var l=CampaignLayout.For(index);l.Check(map);Near(source.Spawn.Position,map.Center(l.Spawn),"spawn");Near(hp.transform.position,map.Center(l.Spawn)+Vector3.up*.03f,"player");Near(exit.transform.position,map.Center(l.Exit),"exit");Near(world.Secret.transform.position,map.Center(l.Secret),"secret");Near(world.Switches.Single(x=>x.Door).transform.position,map.Center(l.Switch),"remote switch");Near(world.Teleport.transform.position,map.Center(l.Teleport),"teleport");Near(world.Teleport.Destination.position,map.Center(l.Destination),"destination");
                Near(world.Doors.Single(x=>x.name=="Normal Door").transform.position,new Vector3((l.A+.5f)*2,0,(l.NormalZ+1)*2),"normal door");Near(world.Doors.Single(x=>x.name=="Blue Door").transform.position,new Vector3((l.BlueX+1)*2,0,(l.Mid+.5f)*2),"blue door");Near(objective.RemoteDoor.transform.position,new Vector3((l.B+.5f)*2,0,(l.RemoteZ+1)*2),"remote door");
                Check(AssetDatabase.GetAssetPath(nav.Data).StartsWith(Setup.Generated+"/"+l.Id+"/"),"Shared NavMesh on new map");
                for(int i=0;i<l.Enemies.Length;i++)Near(source.Enemies[i].transform.position,map.Center(l.Enemies[i].Cell),source.Enemies[i].name);
                for(int i=0;i<l.Items.Length;i++)Near(source.Pickups[i].transform.position,map.Center(l.Items[i].Cell),source.Pickups[i].name);
                var planned=LevelBuilder.Plan(map);for(int i=0;i<3;i++){var group=geometry.transform.Find(LevelBuilder.GroupNames[i]);var boxes=group.GetComponents<BoxCollider>();Check(boxes.Length==planned[i].Count,"Geometry collider count mismatch");for(int j=0;j<boxes.Length;j++){Near(boxes[j].center,planned[i][j].Center,group.name);Near(boxes[j].size,planned[i][j].Size,group.name);}var mesh=group.GetComponent<MeshFilter>().sharedMesh;Check(mesh&&mesh.vertexCount==boxes.Length*24,"Mesh mismatch: "+group.name);}
            }
            UkubolaMvpValidator.CheckBaseRoutes(nav.Data,actors,source.Spawn.Position);
            var context=Setup.Find<LevelContext>(s).Single();Check(context.LevelId==Setup.LevelIds[index],"Wrong LevelContext ID");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
        }
    }
}
