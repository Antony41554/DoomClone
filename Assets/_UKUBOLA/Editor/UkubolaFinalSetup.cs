using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
using Ukubola.Persistence;
using Ukubola.Release;
using Ukubola.UI;
using Ukubola.World;
using Object=UnityEngine.Object;
using P14=Ukubola.EditorTools.UkubolaPersistenceSetup;
using MVP=Ukubola.EditorTools.UkubolaMvpSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaFinalSetup
    {
        internal const string Root=MVP.Root, ArtRoot=Root+"/Art/Phase15", AudioRoot=Root+"/Audio/Phase15", Generated=Root+"/Generated/Phase15", ArtPath=Generated+"/ReleaseArt.asset", PostPath=Generated+"/ModernPost.asset";
        internal static readonly string[] Enemies={"Guard","Imp","Brute","Watcher"};
        internal static readonly string[] Items={"Stimpack","Medikit","GreenArmor","BlueArmor","Bullets","Shells","Rockets","Cells","Shotgun","Backpack","BlueCard","YellowCard","RedCard","BlueSkull","YellowSkull","RedSkull","Chainsaw","Chaingun","RocketLauncher","PlasmaRifle","EnergyCannon","Invulnerability","Berserk"};
        internal static readonly string[] SurfaceNames={"Wall","Floor","Ceiling","Door","BlueDoor","RemoteDoor","Hazard","Teleport","Lift","Barrel","Switch"};
        internal static string[] Paths=>P14.Paths;
        internal static bool CanRun()=>MVP.CanRun()&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        internal static void Check(bool condition,string reason){if(!condition)throw new InvalidOperationException(reason);}
        internal static Scene Open(string p)=>EditorSceneManager.OpenScene(p,OpenSceneMode.Single);
        internal static T[] Find<T>(Scene s) where T:Component=>P14.Find<T>(s);
        private static T Need<T>(string path) where T:Object=>MVP.Require<T>(path);
        private static void Folder(){if(!AssetDatabase.IsValidFolder(Generated))Check(!string.IsNullOrEmpty(AssetDatabase.CreateFolder(Root+"/Generated","Phase15")),"Cannot create Phase15 generated folder");}
        private static string AssetName(string file)=>Path.GetFileNameWithoutExtension(file);
        private static bool WeaponTexture(string name)=>Enum.GetNames(typeof(WeaponId)).Any(n=>name==n+"_Idle"||name==n+"_Fire"||name==n+"_Recover");
        internal static bool IsSprite(string path)=>path.Contains("/Sprites/")&&!WeaponTexture(AssetName(path));
        private static void Import(string path)
        {
            var i=AssetImporter.GetAtPath(path) as TextureImporter;Check(i,"No texture importer "+path);bool sprite=IsSprite(path);
            i.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=1024;i.npotScale=TextureImporterNPOTScale.None;i.alphaSource=TextureImporterAlphaSource.FromInput;i.alphaIsTransparency=true;i.mipmapEnabled=path.Contains("/Textures/")&&char.IsDigit(AssetName(path)[0]);i.wrapMode=i.mipmapEnabled?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            if(sprite){i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=AssetName(path).StartsWith("Item_")?64:80;var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);settings.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(settings);}
            i.SaveAndReimport();
        }
        private static string[] PngPaths()=>Directory.GetFiles(ArtRoot,"*.png",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        private static AudioClip Sound(string n)=>Need<AudioClip>(AudioRoot+"/"+n+".wav");
        private static VolumeProfile CreateProfile()
        {
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostPath);if(profile)return profile;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,PostPath);
            var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(.9f);bloom.intensity.Override(.80f);bloom.scatter.Override(.67f);bloom.tint.Override(new Color(.93f,1f,.98f));
            var grading=profile.Add<ColorAdjustments>(true);grading.postExposure.Override(.12f);grading.contrast.Override(11f);grading.saturation.Override(-5f);grading.colorFilter.Override(Color.white);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.18f);vignette.smoothness.Override(.45f);vignette.rounded.Override(false);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
            foreach(var c in profile.components){c.name=c.GetType().Name;AssetDatabase.AddObjectToAsset(c,profile);EditorUtility.SetDirty(c);}EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();return Need<VolumeProfile>(PostPath);
        }
        internal static ReleaseArt GenerateArt()
        {
            Folder();var kit=AssetDatabase.LoadAssetAtPath<ReleaseArt>(ArtPath);if(kit){ValidateArt(kit);return kit;}
            var paths=PngPaths();Check(paths.Length==582,"Expected 582 PNG resources; reinstall full phase15 zip");
            foreach(string p in paths)Import(p);
            foreach(string p in Directory.GetFiles(AudioRoot,"*.wav")){var importer=AssetImporter.GetAtPath(p.Replace('\\','/')) as AudioImporter;Check(importer,"Missing audio importer "+p);importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
            kit=ScriptableObject.CreateInstance<ReleaseArt>();kit.Textures=paths.Where(p=>!IsSprite(p)).Select(Need<Texture2D>).ToArray();kit.Sprites=paths.Where(IsSprite).Select(Need<Sprite>).ToArray();
            kit.Music=Enumerable.Range(0,3).Select(i=>Sound("Mission"+i)).ToArray();kit.Effects=Enum.GetNames(typeof(Ukubola.Audio.Cue)).Select(Sound).ToArray();kit.Weapons=Enum.GetNames(typeof(WeaponId)).Select(Sound).ToArray();
            kit.Surface=Need<Shader>(ArtRoot+"/Shaders/ReleaseSurface.shader");kit.Particle=Need<Shader>(ArtRoot+"/Shaders/ReleaseParticle.shader");kit.Post=CreateProfile();kit.SceneStamps=null;AssetDatabase.CreateAsset(kit,ArtPath);AssetDatabase.SaveAssets();kit=Need<ReleaseArt>(ArtPath);ValidateArt(kit);return kit;
        }
        internal static void ValidateArt(ReleaseArt a)
        {
            Check(a&&a.Textures!=null&&a.Sprites!=null&&a.Textures.Length==79&&a.Sprites.Length==503&&a.Textures.All(t=>t)&&a.Sprites.All(t=>t),"Missing art reference or unexpected count");
            Check(a.Textures.Select(t=>t.name).Distinct().Count()==79&&a.Sprites.Select(t=>t.name).Distinct().Count()==503,"Duplicate art keys");
            Check(a.Surface&&a.Particle&&a.Surface.isSupported&&a.Particle.isSupported&&!ShaderUtil.ShaderHasError(a.Surface)&&!ShaderUtil.ShaderHasError(a.Particle),"Phase15 shader import failed (URP required)");
            Check(a.Post&&a.Post.TryGet<Bloom>(out _)&&a.Post.TryGet<Tonemapping>(out _)&&a.Post.TryGet<Vignette>(out _)&&a.Post.TryGet<ColorAdjustments>(out _),"Missing post effects");
            Check(a.Music!=null&&a.Music.Length==3&&a.Music.All(c=>c&&c.length>=15.9f)&&a.Effects!=null&&a.Effects.Length==16&&a.Effects.All(c=>c&&c.length>0)&&a.Weapons!=null&&a.Weapons.Length==8&&a.Weapons.All(c=>c&&c.length>0),"Missing audio");
            foreach(var name in Enemies){for(int d=0;d<8;d++){Check(a.Sprite(name+"_Idle_"+d),"Missing idle "+name);for(int i=0;i<4;i++)Check(a.Sprite(name+"_Walk_"+d+"_"+i),"Missing walk "+name);}for(int d=0;d<8;d++){for(int i=0;i<4;i++){Check(a.Sprite(name+"_Attack_"+d+"_"+i),"Missing attack "+name);Check(a.Sprite(name+"_Dead_"+d+"_"+i),"Missing death "+name);}for(int i=0;i<2;i++)Check(a.Sprite(name+"_Pain_"+d+"_"+i),"Missing hurt "+name);}}
            foreach(string n in Items)Check(a.Sprite("Item_"+n),"Missing item "+n);
            foreach(string n in Enum.GetNames(typeof(WeaponId)))foreach(string state in new[]{"Idle","Fire","Recover"})Check(a.Texture(n+"_"+state),"Missing weapon "+n+" "+state);
            for(int i=0;i<16;i++)Check(a.Texture("Face_"+i.ToString("00")),"Missing face");for(int stage=0;stage<3;stage++)foreach(var n in SurfaceNames)Check(a.Texture(stage+"_"+n),"Missing surface "+n);
            foreach(string key in new[]{"StatusBar","MenuBackground","Logo","Spark","Smoke","Glow"})Check(a.Texture(key),"Missing texture "+key);
            foreach(var t in a.Textures)Check(t.filterMode==FilterMode.Point,"Non-point art "+t.name);foreach(var s in a.Sprites)Check(s.texture.filterMode==FilterMode.Point&&Mathf.Approximately(s.pixelsPerUnit,s.name.StartsWith("Item_")?64:80),"Wrong sprite sampling/size "+s.name);
        }
        private static string F(float x)=>x.ToString("F5",CultureInfo.InvariantCulture);
        private static string V(Vector3 p)=>F(p.x)+","+F(p.y)+","+F(p.z);
        private static string Identity(Transform t)=>t.parent?Identity(t.parent)+"/"+t.GetSiblingIndex()+":"+t.name:t.GetSiblingIndex()+":"+t.name;
        private static string AssetId(Object o)=>o?AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)):"-";
        // A separate fingerprint of collider geometry and persistent identities. The existing SaveWorld signature stays intact.
        internal static string Stamp(Scene s)
        {
            var text=new StringBuilder();var flow=Find<MenuFlow>(s).Single();text.Append(s.path).Append('|').Append(flow.RouteGame).Append('|').Append(flow.RouteTitle);
            if(flow.Hud)text.Append('|').Append(SaveWorld.Signature(flow.Hud));
            foreach(var t in Find<Transform>(s)){text.Append('\n').Append(Identity(t)).Append('|').Append(t.gameObject.layer).Append('|').Append(t.gameObject.activeSelf).Append('|').Append(V(t.localPosition)).Append('|').Append(F(t.localRotation.x)).Append(',').Append(F(t.localRotation.y)).Append(',').Append(F(t.localRotation.z)).Append(',').Append(F(t.localRotation.w)).Append('|').Append(V(t.localScale));}
            foreach(var c in Find<Collider>(s)){
                text.Append('\n').Append(Identity(c.transform)).Append('|').Append(c.GetType().Name).Append('|').Append(c.enabled).Append('|').Append(c.isTrigger);
                if(c is BoxCollider b)text.Append('|').Append(V(b.center)).Append('|').Append(V(b.size));
                else if(c is SphereCollider sp)text.Append('|').Append(V(sp.center)).Append('|').Append(F(sp.radius));
                else if(c is CapsuleCollider cap)text.Append('|').Append(V(cap.center)).Append('|').Append(F(cap.radius)).Append('|').Append(F(cap.height)).Append('|').Append(cap.direction);
                else if(c is CharacterController cc)text.Append('|').Append(V(cc.center)).Append('|').Append(F(cc.radius)).Append('|').Append(F(cc.height)).Append('|').Append(F(cc.stepOffset)).Append('|').Append(F(cc.slopeLimit)).Append('|').Append(F(cc.skinWidth));
                else if(c is MeshCollider mc)text.Append('|').Append(AssetId(mc.sharedMesh));
            }
            foreach(var b in Find<Rigidbody>(s))text.Append('\n').Append(Identity(b.transform)).Append('|').Append(b.isKinematic).Append('|').Append(b.useGravity);
            foreach(var n in Find<EnemyNavigation>(s))text.Append('\n').Append(AssetId(n.Data));
            foreach(var o in Find<NavMeshObstacle>(s))text.Append('\n').Append(Identity(o.transform)).Append('|').Append(V(o.center)).Append('|').Append(V(o.size)).Append('|').Append(o.carving).Append('|').Append(o.enabled);
            foreach(var e in Find<EntityId>(s)){text.Append('\n').Append(Identity(e.transform));var so=new SerializedObject(e);var prop=so.GetIterator();while(prop.NextVisible(true)){if(prop.propertyType==SerializedPropertyType.String&&!prop.propertyPath.StartsWith("m_"))text.Append('|').Append(prop.propertyPath).Append(':').Append(prop.stringValue);}}
            return SaveStore.Hash(text.ToString());
        }
        private static void Configure(Scene s,ReleaseArt art,int stage)
        {
            var f=Find<MenuFlow>(s).Single();Check(f.PersistenceEnabled,"Phase14 setup required");var existing=f.GetComponent<ReleasePresentation>();var v=existing?existing.PostVolume:null;
            if(stage>=0&&!v){Check(!f.GetComponent<Volume>(),"Another Volume already exists on MenuFlow");v=f.gameObject.AddComponent<Volume>();v.enabled=false;v.isGlobal=true;v.priority=100;v.sharedProfile=art.Post;EditorUtility.SetDirty(v);}
            var presentation=existing?existing:f.gameObject.AddComponent<ReleasePresentation>();presentation.Configure(art,stage,v);EditorUtility.SetDirty(presentation);MVP.Save(s);
        }
        internal static void ValidateAll(ReleaseArt art)
        {
            ValidateArt(art);Check(art.SceneStamps!=null&&art.SceneStamps.Length==4,"Missing baseline; run Setup Final Release");
            P14.ValidateAll(); // Includes isolated NavMesh routes and save layout identity.
            for(int i=0;i<Paths.Length;i++){
                Scene s=Open(Paths[i]);var f=Find<MenuFlow>(s).Single();var r=Find<ReleasePresentation>(s).Single();Check(r.gameObject==f.gameObject&&r.enabled&&r.Art==art&&r.Stage==i-1,"Incorrect release binding: "+s.path);
                Check(Stamp(s)==art.SceneStamps[i],"Physical or persistent layout changed: "+s.path+". See README before changing the baseline.");
                if(i==0){Check(!r.PostVolume,"Title has an unnecessary Volume");continue;}
                Check(r.PostVolume&&!r.PostVolume.enabled&&r.PostVolume.sharedProfile==art.Post&&r.PostVolume.isGlobal,"Missing persistent post volume: "+s.path);
                var w=f.Hud.World;foreach(var e in w.Source.Enemies)Check(Enemies.Contains(e.Definition.name),"No new art for enemy "+e.Definition.name);foreach(var p in w.Source.Pickups)Check(art.Sprite("Item_"+p.Definition.Sprite.name.Replace("_Pickup","")),"No new art for pickup "+p.Definition.Sprite.name);
                Check(EditorBuildSettings.scenes.Count(x=>x.path==s.path&&x.enabled)==1,"Mission not registered: "+s.path);
            }
            Check(EditorBuildSettings.scenes.Count(x=>x.path==Paths[0]&&x.enabled)==1,"Title not registered");Open(Paths[0]);
        }
        private static string ProjectRoot=>Directory.GetParent(Application.dataPath).FullName;
        [MenuItem("Tools/UKUBOLA/Phase 15/Generate Visual Assets",false,151)]public static void Generate(){if(!CanRun())return;try{GenerateArt();Debug.Log("[UKUBOLA P15] ART READY | 582 PNG + 25 WAV | 2 shaders");}catch(Exception ex){Debug.LogError("[UKUBOLA P15] ART FAILED: "+ex);}}
        [MenuItem("Tools/UKUBOLA/Phase 15/Setup Final Release",false,150)]public static void Setup()
        {
            if(!CanRun())return;string backup=null;bool changed=false;ReleaseArt kit=null;string[] previousStamps=null;
            try{
                P14.ValidateAll();kit=GenerateArt();var paths=Paths;var stamps=new string[paths.Length];int installed=0;
                for(int i=0;i<paths.Length;i++){var s=Open(paths[i]);stamps[i]=Stamp(s);var r=Find<ReleasePresentation>(s);Check(r.Length<=1,"Duplicate release components");if(r.Length==1)installed++;}
                if(installed==paths.Length){ValidateAll(kit);Debug.Log("[UKUBOLA P15] Existing release preserved. VALIDATION PASSED | 0 errors.");return;}
                Check(installed==0,"Partial release. Run Remove Visual Layer, then Setup Final Release.");
                backup=Path.Combine(ProjectRoot,"Backups","UKUBOLA_P15_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));Directory.CreateDirectory(backup);foreach(var p in paths)File.Copy(p,Path.Combine(backup,Path.GetFileName(p)),false);
                previousStamps=kit.SceneStamps;kit.SceneStamps=stamps;EditorUtility.SetDirty(kit);AssetDatabase.SaveAssets();changed=true;
                for(int i=0;i<paths.Length;i++)Configure(Open(paths[i]),kit,i-1);
                ValidateAll(kit);Debug.Log("[UKUBOLA P15] FINAL RELEASE READY | VALIDATION PASSED | 0 errors. Backup: "+backup);
            }catch(Exception ex){
                if(changed&&backup!=null){try{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);foreach(var p in Paths){File.Copy(Path.Combine(backup,Path.GetFileName(p)),p,true);AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);}if(kit){kit.SceneStamps=previousStamps;EditorUtility.SetDirty(kit);AssetDatabase.SaveAssets();}Open(Paths[0]);Debug.LogWarning("[UKUBOLA P15] Scene changes rolled back. Backup: "+backup);}catch(Exception rollback){Debug.LogError("[UKUBOLA P15] Rollback failed: "+rollback+" | Backup: "+backup);}}
                Debug.LogError("[UKUBOLA P15] SETUP STOPPED: "+ex);
            }
        }
        [MenuItem("Tools/UKUBOLA/Phase 15/Open Final Title",false,152)]public static void OpenTitle(){if(CanRun())Open(Paths[0]);}
        [MenuItem("Tools/UKUBOLA/Phase 15/Validate Final Release",false,153)]public static void Validate(){if(!CanRun())return;try{ValidateAll(Need<ReleaseArt>(ArtPath));Debug.Log("[UKUBOLA P15] VALIDATION PASSED | 0 errors.");}catch(Exception ex){Debug.LogError("[UKUBOLA P15] VALIDATION FAILED: "+ex);}}
        [MenuItem("Tools/UKUBOLA/Phase 15/Remove Visual Layer",false,159)]public static void Remove()
        {
            if(!CanRun()||!EditorUtility.DisplayDialog("UKUBOLA Phase 15","Remove only the release presentation components from the four phase14 scenes? Saves, gameplay and art files will remain.","Remove","Cancel"))return;
            try{foreach(string p in Paths){var s=Open(p);foreach(var r in Find<ReleasePresentation>(s)){if(r.PostVolume)Object.DestroyImmediate(r.PostVolume);Object.DestroyImmediate(r);}MVP.Save(s);}Open(Paths[0]);Debug.Log("[UKUBOLA P15] Visual layer removed. Phase14 routes and saves retained.");}catch(Exception ex){Debug.LogError("[UKUBOLA P15] Remove failed: "+ex);}
        }
    }
}
