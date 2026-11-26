using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Persistence;
using Ukubola.Player;
using Ukubola.UI;
using Ukubola.World;
using MVP=Ukubola.EditorTools.UkubolaMvpSetup;
using Previous=Ukubola.EditorTools.UkubolaCampaignSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaPersistenceSetup
    {
        internal static string[] Paths=>new[]{SaveCampaignPaths.Title}.Concat(SaveCampaignPaths.Missions).ToArray();
        internal static T[] Find<T>(Scene s)where T:Component=>MVP.Find<T>(s);
        internal static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
        private static Scene Open(string path)=>EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        private static bool CanRun()=>MVP.CanRun()&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        private static void Register()
        {
            var paths=Paths;EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(x=>!paths.Contains(x.path)).Concat(paths.Select(p=>new EditorBuildSettingsScene(p,true))).ToArray();
        }
        private static void CheckRegistered(){foreach(var p in Paths)Check(EditorBuildSettings.scenes.Count(x=>x.path==p&&x.enabled)==1,"Missing scene in build list: "+p);}
        private static void Configure(Scene s,int index)
        {
            string path=index<0?SaveCampaignPaths.Title:SaveCampaignPaths.Missions[index];var f=Find<MenuFlow>(s).Single();var c=Find<CampaignStage>(s).Single();
            f.ConfigureRoute(SaveCampaignPaths.Title,index<0?SaveCampaignPaths.Missions[0]:path,index<0?"EPISODE 1 / SAVE CAMPAIGN":Previous.Labels[index]);f.EnablePersistence();
            c.Configure(index,path,index>=0&&index<2?SaveCampaignPaths.Missions[index+1]:null,index<0?"EPISODE 1":Previous.Labels[index]);
            EditorUtility.SetDirty(f);EditorUtility.SetDirty(c);MVP.Save(s);
        }
        private static string CheckScene(Scene s,int index)
        {
            bool title=index<0;string path=title?SaveCampaignPaths.Title:SaveCampaignPaths.Missions[index];
            Check(s.isLoaded&&s.path==path,"Wrong phase14 scene");var f=Find<MenuFlow>(s).Single();var c=Find<CampaignStage>(s).Single();
            Check(f.PersistenceEnabled&&f.TitleScene==title&&f.Typeface&&f.RouteTitle==SaveCampaignPaths.Title&&f.RouteGame==(title?SaveCampaignPaths.Missions[0]:path),"Invalid persistent menu route");
            Check(c.gameObject==f.gameObject&&c.Index==index&&c.CurrentPath==path&&(c.NextPath??"")==(!title&&index<2?SaveCampaignPaths.Missions[index+1]:""),"Invalid persistent campaign route");
            Check(Find<Camera>(s).Count(x=>x.isActiveAndEnabled)==(title?1:2)&&Find<AudioListener>(s).Count(x=>x.isActiveAndEnabled)==1,"Wrong camera or listener count");
            foreach(var t in Find<Transform>(s))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
            if(title){Check(!f.Hud&&f.TitleMusic&&f.TitleMusic.clip,"Missing title references");return null;}
            var h=f.Hud;Check(h&&h.World&&h.World.Source&&h.Audio&&h.Stats&&!h.DiagnosticsEnabled&&!h.Audio.DiagnosticsEnabled,"Invalid HUD");var w=h.World;var source=w.Source;
            Check(source.Player&&source.Player.Armor&&source.Player.GetComponent<PlayerInventory>()&&source.Player.GetComponent<Powerup>()&&source.Player.GetComponent<CharacterController>()&&source.Weapons&&source.Spawn,"Missing player state references");
            Check(!w.enabled&&!source.enabled&&w.Doors.Length==4&&w.Switches.Length==2&&w.Lift&&w.Secret&&w.Zone&&w.Teleport&&w.Barrels.Length==2,"Missing world references");
            Check(source.Enemies.Length==6+2*index&&source.Pickups.Length==(index==0?25:24)&&Find<EnemyBrain>(s).Length==source.Enemies.Length&&Find<Pickup>(s).Length==source.Pickups.Length,"Wrong snapshot object counts");
            void Unique<T>(T[] a)where T:Component{Check(a.All(x=>x)&&a.Select(x=>x.name).Distinct(StringComparer.Ordinal).Count()==a.Length,"Duplicate or missing snapshot name in "+typeof(T).Name);}
            Unique(source.Enemies);Unique(source.Pickups);Unique(w.Doors);Unique(w.Switches);Unique(w.Barrels);
            foreach(var e in source.Enemies)Check(e.Definition&&e.GetComponent<Health>()&&e.GetComponent<NavMeshAgent>()&&e.GetComponent<EnemyMotor>(),"Missing enemy state: "+e.name);
            foreach(var p in source.Pickups)Check(p.Definition&&p.Visual&&p.GetComponent<SphereCollider>(),"Missing pickup state: "+p.name);
            foreach(var d in w.Doors)Check(d.Panel&&d.GetComponent<NavMeshObstacle>(),"Missing door state");
            foreach(var b in w.Barrels)Check(b.GetComponent<Health>()&&b.GetComponent<BoxCollider>(),"Missing barrel state");
            var map=SaveWorld.Map(h);Check(map.Level&&map.Player==source.Player,"Missing automap state");var nav=Find<EnemyNavigation>(s).Single();Check(nav.Data,"Missing NavMesh");UkubolaMvpValidator.CheckBaseRoutes(nav.Data,source.Enemies,source.Spawn.Position);
            return SaveWorld.Signature(h);
        }
        internal static void ValidateAll()
        {
            for(int i=0;i<3;i++)
            {
                var original=Open(Previous.Missions[i]);UkubolaCampaignValidator.CheckMission(original,i);
                var expected=SaveWorld.Signature(Find<MenuFlow>(original).Single().Hud);
                Check(CheckScene(Open(SaveCampaignPaths.Missions[i]),i)==expected,"Snapshot layout differs from phase13 mission "+(i+1));
            }
            CheckScene(Open(SaveCampaignPaths.Title),-1);
        }
        [MenuItem("Tools/UKUBOLA/Phase 14/Setup Persistence",false,140)]public static void Setup()
        {
            if(!CanRun())return;
            try
            {
                if(Paths.Any(MVP.Exists))Check(Paths.All(MVP.Exists),"Partial phase14 scenes. See README recovery.");
                else
                {
                    Previous.ValidateAll();var from=Previous.Paths;var to=Paths;
                    for(int i=0;i<to.Length;i++){Check(AssetDatabase.CopyAsset(from[i],to[i]),"Cannot copy "+to[i]);AssetDatabase.ImportAsset(to[i],ImportAssetOptions.ForceSynchronousImport);}
                    for(int i=0;i<to.Length;i++)Configure(Open(to[i]),i-1);
                }
                ValidateAll();Register();CheckRegistered();Debug.Log("[UKUBOLA P14] Persistence ready. Phase13 preserved. VALIDATION PASSED | 0 errors.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P14] Setup stopped: "+ex+"\nSee README recovery.");}
        }
        [MenuItem("Tools/UKUBOLA/Phase 14/Open Save Campaign Title",false,141)]public static void OpenTitle(){if(CanRun())Open(SaveCampaignPaths.Title);}
        [MenuItem("Tools/UKUBOLA/Phase 14/Validate Persistence",false,142)]public static void Validate(){if(!CanRun())return;try{ValidateAll();CheckRegistered();Debug.Log("[UKUBOLA P14] VALIDATION PASSED | 0 errors.");}catch(Exception ex){Debug.LogError("[UKUBOLA P14] VALIDATION FAILED: "+ex);}}
        [MenuItem("Tools/UKUBOLA/Phase 14/Open Saves Folder",false,145)]public static void OpenFolder(){try{Directory.CreateDirectory(SaveStore.Folder);EditorUtility.RevealInFinder(SaveStore.Folder);Debug.Log("[UKUBOLA P14] "+SaveStore.Folder);}catch(Exception ex){Debug.LogError(ex.Message);}}
        [MenuItem("Tools/UKUBOLA/Phase 14/Build Windows Saved Campaign",false,146)]public static void Build()
        {
            if(!CanRun())return;
            try
            {
                Check(EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneWindows64&&BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64),"Select Windows x64 and install Build Support");ValidateAll();CheckRegistered();
                string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Builds","UKUBOLA_PERSISTENCE_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));Directory.CreateDirectory(folder);
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=Paths,target=BuildTarget.StandaloneWindows64,locationPathName=Path.Combine(folder,"UKUBOLA.exe"),options=BuildOptions.None});
                Check(result.summary.result==BuildResult.Succeeded,"Build failed");Debug.Log("[UKUBOLA P14] WINDOWS BUILD SUCCEEDED | "+folder);EditorUtility.RevealInFinder(folder);
            }catch(Exception ex){Debug.LogError("[UKUBOLA P14] BUILD FAILED: "+ex);}
        }
    }
}
