using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.Enemies;
using Ukubola.World;
using Object = UnityEngine.Object;

namespace Ukubola.EditorTools
{
    public static class UkubolaPickupSetup
    {
        internal const string Root=UkubolaEnemySetup.Root, Source=UkubolaEnemySetup.Lab, Lab=Root+"/Scenes/Tests/PickupLab.unity", Data=Root+"/Data/Pickups", Prefabs=Root+"/Prefabs/Pickups", Art=Root+"/Art/Sprites/Phase07", LevelId="PICKUP_LAB";
        internal static bool CanRun()=>UkubolaEnemySetup.CanRun();
        internal static T[] Find<T>(Scene scene) where T:Component=>UkubolaEnemySetup.Find<T>(scene);
        internal static T Require<T>(string path) where T:Object=>UkubolaEnemySetup.Require<T>(path);
        internal static bool Exists(string path)=>UkubolaEnemySetup.Exists(path);
        internal static Scene OpenInternal()=>EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);
        internal sealed class Spec
        {
            internal readonly string Name; internal readonly PickupKind Kind; internal readonly Vector2Int Cell; internal readonly int Amount; internal readonly AmmoType Ammo; internal readonly ArmorKind Armor; internal readonly WeaponId Weapon; internal readonly KeyId Key;
            internal Spec(string name,PickupKind kind,int x,int z,int amount=0,AmmoType ammo=AmmoType.Bullets,ArmorKind armor=ArmorKind.None,WeaponId weapon=WeaponId.Fist,KeyId key=KeyId.None)
            {Name=name;Kind=kind;Cell=new Vector2Int(x,z);Amount=amount;Ammo=ammo;Armor=armor;Weapon=weapon;Key=key;}
        }
        internal static readonly Spec[] Specs={
            new Spec("Stimpack",PickupKind.Health,2,3,10),
            new Spec("Medikit",PickupKind.Health,3,3,25),
            new Spec("GreenArmor",PickupKind.Armor,1,3,armor:ArmorKind.Green),
            new Spec("BlueArmor",PickupKind.Armor,5,5,armor:ArmorKind.Blue),
            new Spec("Bullets",PickupKind.Ammo,1,2,10,AmmoType.Bullets),
            new Spec("Shells",PickupKind.Ammo,4,3,4,AmmoType.Shells),
            new Spec("Rockets",PickupKind.Ammo,5,3,1,AmmoType.Rockets),
            new Spec("Cells",PickupKind.Ammo,5,4,20,AmmoType.Cells),
            new Spec("Shotgun",PickupKind.Weapon,3,2,8,AmmoType.Shells,weapon:WeaponId.Shotgun),
            new Spec("Backpack",PickupKind.Backpack,4,4),
            new Spec("BlueCard",PickupKind.Key,11,2,key:KeyId.BlueCard),
            new Spec("YellowCard",PickupKind.Key,12,3,key:KeyId.YellowCard),
            new Spec("RedCard",PickupKind.Key,13,3,key:KeyId.RedCard),
            new Spec("BlueSkull",PickupKind.Key,3,9,key:KeyId.BlueSkull),
            new Spec("YellowSkull",PickupKind.Key,4,10,key:KeyId.YellowSkull),
            new Spec("RedSkull",PickupKind.Key,13,12,key:KeyId.RedSkull)
        };
        private static void Folder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1))))throw new IOException("Cannot create "+path);}
        private static void ImportArt()
        {
            foreach(var spec in Specs)
            {
                string path=Art+"/"+spec.Name+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer)throw new InvalidOperationException("Missing sprite importer: "+path);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single || importer.spritePixelsPerUnit!=40 || importer.filterMode!=FilterMode.Point || importer.mipmapEnabled || importer.textureCompression!=TextureImporterCompression.Uncompressed || importer.npotScale!=TextureImporterNPOTScale.None || !importer.alphaIsTransparency || importer.alphaSource!=TextureImporterAlphaSource.FromInput || importer.wrapMode!=TextureWrapMode.Clamp || settings.spriteAlignment!=(int)SpriteAlignment.Center || settings.spriteMeshType!=SpriteMeshType.FullRect)
                {
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=40;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.wrapMode=TextureWrapMode.Clamp;
                    importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
                }
                var sprite=Require<Sprite>(path);if(sprite.rect.width!=32 || sprite.rect.height!=32 || sprite.pixelsPerUnit!=40 || sprite.texture.filterMode!=FilterMode.Point)throw new InvalidOperationException("Invalid sprite: "+path);
            }
        }
        private static void CreateDefinition(Spec spec)
        {
            string path=Data+"/"+spec.Name+".asset";
            if(Exists(path))return;
            var definition=ScriptableObject.CreateInstance<PickupDefinition>();
            definition.Configure(spec.Name,spec.Kind,Require<Sprite>(Art+"/"+spec.Name+".png"),spec.Amount,spec.Ammo,spec.Armor,spec.Weapon,spec.Key);
            if(!definition.IsValid){Object.DestroyImmediate(definition);throw new InvalidOperationException("Invalid pickup definition: "+spec.Name);}
            AssetDatabase.CreateAsset(definition,path);EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        }
        private static void CreatePrefab(Spec spec,int layer)
        {
            string path=Prefabs+"/"+spec.Name+".prefab";if(Exists(path))return;
            var go=new GameObject(spec.Name);go.layer=layer;
            try
            {
                var definition=Require<PickupDefinition>(Data+"/"+spec.Name+".asset");
                var item=go.AddComponent<Pickup>();var trigger=go.GetComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.50f;trigger.center=Vector3.up*.40f;
                var body=go.GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                var child=new GameObject("Visual");child.layer=layer;child.transform.SetParent(go.transform,false);child.transform.localPosition=Vector3.up*.43f;
                var renderer=child.AddComponent<SpriteRenderer>();renderer.sprite=definition.Sprite;renderer.sharedMaterial=Require<Material>(Root+"/Generated/Phase02/SpriteCutout.mat");child.AddComponent<Billboard>();item.Configure(definition,renderer);
                if(!PrefabUtility.SaveAsPrefabAsset(go,path))throw new IOException("Cannot save prefab: "+path);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [MenuItem("Tools/UKUBOLA/Phase 07/Setup Pickup Lab",false,80)]
        public static void Setup()
        {
            if(!CanRun())return;bool created=false;
            try
            {
                Require<SceneAsset>(Source);if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                if(Exists(Lab)){UkubolaPickupValidator.CheckScene(OpenInternal());Debug.Log("[UKUBOLA P07] Existing PickupLab preserved. VALIDATION PASSED | 0 errors.");return;}
                UkubolaEnemyValidator.CheckScene(EditorSceneManager.OpenScene(Source,OpenSceneMode.Single));
                int layer=LayerMask.NameToLayer("Pickup"), playerLayer=LayerMask.NameToLayer("Player");
                if(layer<0 || playerLayer<0 || Physics.GetIgnoreLayerCollision(layer,playerLayer))throw new InvalidOperationException("Pickup/Player layers missing or collision matrix disables their triggers.");
                Folder(Data);Folder(Prefabs);ImportArt();foreach(var spec in Specs)CreateDefinition(spec);AssetDatabase.SaveAssets();
                if(!AssetDatabase.CopyAsset(Source,Lab))throw new IOException("Cannot copy EnemyLab.");created=true;AssetDatabase.ImportAsset(Lab,ImportAssetOptions.ForceSynchronousImport);
                var scene=OpenInternal();foreach(var spec in Specs)CreatePrefab(spec,layer);AssetDatabase.SaveAssets();
                // Reacquire scene references after asset import/save operations.
                var old=Find<EnemyDiagnostics>(scene).Single();var display=Find<RetroDisplay>(scene).Single();var level=Require<LevelDefinition>(UkubolaLevelSetup.Data);
                var root=new GameObject("Pickups");var items=new List<Pickup>();
                foreach(var spec in Specs)
                {
                    if(!LevelDefinition.Walkable(level.Tile(spec.Cell.x,spec.Cell.y)))throw new InvalidOperationException("Pickup cell not walkable: "+spec.Name);
                    var prefab=Require<GameObject>(Prefabs+"/"+spec.Name+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                    go.transform.SetParent(root.transform,false);go.transform.position=level.Center(spec.Cell);go.name=spec.Name;
                    var item=go.GetComponent<Pickup>();item.BindViewer(display.WorldCamera);
                    var billboard=item.Visual.GetComponent<Billboard>();EditorUtility.SetDirty(billboard);PrefabUtility.RecordPrefabInstancePropertyModifications(billboard);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                    Undo.AddComponent<EntityId>(go);items.Add(item);
                }
                var diag=new GameObject("Phase07 Diagnostics").AddComponent<PickupDiagnostics>();diag.Configure(items.ToArray(),old.Enemies,old.Player,old.Weapons,old.Noise,old.Spawn,old.Readout);
                old.Readout.text="PICKUP LAB - Press Play";old.Readout.transform.parent.name="Phase07 Diagnostic Panel";
                var panel=old.Readout.transform.parent.GetComponent<RectTransform>();panel.anchorMin=new Vector2(.015f,.65f);panel.anchorMax=new Vector2(.97f,.985f);EditorUtility.SetDirty(panel);EditorUtility.SetDirty(old.Readout);
                Object.DestroyImmediate(old.gameObject);
                var context=Find<LevelContext>(scene).Single();var so=new SerializedObject(context);var prop=so.FindProperty("levelId");if(prop==null)throw new InvalidOperationException("LevelContext.levelId missing.");prop.stringValue=LevelId;so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);UkubolaPickupValidator.CheckScene(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save PickupLab.");AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);UkubolaPickupValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P07] PickupLab generated. EnemyLab preserved.");Debug.Log("[UKUBOLA P07] VALIDATION PASSED | 0 errors.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P07] Setup stopped: "+ex.Message+"\n"+ex+(created?"\nPickupLab may be incomplete; see README recovery.":""));}
        }
        [MenuItem("Tools/UKUBOLA/Phase 07/Open Pickup Lab",false,81)]
        public static void Open(){if(CanRun()&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())try{OpenInternal();}catch(Exception ex){Debug.LogError("[UKUBOLA P07] "+ex.Message);}}
    }
}
