using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
using Object=UnityEngine.Object;
namespace Ukubola.EditorTools
{
    public static class UkubolaWorldSetup
    {
        internal const string Root=UkubolaPickupSetup.Root, Source=UkubolaPickupSetup.Lab, Lab=Root+"/Scenes/Tests/WorldLab.unity", Generated=Root+"/Generated/Phase08", LevelId="WORLD_LAB";
        internal static bool CanRun()=>UkubolaPickupSetup.CanRun();
        internal static T[] Find<T>(Scene s) where T:Component=>UkubolaPickupSetup.Find<T>(s);
        internal static T Require<T>(string path) where T:Object=>UkubolaPickupSetup.Require<T>(path);
        internal static Scene OpenInternal()=>EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);
        private static bool Exists(string path)=>UkubolaPickupSetup.Exists(path);
        private static Material MakeMaterial(string name,Color color)
        {
            string path=Generated+"/"+name+".mat";if(Exists(path))return Require<Material>(path);
            var shader=Shader.Find("Universal Render Pipeline/Unlit");if(!shader || !shader.isSupported)throw new InvalidOperationException("URP/Unlit unavailable.");
            var m=new Material(shader){name=name};m.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(m,path);EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        private static GameObject RootObject(string name,Transform parent,Vector3 pos,int layer)
        {var go=new GameObject(name);go.layer=layer;go.transform.SetParent(parent,false);go.transform.position=pos;return go;}
        private static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,int layer,bool collider=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=layer;go.transform.SetParent(parent,false);go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collider)Object.DestroyImmediate(go.GetComponent<BoxCollider>());return go;
        }
        private static void Id(GameObject go)=>go.AddComponent<EntityId>();
        private static void Obstacle(GameObject go,Vector3 size,Vector3 center)
        {var o=go.GetComponent<NavMeshObstacle>();if(!o)o=go.AddComponent<NavMeshObstacle>();o.shape=NavMeshObstacleShape.Box;o.size=size;o.center=center;o.carving=true;o.carveOnlyStationary=false;}
        private static Door CreateDoor(string name,Transform root,Vector3 floor,Vector3 size,Material material,int layer,KeyId key=KeyId.None,bool any=false,bool remote=false,bool stays=false)
        {
            var go=RootObject(name,root,floor,layer);var panel=Cube("Panel",go.transform,floor+Vector3.up*size.y*.5f,size,material,layer);
            var door=go.AddComponent<Door>();door.Configure(panel.transform,key,any,remote,stays,size.y);Obstacle(go,size,Vector3.up*size.y*.5f);Id(go);return door;
        }
        private static WorldSwitch CreateSwitch(string name,Transform root,Vector3 pos,Material mat,int layer,Door door,Lift lift)
        {
            var go=RootObject(name,root,pos,layer);var panel=Cube("Switch panel",go.transform,pos+Vector3.up*1.35f,new Vector3(.55f,2.7f,.28f),mat,layer);
            var sw=go.AddComponent<WorldSwitch>();sw.Configure(door,lift,panel.GetComponent<Renderer>());Obstacle(go,new Vector3(.55f,2.7f,.28f),Vector3.up*1.35f);Id(go);return sw;
        }
        private static T Trigger<T>(string name,Transform root,Vector3 floor,Vector3 size,int layer) where T:Component
        {
            var go=RootObject(name,root,floor,layer);var box=go.AddComponent<BoxCollider>();box.size=size;box.center=Vector3.up*size.y*.5f;box.isTrigger=true;
            var body=go.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;var result=go.AddComponent<T>();Id(go);return result;
        }
        private static ExplosiveBarrel Barrel(string name,Transform root,Vector3 floor,Material material,int layer,int mask)
        {
            // Solid diagnostic barrel; the navigation obstacle uses the unit cube local bounds.
            var go=Cube(name,root,floor+Vector3.up*.825f,new Vector3(.65f,1.65f,.65f),material,layer);
            go.AddComponent<Health>().Configure(20,null);var barrel=go.AddComponent<ExplosiveBarrel>();barrel.Configure(mask);Obstacle(go,Vector3.one,Vector3.zero);Id(go);return barrel;
        }
        [MenuItem("Tools/UKUBOLA/Phase 08/Setup World Lab",false,90)]
        public static void Setup()
        {
            if(!CanRun())return;bool created=false;
            try
            {
                Require<SceneAsset>(Source);if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                if(Exists(Lab)){UkubolaWorldValidator.CheckScene(OpenInternal());Debug.Log("[UKUBOLA P08] Existing WorldLab preserved. VALIDATION PASSED | 0 errors.");return;}
                UkubolaPickupValidator.CheckScene(EditorSceneManager.OpenScene(Source,OpenSceneMode.Single));
                if(!AssetDatabase.IsValidFolder(Generated) && string.IsNullOrEmpty(AssetDatabase.CreateFolder(Root+"/Generated","Phase08")))throw new IOException("Cannot create Phase08 folder.");
                MakeMaterial("Door",new Color(.44f,.49f,.55f));MakeMaterial("BlueDoor",new Color(.12f,.36f,.85f));MakeMaterial("RemoteDoor",new Color(.65f,.41f,.12f));MakeMaterial("Switch",new Color(1f,.3f,.1f));MakeMaterial("Lift",new Color(.56f,.52f,.25f));MakeMaterial("Hazard",new Color(.85f,.24f,.06f));MakeMaterial("Teleport",new Color(.10f,.68f,.87f));MakeMaterial("Barrel",new Color(.23f,.67f,.25f));AssetDatabase.SaveAssets();
                if(!AssetDatabase.CopyAsset(Source,Lab))throw new IOException("Cannot copy PickupLab.");created=true;AssetDatabase.ImportAsset(Lab,ImportAssetOptions.ForceSynchronousImport);
                var scene=OpenInternal();var old=Find<PickupDiagnostics>(scene).Single();var player=old.Player;var weapons=old.Weapons;
                var level=Require<LevelDefinition>(UkubolaLevelSetup.Data);
                int world=LayerMask.NameToLayer("World"),interact=LayerMask.NameToLayer("Interactable"),enemy=LayerMask.NameToLayer("Enemy"),pl=LayerMask.NameToLayer("Player");
                if(world<0||interact<0||enemy<0||pl<0||Physics.GetIgnoreLayerCollision(pl,interact)||Physics.GetIgnoreLayerCollision(pl,world)||Physics.GetIgnoreLayerCollision(pl,enemy))throw new InvalidOperationException("Required layers or collision matrix invalid.");
                if((weapons.HitMask&(1<<interact))==0 || (weapons.HitMask&(1<<world))==0 || (weapons.TargetMask&(1<<enemy))==0)throw new InvalidOperationException("Weapon collision masks missing world layers.");
                foreach(var cell in new[]{new Vector2Int(2,10),new Vector2Int(2,11),new Vector2Int(6,9),new Vector2Int(6,10),new Vector2Int(15,5),new Vector2Int(17,1),new Vector2Int(16,1),new Vector2Int(17,2),new Vector2Int(3,4),new Vector2Int(4,5),new Vector2Int(12,5)})
                    if(!LevelDefinition.Walkable(level.Tile(cell.x,cell.y))||LevelDefinition.Elevation(level.Tile(cell.x,cell.y))!=0)throw new InvalidOperationException("WorldLab expects original flat cell: "+cell);
                Material M(string n)=>Require<Material>(Generated+"/"+n+".mat");
                var root=new GameObject("Phase08 World").transform;
                var normal=CreateDoor("Normal Door",root,new Vector3(16,0,8),new Vector3(.24f,3.2f,4.04f),M("Door"),interact);
                var blue=CreateDoor("Blue Door",root,new Vector3(8,0,15),new Vector3(4.04f,3.2f,.24f),M("BlueDoor"),interact,KeyId.BlueCard|KeyId.BlueSkull,true);
                var remote=CreateDoor("Remote Door",root,new Vector3(28,0,15),new Vector3(4.04f,3.2f,.24f),M("RemoteDoor"),interact,remote:true,stays:true);
                var secret=CreateDoor("Secret Door",root,new Vector3(34,0,3),new Vector3(.24f,3.2f,2.04f),Require<Material>(Root+"/Generated/Phase03/Wall.mat"),interact,stays:true);
                // The north/east existing walls close the other sides of this one-cell room.
                var secretWall=RootObject("Secret South Wall",root,new Vector3(35,0,4),world);Cube("Solid",secretWall.transform,secretWall.transform.position+Vector3.up*1.6f,new Vector3(2.24f,3.2f,.24f),Require<Material>(Root+"/Generated/Phase03/Wall.mat"),world);Obstacle(secretWall,new Vector3(2.24f,3.2f,.24f),Vector3.up*1.6f);
                var secretTrigger=Trigger<SecretTrigger>("Secret Room",root,level.Center(new Vector2Int(17,1)),new Vector3(1.3f,2f,1.3f),interact);
                var liftRoot=RootObject("Lift",root,level.Center(new Vector2Int(2,10)),interact);var platform=Cube("Platform",liftRoot.transform,liftRoot.transform.position-Vector3.up*.05f,new Vector3(1.7f,.2f,1.7f),M("Lift"),interact);
                var lift=liftRoot.AddComponent<Lift>();lift.Configure(platform.transform,player.GetComponent<CharacterController>());Obstacle(liftRoot,new Vector3(1.7f,1.5f,1.7f),Vector3.up*.75f);Id(liftRoot);
                var liftSwitch=CreateSwitch("Lift Switch",root,new Vector3(5,0,22.55f),M("Switch"),interact,null,lift);
                var doorSwitch=CreateSwitch("Door Switch",root,level.Center(new Vector2Int(12,5)),M("Switch"),interact,remote,null);
                var zone=Trigger<DamageZone>("Damage Zone",root,level.Center(new Vector2Int(6,9)),new Vector3(1.8f,1.2f,1.8f),interact);
                Cube("Hazard Marker",zone.transform,zone.transform.position+Vector3.up*.012f,new Vector3(1.8f,.02f,1.8f),M("Hazard"),interact,false);
                var teleport=Trigger<Teleport>("Teleport",root,level.Center(new Vector2Int(6,10)),new Vector3(1.4f,1.5f,1.4f),interact);
                var destination=RootObject("Teleport Destination",root,level.Center(new Vector2Int(15,5)),interact);destination.transform.rotation=Quaternion.Euler(0,180,0);teleport.Configure(destination.transform);
                Cube("Teleport Marker",teleport.transform,teleport.transform.position+Vector3.up*.012f,new Vector3(1.6f,.02f,1.6f),M("Teleport"),interact,false);Cube("Destination Marker",destination.transform,destination.transform.position+Vector3.up*.012f,new Vector3(1.6f,.02f,1.6f),M("Teleport"),interact,false);
                var b1=Barrel("Barrel A",root,level.Center(new Vector2Int(3,4)),M("Barrel"),enemy,weapons.HitMask);var b2=Barrel("Barrel B",root,level.Center(new Vector2Int(4,5))+new Vector3(-.5f,0,-.5f),M("Barrel"),enemy,weapons.HitMask);
                var use=Undo.AddComponent<PlayerInteractor>(player.gameObject);
                var diag=new GameObject("Phase08 Diagnostics").AddComponent<WorldDiagnostics>();diag.Configure(old,use,new[]{normal,blue,remote,secret},new[]{doorSwitch,liftSwitch},lift,zone,teleport,secretTrigger,new[]{b1,b2});
                old.Readout.text="WORLD LAB - Press Play";old.Readout.transform.parent.name="World Diagnostic Panel";var rect=old.Readout.transform.parent.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.015f,.60f);rect.anchorMax=new Vector2(.97f,.985f);EditorUtility.SetDirty(rect);EditorUtility.SetDirty(old.Readout);
                var context=Find<LevelContext>(scene).Single();var serialized=new SerializedObject(context);var prop=serialized.FindProperty("levelId");if(prop==null)throw new InvalidOperationException("LevelContext.levelId missing.");prop.stringValue=LevelId;serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);UkubolaWorldValidator.CheckScene(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save WorldLab.");AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);UkubolaWorldValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P08] WorldLab generated. PickupLab preserved.");Debug.Log("[UKUBOLA P08] VALIDATION PASSED | 0 errors.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P08] Setup stopped: "+ex.Message+"\n"+ex+(created?"\nWorldLab may be incomplete; see README recovery.":""));}
        }
        [MenuItem("Tools/UKUBOLA/Phase 08/Open World Lab",false,91)]
        public static void Open(){if(CanRun()&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())try{OpenInternal();}catch(Exception ex){Debug.LogError("[UKUBOLA P08] "+ex.Message);}}
    }
}
