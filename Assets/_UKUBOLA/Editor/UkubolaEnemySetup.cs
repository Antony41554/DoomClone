using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Rendering;
using Ukubola.World;
using Object=UnityEngine.Object;
namespace Ukubola.EditorTools
{
    public static class UkubolaEnemySetup
    {
        internal const string Root=UkubolaCombatSetup.Root, Source=UkubolaCombatSetup.Lab, Lab=Root+"/Scenes/Tests/EnemyLab.unity", Data=Root+"/Data/Enemies", Generated=Root+"/Generated/Phase06", Prefabs=Root+"/Prefabs/Enemies", Art=Root+"/Art/Sprites/Phase06", LevelId="ENEMY_LAB";
        internal static bool CanRun()=>UkubolaCombatSetup.CanRun();
        internal static T[] Find<T>(Scene scene) where T:Component=>UkubolaCombatSetup.Find<T>(scene);
        internal static T Require<T>(string path) where T:Object=>UkubolaCombatSetup.Require<T>(path);
        internal static bool Exists(string path)=>UkubolaCombatSetup.Exists(path);
        internal static Scene OpenInternal()=>EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);
        private static void Folder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1))))throw new IOException("Cannot create "+path);}
        private static void ImportArt()
        {
            foreach(string kind in new[]{"Guard","Imp"})foreach(string state in new[]{"Idle","Walk","Attack","Pain","Dead"})for(int i=0;i<(state=="Idle"||state=="Walk"?8:1);i++)
            {
                string path=Art+"/"+kind+"_"+state+"_"+i+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)throw new InvalidOperationException("Missing sprite importer: "+path);
                if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single || importer.spritePixelsPerUnit!=40 || importer.filterMode!=FilterMode.Point || importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.Uncompressed || importer.npotScale!=TextureImporterNPOTScale.None || !importer.alphaIsTransparency || importer.wrapMode!=TextureWrapMode.Clamp || importer.alphaSource!=TextureImporterAlphaSource.FromInput)
                {importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=40;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.wrapMode=TextureWrapMode.Clamp;var spriteSettings=new TextureImporterSettings();importer.ReadTextureSettings(spriteSettings);spriteSettings.spriteAlignment=(int)SpriteAlignment.Center;spriteSettings.spritePivot=new Vector2(.5f,.5f);spriteSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(spriteSettings);importer.SaveAndReimport();}
                var sprite=Require<Sprite>(path);if(sprite.rect.width!=48 || sprite.rect.height!=64 || sprite.pivot!=new Vector2(24,32) || sprite.pixelsPerUnit!=40 || sprite.texture.filterMode!=FilterMode.Point)throw new InvalidOperationException("Wrong sprite size: "+path);
            }
        }
        private static Sprite SpriteFor(string kind,string state,int index=0)=>Require<Sprite>(Art+"/"+kind+"_"+state+"_"+index+".png");
        private static void Definition(string name,EnemyAttackKind kind,int hp,int damage,float speed,float range,float interval)
        {
            string path=Data+"/"+name+".asset";if(Exists(path))return;
            var def=ScriptableObject.CreateInstance<EnemyDefinition>();def.Configure(kind,hp,damage,speed,range,interval);AssetDatabase.CreateAsset(def,path);EditorUtility.SetDirty(def);AssetDatabase.SaveAssetIfDirty(def);
        }
        private static NavMeshData Bake(Scene scene,NavMeshBuildSettings settings)
        {
            string path=Generated+"/EnemyNavMesh.asset";if(Exists(path))return Require<NavMeshData>(path);
            var root=scene.GetRootGameObjects().Single(x=>x.name=="Level Geometry");
            var sources=new List<NavMeshBuildSource>();Bounds bounds=new Bounds();bool first=true;
            foreach(string group in new[]{"Floors","Walls"})
            {
                var parent=root.transform.Find(group);if(!parent)throw new InvalidOperationException("Missing geometry group "+group);
                foreach(var box in parent.GetComponents<BoxCollider>())
                {
                    if(!box.enabled || box.isTrigger)continue;
                    // World transform includes collider center; floor boxes walkable, wall boxes not walkable.
                    sources.Add(new NavMeshBuildSource {shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=group=="Floors"?0:1});
                    if(first){bounds=box.bounds;first=false;}else bounds.Encapsulate(box.bounds);
                }
            }
            if(first || sources.Count<2)throw new InvalidOperationException("No navigation geometry.");
            bounds.Expand(1f);
            // Bake settings are local to this asset, not a mutation of project-wide agent settings.
            settings.agentRadius=.35f;settings.agentHeight=1.7f;settings.agentClimb=.30f;settings.agentSlope=45f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
            if(!data)throw new InvalidOperationException("NavMesh bake returned null.");
            AssetDatabase.CreateAsset(data,path);AssetDatabase.SaveAssetIfDirty(data);return Require<NavMeshData>(path);
        }
        private static void CreatePrefab(string kind,int agentType,int enemyLayer,int mask)
        {
            string path=Prefabs+"/"+kind+".prefab";if(Exists(path))return;
            var def=Require<EnemyDefinition>(Data+"/"+kind+".asset");if(!def.IsValid)throw new InvalidOperationException("Invalid enemy definition: "+kind);
            var go=new GameObject(kind);go.SetActive(false);
            try
            {
                go.layer=enemyLayer;
                var hp=go.AddComponent<Health>();hp.Configure(def.Health,null);
                var collider=go.AddComponent<CapsuleCollider>();collider.radius=.32f;collider.height=1.6f;collider.center=Vector3.up*.8f;
                var agent=go.AddComponent<NavMeshAgent>();agent.enabled=false;agent.agentTypeID=agentType;agent.radius=.35f;agent.height=1.7f;agent.speed=def.Speed;agent.acceleration=12;agent.angularSpeed=360;agent.stoppingDistance=1.8f;agent.autoBraking=true;agent.baseOffset=0;
                go.AddComponent<EnemyMotor>();go.AddComponent<EnemyAttack>().Configure(Require<GameObject>(UkubolaCombatSetup.ProjectilePath).GetComponent<Projectile>(),mask);
                var visual=new GameObject("Visual");visual.layer=enemyLayer;visual.transform.SetParent(go.transform,false);visual.transform.localPosition=Vector3.up*.8f;
                var renderer=visual.AddComponent<SpriteRenderer>();renderer.sharedMaterial=Require<Material>(Root+"/Generated/Phase02/SpriteCutout.mat");var billboard=visual.AddComponent<Billboard>();
                go.AddComponent<EnemySpriteView>().Configure(renderer,billboard,Enumerable.Range(0,8).Select(i=>SpriteFor(kind,"Idle",i)).ToArray(),Enumerable.Range(0,8).Select(i=>SpriteFor(kind,"Walk",i)).ToArray(),SpriteFor(kind,"Attack"),SpriteFor(kind,"Pain"),SpriteFor(kind,"Dead"));
                go.AddComponent<EnemyBrain>().SetDefinition(def);go.SetActive(true);
                if(!PrefabUtility.SaveAsPrefabAsset(go,path))throw new IOException("Cannot save "+path);
            }
            finally{Object.DestroyImmediate(go);}
        }
        private static EnemyBrain Spawn(Scene scene,string kind,Vector3 position,Quaternion rotation,Health hp,Camera camera,NoiseEmitter noise,EnemyNavigation nav)
        {
            var prefab=Require<GameObject>(Prefabs+"/"+kind+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.name=kind;go.transform.SetPositionAndRotation(position,rotation);
            var brain=go.GetComponent<EnemyBrain>();brain.Bind(hp,camera,noise,nav);
            EditorUtility.SetDirty(brain);PrefabUtility.RecordPrefabInstancePropertyModifications(brain);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            // IDs belong to scene instances, not shared prefab assets.
            Undo.AddComponent<EntityId>(go);return brain;
        }
        [MenuItem("Tools/UKUBOLA/Phase 06/Setup Enemy Lab",false,70)]
        public static void Setup()
        {
            if(!CanRun())return;bool created=false;
            try
            {
                Require<SceneAsset>(Source);if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                if(Exists(Lab)){UkubolaEnemyValidator.CheckScene(OpenInternal());Debug.Log("[UKUBOLA P06] Existing EnemyLab preserved. VALIDATION PASSED | 0 errors.");return;}
                UkubolaCombatValidator.CheckScene(EditorSceneManager.OpenScene(Source,OpenSceneMode.Single));
                int enemyLayer=LayerMask.NameToLayer("Enemy");if(enemyLayer<0)throw new InvalidOperationException("Enemy layer missing.");
                Folder(Data);Folder(Generated);Folder(Prefabs);ImportArt();
                Definition("Guard",EnemyAttackKind.Hitscan,20,8,2.5f,9f,1.4f);Definition("Imp",EnemyAttackKind.Projectile,60,15,2.8f,10f,1.6f);AssetDatabase.SaveAssets();
                if(NavMesh.GetSettingsCount()==0)throw new InvalidOperationException("No NavMesh agent settings available.");var settings=NavMesh.GetSettingsByIndex(0);
                if(!AssetDatabase.CopyAsset(Source,Lab))throw new IOException("Cannot copy CombatLab.");created=true;AssetDatabase.ImportAsset(Lab,ImportAssetOptions.ForceSynchronousImport);
                var scene=OpenInternal();var mask=Find<WeaponController>(scene).Single().HitMask;
                Bake(scene,settings);CreatePrefab("Guard",settings.agentTypeID,enemyLayer,mask);CreatePrefab("Imp",settings.agentTypeID,enemyLayer,mask);AssetDatabase.SaveAssets();
                // Reacquire scene and persistent references after asset operations.
                var old=Find<CombatDiagnostics>(scene).Single();var hp=old.Player;var weapons=old.Weapons;var readout=old.Readout;var display=Find<RetroDisplay>(scene).Single();
                Object.DestroyImmediate(old.Target.gameObject);Object.DestroyImmediate(old.gameObject);
                var context=Find<LevelContext>(scene).Single();var so=new SerializedObject(context);var prop=so.FindProperty("levelId");if(prop==null)throw new InvalidOperationException("LevelContext.levelId missing.");prop.stringValue=LevelId;so.ApplyModifiedPropertiesWithoutUndo();
                var nav=new GameObject("Enemy Navigation").AddComponent<EnemyNavigation>();nav.Configure(Require<NavMeshData>(Generated+"/EnemyNavMesh.asset"));
                var noise=new GameObject("Player Noise").AddComponent<NoiseEmitter>();noise.Configure(weapons);
                var level=Require<LevelDefinition>(UkubolaLevelSetup.Data);
                var a=new Vector2Int(4,2);var b=new Vector2Int(11,3);
                if(!LevelDefinition.Walkable(level.Tile(a.x,a.y)) || !LevelDefinition.Walkable(level.Tile(b.x,b.y)))throw new InvalidOperationException("Default enemy cells are not walkable.");
                var guard=Spawn(scene,"Guard",level.Center(a),Quaternion.Euler(0,-90,0),hp,display.WorldCamera,noise,nav);
                var imp=Spawn(scene,"Imp",level.Center(b),Quaternion.Euler(0,-90,0),hp,display.WorldCamera,noise,nav);
                readout.text="ENEMY LAB - Press Play";EditorUtility.SetDirty(readout);readout.transform.parent.name="Phase06 Diagnostic Panel";
                new GameObject("Phase06 Diagnostics").AddComponent<EnemyDiagnostics>().Configure(new[]{guard,imp},hp,weapons,noise,Find<PlayerSpawn>(scene).Single(),readout);
                EditorSceneManager.MarkSceneDirty(scene);UkubolaEnemyValidator.CheckScene(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save EnemyLab.");AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);UkubolaEnemyValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P06] EnemyLab generated. CombatLab preserved.");Debug.Log("[UKUBOLA P06] VALIDATION PASSED | 0 errors.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P06] Setup stopped: "+ex.Message+"\n"+ex+(created?"\nEnemyLab may be incomplete; see README recovery.":""));}
        }
        [MenuItem("Tools/UKUBOLA/Phase 06/Open Enemy Lab",false,71)]
        public static void Open(){if(CanRun() && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())try{OpenInternal();}catch(Exception ex){Debug.LogError("[UKUBOLA P06] "+ex.Message);}}
    }
}
