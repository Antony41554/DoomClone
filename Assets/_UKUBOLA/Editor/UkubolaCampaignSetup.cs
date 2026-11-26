using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.UI;
using Ukubola.World;
using Object=UnityEngine.Object;
using MVP=Ukubola.EditorTools.UkubolaMvpSetup;
using Advanced=Ukubola.EditorTools.UkubolaAdvancedSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaCampaignSetup
    {
        internal const string Root=MVP.Root,Title=Root+"/Scenes/Menus/CampaignTitle.unity",Generated=Root+"/Generated/Phase13",Data=Root+"/Data/Phase13";
        internal static readonly string[] Missions={Root+"/Scenes/Levels/E1M1_Campaign.unity",Root+"/Scenes/Levels/E1M2_TheFoundry.unity",Root+"/Scenes/Levels/E1M3_TheCore.unity"};
        internal static readonly string[] Labels={"E1M1 / THE OUTPOST","E1M2 / THE FOUNDRY","E1M3 / THE CORE"};
        internal static readonly string[] LevelIds={"E1M1_CAMPAIGN","E1M2_FOUNDRY","E1M3_CORE"};
        internal static string[] Paths=>new[]{Title}.Concat(Missions).ToArray();
        internal static T[] Find<T>(Scene s)where T:Component=>MVP.Find<T>(s);
        internal static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        private static void Dirty(Object o){EditorUtility.SetDirty(o);if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
        private static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;var parent=p.Substring(0,p.LastIndexOf('/'));Folder(parent);Check(!string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,p.Substring(p.LastIndexOf('/')+1))),"Cannot create folder "+p);}
        private static Scene Open(string p)=>EditorSceneManager.OpenScene(p,OpenSceneMode.Single);
        private static void Copy(string from,string to){Check(AssetDatabase.CopyAsset(from,to),"Cannot copy "+to);AssetDatabase.ImportAsset(to,ImportAssetOptions.ForceSynchronousImport);}
        internal static string LevelPath(int index)=>Data+"/"+LevelIds[index]+".asset";
        private static void SetContext(Scene s,int index){var c=Find<LevelContext>(s).Single();var so=new SerializedObject(c);so.FindProperty("levelId").stringValue=LevelIds[index];so.ApplyModifiedPropertiesWithoutUndo();}
        private static void Route(Scene s,int index)
        {
            var flow=Find<MenuFlow>(s).Single();flow.ConfigureRoute(Title,index<0?Missions[0]:Missions[index],index<0?"EPISODE 1 / THREE MISSIONS":Labels[index]);Dirty(flow);
            flow.gameObject.AddComponent<CampaignStage>().Configure(index,index<0?Title:Missions[index],index>=0&&index<Missions.Length-1?Missions[index+1]:null,index<0?"EPISODE 1":Labels[index]);
            if(index>=0)SetContext(s,index);
        }
        private static void Move(Transform t,Vector3 p){t.position=p;Dirty(t);}
        private static void PositionDoor(Door door,Vector3 p,Vector3 size)
        {
            door.transform.SetPositionAndRotation(p,Quaternion.identity);door.Panel.localPosition=Vector3.up*size.y*.5f;door.Panel.localScale=size;
            var o=door.GetComponent<NavMeshObstacle>();o.size=size;o.center=Vector3.up*size.y*.5f;Dirty(o);Dirty(door.transform);Dirty(door.Panel);
        }
        private static void RepositionWorld(Scene s,CampaignLayout l,LevelDefinition map,WorldDiagnostics world)
        {
            Vector3 At(Vector2Int p)=>map.Center(p);
            var source=world.Source;var yaw=l.Width==21?Quaternion.identity:Quaternion.Euler(0,180,0);
            source.Spawn.transform.SetPositionAndRotation(At(l.Spawn),yaw);source.Player.transform.SetPositionAndRotation(At(l.Spawn)+Vector3.up*.03f,yaw);Dirty(source.Spawn.transform);Dirty(source.Player.transform);
            Move(Find<LevelExit>(s).Single().transform,At(l.Exit));
            // Door panels cross the full two-cell opening, including a small wall overlap.
            PositionDoor(world.Doors.Single(x=>x.name=="Normal Door"),new Vector3((l.A+.5f)*2,0,(l.NormalZ+1)*2),new Vector3(.24f,3.2f,4.04f));
            PositionDoor(world.Doors.Single(x=>x.name=="Blue Door"),new Vector3((l.BlueX+1)*2,0,(l.Mid+.5f)*2),new Vector3(4.04f,3.2f,.24f));
            PositionDoor(world.Doors.Single(x=>x.SwitchOnly),new Vector3((l.B+.5f)*2,0,(l.RemoteZ+1)*2),new Vector3(.24f,3.2f,4.04f));
            PositionDoor(world.Doors.Single(x=>x.name=="Secret Door"),new Vector3(l.Secret.x*2,0,(l.Secret.y+.5f)*2),new Vector3(.24f,3.2f,2.04f));
            Move(Find<Transform>(s).Single(x=>x.name=="Secret South Wall"),new Vector3((l.Secret.x+.5f)*2,0,(l.Secret.y+1)*2));
            Move(world.Secret.transform,At(l.Secret));Move(world.Lift.transform,At(l.Lift));Move(world.Switches.Single(x=>x.Lift).transform,At(l.LiftSwitch));Move(world.Switches.Single(x=>x.Door).transform,At(l.Switch));
            Move(world.Zone.transform,At(l.Hazard));Move(world.Teleport.transform,At(l.Teleport));Move(world.Teleport.Destination,At(l.Destination));
            Move(world.Barrels[0].transform,At(l.BarrelA)+Vector3.up*.825f);Move(world.Barrels[1].transform,At(l.BarrelB)+Vector3.up*.825f);
            Find<MissionObjective>(s).Single().ConfigureInstructions(l.Width==21?"1/3 BLUE CARD - CENTRAL NORTH":"1/3 BLUE CARD - CENTRAL SOUTH",l.Width==21?"2/3 ORANGE SWITCH - CENTRAL SOUTH":"2/3 ORANGE SWITCH - CENTRAL NORTH","3/3 GREEN EXIT - EAST SECTOR");
        }
        private static EnemyBrain Actor(Scene s,MvpLayout.Placement p,int i,LevelDefinition level,PickupDiagnostics source,EnemyNavigation nav)
        {
            bool newer=p.Prefab=="Brute"||p.Prefab=="Watcher";var prefab=MVP.Require<GameObject>((newer?Advanced.Prefabs:UkubolaEnemySetup.Prefabs)+"/"+p.Prefab+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,s);go.name=p.Name+" - "+(i+1);go.transform.SetPositionAndRotation(level.Center(p.Cell),Quaternion.Euler(0,p.Yaw,0));var a=go.GetComponent<EnemyBrain>();a.Bind(source.Player,source.Weapons.AimCamera,source.Noise,nav);
            if(!go.GetComponent<EnemyFaction>())go.AddComponent<EnemyFaction>().Configure(p.Prefab=="Guard"?1:2);
            go.AddComponent<EntityId>();a.GetComponentInChildren<SpriteRenderer>().gameObject.AddComponent<SectorLighting>();Dirty(a);Dirty(go.transform);return a;
        }
        private static Pickup Item(Scene s,MvpLayout.Placement p,int i,LevelDefinition level,PickupDiagnostics source,Transform parent)
        {
            bool advanced=Advanced.Items.Take(7).Contains(p.Prefab);var prefab=MVP.Require<GameObject>((advanced?Advanced.Prefabs:UkubolaPickupSetup.Prefabs)+"/"+p.Prefab+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,s);go.name=p.Name+" - "+(i+1);go.transform.SetParent(parent,false);go.transform.position=level.Center(p.Cell);
            var item=go.GetComponent<Pickup>();item.BindViewer(source.Weapons.AimCamera);go.AddComponent<EntityId>();item.Visual.gameObject.AddComponent<SectorLighting>();Dirty(go.transform);Dirty(item.Visual.GetComponent<Billboard>());return item;
        }
        private static void Bake(Scene s,EnemyNavigation nav,EnemyBrain[] actors,Vector3 spawn,string folder)
        {
            var settings=NavMesh.GetSettingsByID(actors[0].GetComponent<NavMeshAgent>().agentTypeID);settings.agentRadius=.35f;settings.agentHeight=1.7f;settings.agentClimb=.30f;settings.agentSlope=45;settings.overrideVoxelSize=true;
            var sources=new List<NavMeshBuildSource>();var bounds=new Bounds();bool first=true;var geometry=s.GetRootGameObjects().Single(x=>x.name=="Level Geometry");
            foreach(string group in new[]{"Floors","Walls"})foreach(var box in geometry.transform.Find(group).GetComponents<BoxCollider>()){sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=group=="Floors"?0:1});if(first){bounds=box.bounds;first=false;}else bounds.Encapsulate(box.bounds);}
            Check(!first,"Missing geometry");bounds.Expand(1);
            foreach(float voxel in new[]{.05f,.04f})
            {
                settings.voxelSize=voxel;var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!data)continue;
                try{UkubolaMvpValidator.CheckBaseRoutes(data,actors,spawn);}catch(Exception e){Object.DestroyImmediate(data);Debug.LogWarning("[UKUBOLA P13] NavMesh candidate rejected: "+e.Message);continue;}
                string path=folder+"/Navigation.asset";AssetDatabase.CreateAsset(data,path);AssetDatabase.SaveAssets();nav.Configure(MVP.Require<NavMeshData>(path));Dirty(nav);return;
            }
            throw new InvalidOperationException("No isolated base navigation candidate connected all actors in "+s.path);
        }
        private static void Rebuild(Scene s,int index)
        {
            var l=CampaignLayout.For(index);var map=MVP.Require<LevelDefinition>(LevelPath(index));l.Check(map);var flow=Find<MenuFlow>(s).Single();var world=flow.Hud.World;var source=world.Source;var nav=Find<EnemyNavigation>(s).Single();var geometry=s.GetRootGameObjects().Single(x=>x.name=="Level Geometry");
            Object.DestroyImmediate(geometry);geometry=new GameObject("Level Geometry");geometry.layer=LayerMask.NameToLayer("World");string folder=Generated+"/"+l.Id;Folder(folder);
            LevelBuilder.Build(geometry.transform,map,new[]{"Floor","Ceiling","Wall"}.Select(n=>MVP.Require<Material>(Advanced.Generated+"/"+n+".mat")).ToArray(),folder,geometry.layer);AssetDatabase.SaveAssets();
            // Preserve the parent and all HUD/audio references, replace only enemy and pickup instances.
            flow=Find<MenuFlow>(s).Single();world=flow.Hud.World;source=world.Source;nav=Find<EnemyNavigation>(s).Single();map=MVP.Require<LevelDefinition>(LevelPath(index));var parent=source.Pickups[0].transform.parent;
            foreach(var a in source.Enemies)Object.DestroyImmediate(a.gameObject);foreach(var p in source.Pickups)Object.DestroyImmediate(p.gameObject);
            var actors=l.Enemies.Select((p,i)=>Actor(s,p,i,map,source,nav)).ToArray();var items=l.Items.Select((p,i)=>Item(s,p,i,map,source,parent)).ToArray();
            source.Configure(items,actors,source.Player,source.Weapons,source.Noise,source.Spawn,source.Readout);Dirty(source);RepositionWorld(s,l,map,world);
            var automap=Find<Automap>(s).Single();var so=new SerializedObject(automap);so.FindProperty("level").objectReferenceValue=map;so.ApplyModifiedPropertiesWithoutUndo();
            source.Noise.GetComponent<SectorNoise>().Configure(map,LayerMask.GetMask("World","Interactable"));Dirty(source.Noise.GetComponent<SectorNoise>());Physics.SyncTransforms();Bake(s,nav,actors,source.Spawn.Position,folder);
        }
        private static void Register(){var all=Paths;EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(x=>!all.Contains(x.path)).Concat(all.Select(p=>new EditorBuildSettingsScene(p,true))).ToArray();}
        internal static void ValidateAll(){for(int i=0;i<Missions.Length;i++)UkubolaCampaignValidator.CheckMission(Open(Missions[i]),i);UkubolaCampaignValidator.CheckTitle(Open(Title));}
        [MenuItem("Tools/UKUBOLA/Phase 13/Setup Campaign",false,130)]public static void Setup()
        {
            if(!MVP.CanRun()||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try
            {
                if(Paths.Any(MVP.Exists)){Check(Paths.All(MVP.Exists),"Partial campaign. See README recovery.");ValidateAll();Register();UkubolaCampaignValidator.CheckRegistered();Debug.Log("[UKUBOLA P13] Existing campaign preserved. VALIDATION PASSED | 0 errors.");return;}
                Advanced.ValidateInternal();Check(!AssetDatabase.IsValidFolder(Generated)&&!AssetDatabase.IsValidFolder(Data),"Phase13 assets already exist; see README recovery.");Folder(Generated);Folder(Data);
                for(int i=1;i<3;i++){var l=CampaignLayout.For(i);var map=ScriptableObject.CreateInstance<LevelDefinition>();map.SetLayout(l.Id,l.Rows());l.Check(map);AssetDatabase.CreateAsset(map,LevelPath(i));}AssetDatabase.SaveAssets();
                foreach(var path in Missions)Copy(Advanced.Mission,path);Copy(Advanced.Title,Title);
                for(int i=0;i<3;i++){var s=Open(Missions[i]);if(i>0)Rebuild(s,i);Route(s,i);MVP.Save(s);}
                var title=Open(Title);Route(title,-1);MVP.Save(title);ValidateAll();Register();UkubolaCampaignValidator.CheckRegistered();Debug.Log("[UKUBOLA P13] Campaign generated. Previous scenes preserved. VALIDATION PASSED | 0 errors.");
            }
            catch(Exception e){Debug.LogError("[UKUBOLA P13] Setup stopped: "+e+"\nSee README recovery.");}
        }
        private static void OpenMenu(string path){if(MVP.CanRun()&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())try{MVP.Require<SceneAsset>(path);Open(path);}catch(Exception e){Debug.LogError(e.Message);}}
        [MenuItem("Tools/UKUBOLA/Phase 13/Open Campaign Title",false,131)]public static void OpenTitle()=>OpenMenu(Title);
        [MenuItem("Tools/UKUBOLA/Phase 13/Open Mission 1",false,132)]public static void OpenOne()=>OpenMenu(Missions[0]);
        [MenuItem("Tools/UKUBOLA/Phase 13/Open Mission 2",false,133)]public static void OpenTwo()=>OpenMenu(Missions[1]);
        [MenuItem("Tools/UKUBOLA/Phase 13/Open Mission 3",false,134)]public static void OpenThree()=>OpenMenu(Missions[2]);
        [MenuItem("Tools/UKUBOLA/Phase 13/Validate Campaign",false,135)]public static void Validate(){if(!MVP.CanRun()||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;try{ValidateAll();UkubolaCampaignValidator.CheckRegistered();Debug.Log("[UKUBOLA P13] VALIDATION PASSED | 0 errors.");}catch(Exception e){Debug.LogError("[UKUBOLA P13] VALIDATION FAILED: "+e);}}
        [MenuItem("Tools/UKUBOLA/Phase 13/Build Windows Campaign",false,137)]public static void Build(){if(!MVP.CanRun()||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;try{Check(EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneWindows64&&BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64),"Select Windows x64 and install Build Support");ValidateAll();string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Builds","UKUBOLA_CAMPAIGN_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));Directory.CreateDirectory(folder);var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=Paths,target=BuildTarget.StandaloneWindows64,locationPathName=Path.Combine(folder,"UKUBOLA.exe"),options=BuildOptions.None});Check(result.summary.result==BuildResult.Succeeded,"Build failed");Debug.Log("[UKUBOLA P13] WINDOWS BUILD SUCCEEDED | "+folder);EditorUtility.RevealInFinder(folder);}catch(Exception e){Debug.LogError("[UKUBOLA P13] BUILD FAILED: "+e);}}
    }
}
