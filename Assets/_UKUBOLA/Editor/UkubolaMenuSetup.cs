using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.UI;
using Ukubola.World;
using Object=UnityEngine.Object;

namespace Ukubola.EditorTools
{
    public static class UkubolaMenuSetup
    {
        internal const string Root=UkubolaHudSetup.Root,Source=UkubolaHudSetup.Lab,Lab=MenuFlow.GamePath,Title=MenuFlow.TitlePath,LevelId="MENU_LAB";
        internal static bool CanRun()=>UkubolaHudSetup.CanRun();
        internal static T[] Find<T>(Scene scene) where T:Component=>UkubolaHudSetup.Find<T>(scene);
        internal static T Require<T>(string path) where T:Object=>UkubolaHudSetup.Require<T>(path);
        internal static bool Exists(string path)=>AssetDatabase.LoadAssetAtPath<SceneAsset>(path)||File.Exists(path);
        private static Font Typeface(){var f=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(!f)throw new InvalidOperationException("Missing built-in UI font.");return f;}
        private static void Save(Scene scene){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save "+scene.path);AssetDatabase.SaveAssets();}
        private static void Register()
        {
            // Only these two entries are repositioned. Other scenes retain their order and enabled flags.
            var others=EditorBuildSettings.scenes.Where(s=>s.path!=Title&&s.path!=Lab);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Title,true),new EditorBuildSettingsScene(Lab,true)}.Concat(others).ToArray();
            UkubolaMenuValidator.CheckBuildList();
        }
        [MenuItem("Tools/UKUBOLA/Phase 10/Setup Menus",false,100)]
        public static void Setup()
        {
            if(!CanRun())return;bool created=false;
            try
            {
                Require<SceneAsset>(Source);if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                if(Exists(Title)||Exists(Lab))
                {
                    if(!Exists(Title)||!Exists(Lab))throw new InvalidOperationException("Partial phase 10 scenes. See README recovery; existing scenes were not overwritten.");
                    UkubolaMenuValidator.CheckGame(EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single));
                    UkubolaMenuValidator.CheckTitle(EditorSceneManager.OpenScene(Title,OpenSceneMode.Single));Register();
                    Debug.Log("[UKUBOLA P10] Existing scenes preserved. VALIDATION PASSED | 0 errors.");return;
                }
                UkubolaHudValidator.CheckScene(EditorSceneManager.OpenScene(Source,OpenSceneMode.Single));
                Require<AudioClip>(UkubolaHudSetup.AudioPath+"/MusicLoop.wav");Typeface();
                if(!AssetDatabase.IsValidFolder(Root+"/Scenes/Menus"))AssetDatabase.CreateFolder(Root+"/Scenes","Menus");
                AssetDatabase.SaveAssets();if(!AssetDatabase.CopyAsset(Source,Lab))throw new IOException("Cannot copy HudLab.");created=true;
                AssetDatabase.ImportAsset(Lab,ImportAssetOptions.ForceSynchronousImport);
                var game=EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single);var hud=Find<ClassicHud>(game).Single();
                // These components remain reference holders; diagnostic keys are not gameplay features.
                hud.World.Source.enabled=false;hud.World.enabled=false;hud.DiagnosticsEnabled=false;hud.Audio.DiagnosticsEnabled=false;
                EditorUtility.SetDirty(hud.World.Source);EditorUtility.SetDirty(hud.World);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(hud.Audio);
                var context=Find<LevelContext>(game).Single();var so=new SerializedObject(context);var prop=so.FindProperty("levelId");if(prop==null)throw new InvalidOperationException("LevelContext.levelId missing.");prop.stringValue=LevelId;so.ApplyModifiedPropertiesWithoutUndo();
                var flow=new GameObject("Phase10 Menu Flow").AddComponent<MenuFlow>();flow.Configure(false,hud,null,Typeface());Save(game);
                // NewScene is saved before validation; no SetActiveScene or isLoaded check on an unsaved scene.
                var empty=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                if(!EditorSceneManager.SaveScene(empty,Title))throw new IOException("Cannot save empty MainMenu.");
                var title=EditorSceneManager.OpenScene(Title,OpenSceneMode.Single);
                var cameraGo=new GameObject("Menu Camera");cameraGo.tag="MainCamera";var camera=cameraGo.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=0;camera.allowHDR=false;camera.allowMSAA=false;cameraGo.AddComponent<AudioListener>();
                var music=new GameObject("Title Music").AddComponent<AudioSource>();music.clip=Require<AudioClip>(UkubolaHudSetup.AudioPath+"/MusicLoop.wav");music.playOnAwake=false;music.loop=true;music.spatialBlend=0;music.volume=.24f;
                var titleFlow=new GameObject("Phase10 Menu Flow").AddComponent<MenuFlow>();titleFlow.Configure(true,null,music,Typeface());Save(title);
                UkubolaMenuValidator.CheckGame(EditorSceneManager.OpenScene(Lab,OpenSceneMode.Single));
                UkubolaMenuValidator.CheckTitle(EditorSceneManager.OpenScene(Title,OpenSceneMode.Single));Register();
                Debug.Log("[UKUBOLA P10] MainMenu and MenuLab generated. HudLab preserved.");Debug.Log("[UKUBOLA P10] VALIDATION PASSED | 0 errors.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P10] Setup stopped: "+ex.Message+"\n"+ex+(created?"\nPhase 10 may be incomplete; see README recovery.":""));}
        }
        private static void Open(string path){if(!CanRun())return;try{Require<SceneAsset>(path);if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(path,OpenSceneMode.Single);}catch(Exception ex){Debug.LogError("[UKUBOLA P10] "+ex.Message);}}
        [MenuItem("Tools/UKUBOLA/Phase 10/Open Main Menu",false,101)] public static void OpenTitle()=>Open(Title);
        [MenuItem("Tools/UKUBOLA/Phase 10/Open Menu Lab",false,102)] public static void OpenGame()=>Open(Lab);
    }
}
