using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Ukubola.Enemies;
using Ukubola.World;
using Setup=Ukubola.EditorTools.UkubolaMvpSetup;
using Validator=Ukubola.EditorTools.UkubolaMvpValidator;
using Object=UnityEngine.Object;

namespace Ukubola.EditorTools
{
    public static class UkubolaMvpNavigationRepair
    {
        [MenuItem("Tools/UKUBOLA/Phase 11/Repair MVP Navigation",false,116)]
        public static void Repair()
        {
            if(!Setup.CanRun())return;
            try
            {
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.OpenScene(Setup.Mission,OpenSceneMode.Single);
                var nav=Setup.Find<EnemyNavigation>(scene).Single();
                var actors=Setup.Find<EnemyBrain>(scene);
                var spawn=Setup.Find<PlayerSpawn>(scene).Single().Position;
                Validator.Check(actors.Length==MvpLayout.Enemies.Length&&nav.Data,"Incomplete MVP scene. Regenerate using README recovery.");
                try
                {
                    Validator.CheckBaseRoutes(nav.Data,actors,spawn);
                    Debug.Log("[UKUBOLA P11] Base NavMesh already valid; no asset changed. Run Setup MVP.");return;
                }
                catch(InvalidOperationException ex){Debug.Log("[UKUBOLA P11] Rebuilding mission-only NavMesh: "+ex.Message);}
                var agents=actors.Select(a=>a.GetComponent<NavMeshAgent>()).ToArray();
                Validator.Check(agents.All(a=>a&&!a.enabled)&&agents.Select(a=>a.agentTypeID).Distinct().Count()==1,"Invalid agent configuration.");
                var settings=NavMesh.GetSettingsByID(agents[0].agentTypeID);
                Validator.Check(settings.agentTypeID==agents[0].agentTypeID,"Missing agent settings.");
                settings.agentRadius=.35f;settings.agentHeight=1.7f;settings.agentClimb=.30f;settings.agentSlope=45f;
                settings.overrideVoxelSize=true;
                var geometry=scene.GetRootGameObjects().Single(g=>g.name=="Level Geometry");
                var sources=new List<NavMeshBuildSource>();var bounds=new Bounds();bool first=true;
                foreach(string group in new[]{"Floors","Walls"})
                {
                    var parent=geometry.transform.Find(group);
                    Validator.Check(parent,"Missing geometry group: "+group);
                    foreach(var box in parent.GetComponents<BoxCollider>())
                    {
                        if(!box.enabled||box.isTrigger)continue;
                        sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=group=="Floors"?0:1});
                        if(first){bounds=box.bounds;first=false;}else bounds.Encapsulate(box.bounds);
                    }
                }
                Validator.Check(!first&&sources.Count>2,"No navigation geometry.");bounds.Expand(1f);
                NavMeshData accepted=null;
                foreach(float voxel in new[]{.05f,.04f})
                {
                    settings.voxelSize=voxel;
                    var candidate=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
                    if(!candidate)continue;
                    try{Validator.CheckBaseRoutes(candidate,actors,spawn);accepted=candidate;break;}
                    catch(Exception ex){Object.DestroyImmediate(candidate);Debug.LogWarning("[UKUBOLA P11] Candidate voxel "+voxel+" rejected: "+ex.Message);}
                }
                Validator.Check(accepted,"No candidate passed all isolated base routes. Original scene and NavMesh unchanged.");
                string folder=Setup.Root+"/Generated/Phase11";
                if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(Setup.Root+"/Generated","Phase11");
                string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/OutpostNavMesh.asset");
                var original=nav.Data;
                bool saved=false;
                try
                {
                    AssetDatabase.CreateAsset(accepted,path);AssetDatabase.SaveAssets();
                    nav.Configure(Setup.Require<NavMeshData>(path));EditorUtility.SetDirty(nav);
                    Validator.CheckMission(scene);
                    Setup.Save(scene);saved=true;
                    Validator.CheckMission(EditorSceneManager.OpenScene(Setup.Mission,OpenSceneMode.Single));
                    Debug.Log("[UKUBOLA P11] NAVIGATION REPAIRED | 4 complete isolated routes | "+path+". Run Setup MVP.");
                }
                catch
                {
                    if(!saved)
                    {
                        nav.Configure(original);EditorUtility.SetDirty(nav);
                        if(AssetDatabase.LoadMainAssetAtPath(path))AssetDatabase.DeleteAsset(path);
                        else if(accepted&&!EditorUtility.IsPersistent(accepted))Object.DestroyImmediate(accepted);
                    }
                    throw;
                }
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P11] NAVIGATION REPAIR FAILED: "+ex);}
        }
    }
}
