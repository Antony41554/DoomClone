using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;

namespace Ukubola.EditorTools
{
    public static class UkubolaLevelSetup
    {
        internal const string Root="Assets/_UKUBOLA";
        internal const string Source=Root+"/Scenes/Tests/RetroLab.unity";
        internal const string Lab=Root+"/Scenes/Tests/LevelLab.unity";
        internal const string Data=Root+"/Data/Levels/LevelLab.asset";
        internal const string Generated=Root+"/Generated/Phase03";
        internal static bool CanRun() => UkubolaRetroSetup.CanRun();
        internal static T Require<T>(string path) where T:UnityEngine.Object => UkubolaRetroSetup.Require<T>(path);
        internal static T[] Find<T>(Scene scene) where T:Component => UkubolaRetroSetup.Find<T>(scene);
        internal static bool Exists(string path)=>File.Exists(path)||AssetDatabase.LoadMainAssetAtPath(path)!=null;
        private static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            int split=path.LastIndexOf('/');
            if(!AssetDatabase.IsValidFolder(path.Substring(0,split)))throw new IOException("Missing parent folder for "+path);
            if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1))))throw new IOException("Cannot create "+path);
        }
        internal static Scene OpenInternal()
        {
            Require<SceneAsset>(Lab);
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=Lab||SceneManager.sceneCount!=1)scene=EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);
            if(!scene.IsValid()||!scene.isLoaded||SceneManager.GetActiveScene()!=scene)throw new InvalidOperationException("LevelLab is not active and loaded.");
            return scene;
        }
        [MenuItem("Tools/UKUBOLA/Phase 03/Setup Level Lab",false,40)]
        public static void Setup()
        {
            if(!CanRun())return;
            try
            {
                Require<SceneAsset>(Source);
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                if(Exists(Lab))
                {
                    UkubolaLevelValidator.CheckScene(OpenInternal());
                    Debug.Log("[UKUBOLA P03] Existing LevelLab preserved. VALIDATION PASSED | 0 errors.");return;
                }
                var source=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
                UkubolaRetroValidator.CheckScene(source);
                if(Find<PlayerMotor>(source).Single().GetComponent<CharacterController>().stepOffset<LevelDefinition.MaxStep)
                    throw new InvalidOperationException("Player stepOffset must be at least 0.25m.");
                // Confirm the exact source roots before removing anything in the copy.
                foreach(string name in RemovedRoots)if(source.GetRootGameObjects().Count(r=>r.name==name)!=1)throw new InvalidOperationException("Expected source root: "+name);
                int world=LayerMask.NameToLayer("World"),playerLayer=LayerMask.NameToLayer("Player"),interact=LayerMask.NameToLayer("Interactable");
                if(world<0||playerLayer<0||interact<0||Physics.GetIgnoreLayerCollision(world,playerLayer)||Physics.GetIgnoreLayerCollision(interact,playerLayer))
                    throw new InvalidOperationException("World/Player and Interactable/Player layers must collide.");
                Folder(Root+"/Data/Levels");Folder(Generated);
                foreach(string name in LevelBuilder.GroupNames)if(Exists(Generated+"/"+name+".asset"))throw new InvalidOperationException("Orphan Phase03 mesh; see recovery in README.");
                LevelDefinition definition;
                if(Exists(Data))definition=Require<LevelDefinition>(Data);
                else {definition=ScriptableObject.CreateInstance<LevelDefinition>();AssetDatabase.CreateAsset(definition,Data);}
                if(!definition.TryValidate(out string reason,out var spawnCell,out var exitCell))throw new InvalidOperationException(reason);
                var materials=new[]{Surface("Floor","Floor",Color.white),Surface("Ceiling","Floor",new Color(.55f,.55f,.55f)),Surface("Wall","Wall",Color.white)};
                var exitMat=Surface("Exit",null,new Color(.15f,.95f,.35f));
                if(!AssetDatabase.CopyAsset(Source,Lab))throw new IOException("Cannot copy RetroLab.");
                AssetDatabase.ImportAsset(Lab,ImportAssetOptions.ForceSynchronousImport);
                var scene=OpenInternal();
                foreach(string name in RemovedRoots)UnityEngine.Object.DestroyImmediate(scene.GetRootGameObjects().Single(r=>r.name==name));
                var player=Find<PlayerMotor>(scene).Single();
                var context=Find<LevelContext>(scene).Single();
                var serialized=new SerializedObject(context);var id=serialized.FindProperty("levelId");
                if(id==null)throw new InvalidOperationException("LevelContext.levelId not found.");
                id.stringValue=definition.LevelId;serialized.ApplyModifiedPropertiesWithoutUndo();
                var stats=context.gameObject.AddComponent<LevelStats>();
                var root=new GameObject("Level Geometry");root.layer=world;
                LevelBuilder.Build(root.transform,definition,materials,Generated,world);
                var spawn=new GameObject("Player Spawn").AddComponent<PlayerSpawn>();
                spawn.transform.SetPositionAndRotation(definition.Center(spawnCell),Quaternion.identity);
                player.transform.SetPositionAndRotation(spawn.Position+Vector3.up*.03f,spawn.Rotation);
                if(PrefabUtility.IsPartOfPrefabInstance(player.transform))PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                var exitGo=new GameObject("Level Exit");exitGo.layer=interact;exitGo.transform.position=definition.Center(exitCell);
                var trigger=exitGo.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,.9f,0);trigger.size=new Vector3(1.5f,1.8f,1.5f);
                var rb=exitGo.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
                // A thin green floor marker and upright marker without solid colliders.
                Marker("Exit Pad",exitGo.transform,new Vector3(0,.012f,0),new Vector3(1.5f,.02f,1.5f),exitMat,interact);
                var indicator=Marker("Exit Beacon",exitGo.transform,new Vector3(0,1.1f,.8f),new Vector3(.65f,1.7f,.08f),exitMat,interact);
                exitGo.AddComponent<LevelExit>().Configure(stats,indicator);
                exitGo.AddComponent<EntityId>();
                EditorSceneManager.MarkSceneDirty(scene);
                UkubolaLevelValidator.CheckScene(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save LevelLab.");
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);
                UkubolaLevelValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P03] LevelLab generated. RetroLab preserved.");
                Debug.Log("[UKUBOLA P03] VALIDATION PASSED | 0 errors.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P03] Setup stopped: "+ex.Message+"\n"+ex+"\nSee README for recovery. Existing assets are not overwritten.");}
        }
        internal static readonly string[] RemovedRoots={"Geometry","Phase01 Terrain","PlayerSpawn_PLACEHOLDER","Billboard Beacon","Directional Dummy","Diagnostic Camera"};
        private static Material Surface(string name,string sourceName,Color tint)
        {
            string path=Generated+"/"+name+".mat";
            if(Exists(path))return Require<Material>(path);
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            if(!shader||!shader.isSupported)throw new InvalidOperationException("URP Unlit unavailable.");
            var mat=new Material(shader){name="Phase03_"+name};
            if(sourceName!=null)
            {
                var source=Require<Material>(UkubolaRetroSetup.Generated+"/"+sourceName+".mat");
                var texture=source.GetTexture("_BaseMap");
                if(!texture||texture.filterMode!=FilterMode.Point||texture.wrapMode!=TextureWrapMode.Repeat)throw new InvalidOperationException("Invalid Phase02 texture: "+sourceName);
                mat.SetTexture("_BaseMap",texture);
            }
            mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetTextureOffset("_BaseMap",Vector2.zero);mat.SetColor("_BaseColor",tint);
            AssetDatabase.CreateAsset(mat,path);return mat;
        }
        private static Renderer Marker(string name,Transform parent,Vector3 center,Vector3 size,Material mat,int layer)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=layer;go.transform.SetParent(parent,false);
            go.transform.localPosition=center;go.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;return renderer;
        }
        [MenuItem("Tools/UKUBOLA/Phase 03/Open Level Lab",false,41)]
        public static void Open()
        {
            if(!CanRun())return;
            try{if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())OpenInternal();}
            catch(Exception ex){Debug.LogError("[UKUBOLA P03] "+ex.Message);}
        }
    }
}
