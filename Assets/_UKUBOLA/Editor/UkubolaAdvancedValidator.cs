using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Enemies;
using Ukubola.World;
using Ukubola.Rendering;
using Ukubola.UI;
using Setup=Ukubola.EditorTools.UkubolaAdvancedSetup;
namespace Ukubola.EditorTools {
 public static class UkubolaAdvancedValidator {
  private static void Check(bool ok,string text)=>Setup.Check(ok,text);
  private static MenuFlow Common(Scene s,string path,bool title){Check(s.isLoaded&&s.path==path,"Wrong scene "+path);var f=Setup.Find<MenuFlow>(s).Single();Check(f&&f.isActiveAndEnabled&&f.TitleScene==title&&f.RouteTitle==Setup.Title&&f.RouteGame==Setup.Mission&&f.MissionLabel==Setup.Label&&f.Typeface,"Incorrect flow");Check(Setup.Find<Camera>(s).Count(c=>c.isActiveAndEnabled)==(title?1:2)&&Setup.Find<AudioListener>(s).Count(a=>a.isActiveAndEnabled)==1,"Incorrect cameras/listener");Check(Setup.Find<EventSystem>(s).Length==0,"Unexpected EventSystem");foreach(var t in Setup.Find<Transform>(s))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);return f;}
  internal static void CheckTitle(Scene s){var f=Common(s,Setup.Title,true);Check(!f.Hud&&f.TitleMusic&&f.TitleMusic.clip&&f.TitleMusic.loop&&!f.TitleMusic.playOnAwake&&Setup.Find<PlayerMotor>(s).Length==0,"Invalid title");}
  internal static void CheckMission(Scene s){var f=Common(s,Setup.Mission,false);var hud=f.Hud;Check(hud&&hud.World&&hud.Audio&&hud.Stats&&!hud.DiagnosticsEnabled&&!hud.Audio.DiagnosticsEnabled,"Missing HUD/audio/stats or enabled diagnostics");var w=hud.World;var source=w.Source;Check(source&&source.Player&&source.Noise&&source.Spawn&&source.Weapons&&source.Player.GetComponent<Powerup>()&&!source.enabled&&!w.enabled,"Missing player or advanced systems");var guns=source.Weapons;var display=Setup.Find<RetroDisplay>(s).Single();Check(display.WorldCamera==guns.AimCamera&&display.Target&&display.Target.width==320&&display.Target.height==200&&display.Image&&display.WorldCamera.targetTexture==display.Target,"Retro display invalid");
   UkubolaCombatValidator.CheckDefinition(guns.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(guns.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
   Check(guns.Advanced!=null&&guns.Advanced.Length==6&&guns.AdvancedProjectile&&guns.Advanced.All(x=>x&&x.IsValid&&EditorUtility.IsPersistent(x)&&x.Idle.filterMode==FilterMode.Point&&x.Firing.filterMode==FilterMode.Point)&&guns.Advanced.Select(x=>x.Weapon).Distinct().Count()==6,"Advanced weapon definitions incomplete");
   foreach(var id in Setup.Ids){var d=guns.Advanced.Single(x=>x.Weapon==id);Check(AssetDatabase.GetAssetPath(d)==Setup.Data+"/"+id+".asset","Wrong weapon asset "+id);Check(hud.Audio.AdvancedClips!=null&&hud.Audio.AdvancedClips.Length==8&&hud.Audio.AdvancedClips[(int)id],"Missing weapon audio "+id);}
   var nav=Setup.Find<EnemyNavigation>(s).Single();var actors=Setup.Find<EnemyBrain>(s);var items=Setup.Find<Pickup>(s);Check(actors.Length==6&&source.Enemies.Length==6&&actors.All(a=>source.Enemies.Contains(a)),"Expected six enemies");Check(items.Length==25&&source.Pickups.Length==25&&items.All(p=>source.Pickups.Contains(p)),"Expected 25 pickups");
   foreach(var a in actors){var hp=a.GetComponent<Health>();var agent=a.GetComponent<NavMeshAgent>();Check(a.Target==source.Player&&a.Viewer==display.WorldCamera&&a.Noise==source.Noise&&a.Navigation==nav&&a.Definition&&a.Definition.IsValid&&hp&&hp.MaxHealth==a.Definition.Health&&agent&&!agent.enabled&&a.GetComponent<CapsuleCollider>()&&a.GetComponent<EnemyAttack>()&&a.GetComponent<EnemySpriteView>().IsValid&&a.GetComponent<EnemyFaction>()&&a.GetComponent<EntityId>()&&a.GetComponentInChildren<SectorLighting>(),"Enemy links invalid: "+a.name);}
   Check(actors.Count(a=>a.GetComponent<FloatingEnemyMotor>())==1&&actors.Count(a=>a.Definition.AttackKind==EnemyAttackKind.Melee)==1,"Expected a floating Watcher and melee Brute");
   var level=Setup.Require<LevelDefinition>(UkubolaLevelSetup.Data);for(int i=0;i<Setup.EnemyCells.Length;i++){var a=actors.Single(x=>x.name==(i==0?"Brute":"Watcher"));Check(Vector3.Distance(a.transform.position,level.Center(Setup.EnemyCells[i]))<.02f,"Enemy position changed: "+a.name);}
   foreach(var p in items)Check(p.Definition&&p.Definition.IsValid&&p.Visual&&p.GetComponent<EntityId>()&&p.GetComponent<SphereCollider>().isTrigger&&p.GetComponent<Rigidbody>().isKinematic&&p.Visual.GetComponent<SectorLighting>(),"Pickup invalid: "+p.name);
   for(int i=0;i<Setup.Items.Length;i++){var p=items.Single(x=>x.name=="Advanced "+Setup.Items[i]);Check(Vector3.Distance(p.transform.position,level.Center(Setup.Cells[i]))<.02f,"Wrong pickup position "+p.name);}
   Check(items.Count(x=>x.Definition.Kind==PickupKind.Powerup)==2&&items.Count(x=>x.Definition.Kind==PickupKind.Key&&x.Definition.Key==KeyId.BlueCard)==1,"Powerups or objective key missing");
   var objective=Setup.Find<MissionObjective>(s).Single();var exit=Setup.Find<LevelExit>(s).Single();Check(objective&&objective.Health==source.Player&&objective.Inventory==source.Player.GetComponent<PlayerInventory>()&&objective.Stats==hud.Stats&&exit.Stats==hud.Stats&&objective.GetComponent<LevelExit>()==exit&&objective.RemoteDoor&&objective.RemoteDoor.SwitchOnly&&w.Switches.Any(x=>x.Door==objective.RemoteDoor)&&objective.ObjectiveLabel,"Objective broken");
   Check(w.Doors.Length==4&&w.Switches.Length==2&&w.Lift&&w.Zone&&w.Teleport&&w.Secret&&w.Barrels.Length==2,"Previous mechanisms missing");var map=Setup.Find<Automap>(s).Single();Check(map.Level==level&&map.Player==source.Player&&map.Map&&map.Map.transform.IsChildOf(display.Image.transform)&&!map.Map.enabled&&source.Noise.GetComponent<SectorNoise>()&&source.Noise.GetComponent<SectorNoise>().Level==level,"Automap or sector noise invalid");
   var geometry=s.GetRootGameObjects().Single(g=>g.name=="Level Geometry");foreach(string group in new[]{"Floors","Ceilings","Walls"}){var mat=geometry.transform.Find(group).GetComponent<MeshRenderer>().sharedMaterial;Check(mat&&mat.shader.name=="UKUBOLA/SectorUnlit"&&mat.shader.isSupported&&AssetDatabase.GetAssetPath(mat).StartsWith(Setup.Generated+"/"),"Sector material invalid: "+group);}
   Check(nav.Data&&EditorUtility.IsPersistent(nav.Data),"Missing base NavMesh");UkubolaMvpValidator.CheckBaseRoutes(nav.Data,actors,source.Spawn.Position);
   var context=Setup.Find<LevelContext>(s).Single();Check(context.LevelId=="E1M1_ADVANCED","Invalid level ID");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
  }
 }
}
