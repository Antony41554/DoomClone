using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Ukubola.UI;
using Ukubola.Audio;
using Ukubola.World;
using Ukubola.Rendering;
using Object=UnityEngine.Object;
namespace Ukubola.EditorTools
{
    public static class UkubolaHudSetup
    {
        internal const string Root=UkubolaWorldSetup.Root,Source=UkubolaWorldSetup.Lab,Lab=Root+"/Scenes/Tests/HudLab.unity",LevelId="HUD_LAB",Art=Root+"/Art/Sprites/Phase09",AudioPath=Root+"/Audio/Phase09";
        internal static bool CanRun()=>UkubolaWorldSetup.CanRun();
        internal static T[] Find<T>(Scene scene) where T:Component=>UkubolaWorldSetup.Find<T>(scene);
        internal static T Require<T>(string path) where T:Object=>UkubolaWorldSetup.Require<T>(path);
        internal static Scene OpenInternal()=>EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);
        private static void ImportTexture(string name)
        {
            string path=Art+"/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)throw new IOException("Missing texture: "+path);
            importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        }
        private static void Place(RectTransform rect,float x1,float y1,float x2,float y2){rect.anchorMin=new Vector2(x1,y1);rect.anchorMax=new Vector2(x2,y2);rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static T Graphic<T>(Transform parent,string name,float x1,float y1,float x2,float y2) where T:UnityEngine.UI.Graphic
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),x1,y1,x2,y2);var g=go.AddComponent<T>();g.raycastTarget=false;return g;
        }
        private static Text Label(Transform p,Font font,string name,string value,Color color,float x1,float y1,float x2,float y2,int max=80)
        {
            var t=Graphic<Text>(p,name,x1,y1,x2,y2);t.font=font;t.text=value;t.color=color;t.fontStyle=FontStyle.Bold;t.fontSize=18;t.resizeTextForBestFit=true;t.resizeTextMinSize=5;t.resizeTextMaxSize=max;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        private static AudioSource Voice(Transform root,string name,bool spatial,float volume)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=false;a.spatialBlend=spatial?1f:0f;a.volume=volume;a.dopplerLevel=0;a.minDistance=2;a.maxDistance=22;a.rolloffMode=AudioRolloffMode.Linear;return a;
        }
        [MenuItem("Tools/UKUBOLA/Phase 09/Setup HUD Lab",false,100)]
        public static void Setup()
        {
            if(!CanRun())return;bool created=false;
            try{
                Require<SceneAsset>(Source);if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Lab)||File.Exists(Lab)){UkubolaHudValidator.CheckScene(OpenInternal());Debug.Log("[UKUBOLA P09] Existing HudLab preserved. VALIDATION PASSED | 0 errors.");return;}
                UkubolaWorldValidator.CheckScene(EditorSceneManager.OpenScene(Source,OpenSceneMode.Single));
                ImportTexture("StatusBar");for(int i=0;i<HudRules.FaceCount;i++)ImportTexture($"Face_{i:00}");
                foreach(var cue in Enum.GetNames(typeof(Cue)).Concat(new[]{"MusicLoop"})){string path=AudioPath+"/"+cue+".wav";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);Require<AudioClip>(path);}
                AssetDatabase.SaveAssets();if(!AssetDatabase.CopyAsset(Source,Lab))throw new IOException("Cannot copy WorldLab.");created=true;AssetDatabase.ImportAsset(Lab,ImportAssetOptions.ForceSynchronousImport);
                var scene=OpenInternal();var world=Find<WorldDiagnostics>(scene).Single();var display=Find<RetroDisplay>(scene).Single();var stats=Find<LevelStats>(scene).Single();var frame=display.Image.transform;
                var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(!font)throw new InvalidOperationException("Missing built-in UI font.");
                // Existing weapon sprite remains in its viewport, now above the status bar.
                var viewport=(RectTransform)world.Source.Weapons.View.Image.transform.parent;Place(viewport,0,.20f,1,1);EditorUtility.SetDirty(viewport);
                var diag=world.Source.Readout.transform.parent.gameObject;Place(diag.GetComponent<RectTransform>(),.015f,.57f,.97f,.87f);diag.SetActive(false);
                var flash=Graphic<Image>(frame,"Damage Flash",0,.20f,1,1);flash.color=Color.clear;
                var bar=Graphic<RawImage>(frame,"Classic Status Bar",0,0,1,.20f);bar.texture=Require<Texture2D>(Art+"/StatusBar.png");
                var title=new Color(.90f,.80f,.62f);var digits=new Color(1f,.32f,.22f);var parent=bar.transform;
                Text Stat(string name,string initial,float x1,float x2){Label(parent,font,name+" Caption",name,title,x1,.08f,x2,.31f);return Label(parent,font,name,initial,digits,x1,.33f,x2,.93f);}
                var ammo=Stat("AMMO","50",.012f,.155f);var hp=Stat("HEALTH","100%",.167f,.326f);var armor=Stat("ARMOR","0%",.508f,.67f);
                var face=Graphic<RawImage>(parent,"Marine Face",.355f,.06f,.48f,.94f);var faces=Enumerable.Range(0,HudRules.FaceCount).Select(i=>Require<Texture2D>(Art+$"/Face_{i:00}.png")).ToArray();face.texture=faces[0];
                var blue=Label(parent,font,"Blue Keys","--",new Color(.37f,.65f,1),.685f,.65f,.79f,.95f);var yellow=Label(parent,font,"Yellow Keys","--",new Color(1,.83f,.20f),.685f,.35f,.79f,.65f);var red=Label(parent,font,"Red Keys","--",new Color(1,.34f,.26f),.685f,.05f,.79f,.35f);
                var reserves=Label(parent,font,"Ammo Reserves","B 50/200\nS 0/50\nR 0/50\nC 0/300",title,.805f,.035f,.99f,.965f);reserves.alignment=TextAnchor.MiddleLeft;
                var messageBg=Graphic<Image>(frame,"Message Backdrop",.015f,.91f,.985f,.99f);messageBg.color=new Color(0,0,0,.55f);
                var notice=Label(messageBg.transform,font,"Notice","UKUBOLA",title,0,0,1,1);var status=Label(frame,font,"Level Status","K 0/2 I 0/16 S 0/1 AI OFF M ON FX ON",title,.015f,.865f,.985f,.91f);status.alignment=TextAnchor.MiddleLeft;
                var audioRoot=new GameObject("Phase09 Audio");var music=Voice(audioRoot.transform,"Music",false,.24f);music.clip=Require<AudioClip>(AudioPath+"/MusicLoop.wav");music.loop=true;var local=Voice(audioRoot.transform,"Local Effects",false,.70f);
                var pool=Enumerable.Range(0,8).Select(i=>Voice(audioRoot.transform,"Spatial Voice "+i,true,.75f)).ToArray();var clips=Enum.GetNames(typeof(Cue)).Select(n=>Require<AudioClip>(AudioPath+"/"+n+".wav")).ToArray();
                var audio=audioRoot.AddComponent<GameAudio>();audio.Configure(world,stats,music,local,pool,clips);
                var hud=bar.gameObject.AddComponent<ClassicHud>();hud.Configure(world,stats,audio,new[]{ammo,hp,armor,reserves,blue,yellow,red,notice,status},face,flash,faces);
                var context=Find<LevelContext>(scene).Single();var so=new SerializedObject(context);var id=so.FindProperty("levelId");if(id==null)throw new InvalidOperationException("LevelContext.levelId missing.");id.stringValue=LevelId;so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);UkubolaHudValidator.CheckScene(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save HudLab.");AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);UkubolaHudValidator.CheckScene(OpenInternal());
                Debug.Log("[UKUBOLA P09] HudLab generated. WorldLab preserved.");Debug.Log("[UKUBOLA P09] VALIDATION PASSED | 0 errors.");
            }catch(Exception ex){Debug.LogError("[UKUBOLA P09] Setup stopped: "+ex.Message+"\n"+ex+(created?"\nHudLab may be incomplete; see README recovery.":""));}
        }
        [MenuItem("Tools/UKUBOLA/Phase 09/Open HUD Lab",false,101)]
        public static void Open(){if(CanRun()&&EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())try{OpenInternal();}catch(Exception ex){Debug.LogError("[UKUBOLA P09] "+ex.Message);}}
    }
}
