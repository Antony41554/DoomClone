using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ukubola.UI;
using Ukubola.World;
using Ukubola.Player;
using Ukubola.Enemies;
using Ukubola.Rendering;
using Object=UnityEngine.Object;
namespace Ukubola.EditorTools
{
    public static class UkubolaMvpSetup
    {
        internal const string Root=UkubolaMenuSetup.Root, Title=Root+"/Scenes/Menus/MvpTitle.unity", Mission=Root+"/Scenes/Levels/E1M1_TheOutpost.unity", LevelId="E1M1_OUTPOST", Label="E1M1 / THE OUTPOST - MVP";
        internal static bool CanRun()=>UkubolaMenuSetup.CanRun();
        internal static T[] Find<T>(Scene s) where T:Component=>UkubolaMenuSetup.Find<T>(s);
        internal static T Require<T>(string path) where T:Object=>UkubolaMenuSetup.Require<T>(path);
        internal static bool Exists(string path)=>UkubolaMenuSetup.Exists(path);
        internal static void Save(Scene s){EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Cannot save "+s.path);AssetDatabase.SaveAssets();}
        internal static void Register()
        {
            // Preserve the Phase 10 list and all other entries. The explicit MVP build uses only the two new scenes.
            var old=EditorBuildSettings.scenes.Where(x=>x.path!=Title&&x.path!=Mission);
            EditorBuildSettings.scenes=old.Concat(new[]{new EditorBuildSettingsScene(Title,true),new EditorBuildSettingsScene(Mission,true)}).ToArray();
            UkubolaMvpValidator.CheckBuildList();
        }
        private static void Copy(string source,string target)
        {if(!AssetDatabase.CopyAsset(source,target))throw new IOException("Cannot copy "+source+" to "+target);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceSynchronousImport);}
        private static void Dirty(Object value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
        private static Text MakeObjectiveLabel(Transform frame,Font font)
        {
            var go=new GameObject("Mission Objective",typeof(RectTransform),typeof(Text));var rt=(RectTransform)go.transform;rt.SetParent(frame,false);
            rt.anchorMin=new Vector2(.015f,.815f);rt.anchorMax=new Vector2(.985f,.862f);rt.offsetMin=rt.offsetMax=Vector2.zero;
            var text=go.GetComponent<Text>();text.font=font;text.fontSize=16;text.resizeTextForBestFit=true;text.resizeTextMinSize=8;text.resizeTextMaxSize=40;text.alignment=TextAnchor.MiddleLeft;text.color=new Color(1,.83f,.42f);text.raycastTarget=false;text.text="1/3 FIND BLUE CARD - EAST WING";return text;
        }
        private static EnemyBrain AddEnemy(Scene s,MvpLayout.Placement p,LevelDefinition level,PickupDiagnostics source,EnemyNavigation nav)
        {
            var prefab=Require<GameObject>(UkubolaEnemySetup.Prefabs+"/"+p.Prefab+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,s);go.name=p.Name;
            go.transform.SetPositionAndRotation(level.Center(p.Cell),Quaternion.Euler(0,p.Yaw,0));var brain=go.GetComponent<EnemyBrain>();brain.Bind(source.Player,source.Weapons.AimCamera,source.Noise,nav);
            Undo.AddComponent<EntityId>(go);Dirty(go.transform);Dirty(brain);return brain;
        }
        private static Pickup AddPickup(Scene s,MvpLayout.Placement p,LevelDefinition level,Transform parent,Camera camera)
        {
            var prefab=Require<GameObject>(UkubolaPickupSetup.Prefabs+"/"+p.Prefab+".prefab");var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,s);go.name=p.Name;
            go.transform.SetParent(parent,false);go.transform.position=level.Center(p.Cell);var item=go.GetComponent<Pickup>();item.BindViewer(camera);Undo.AddComponent<EntityId>(go);Dirty(go.transform);Dirty(item.Visual.GetComponent<Billboard>());return item;
        }
        private static void ConfigureMission(Scene s)
        {
            var flow=Find<MenuFlow>(s).Single();var hud=flow.Hud;var world=hud.World;var source=world.Source;var nav=Find<EnemyNavigation>(s).Single();var level=Require<LevelDefinition>(UkubolaLevelSetup.Data);
            MvpLayout.Check(level);var oldEnemies=source.Enemies.ToArray();var oldItems=source.Pickups.ToArray();var parent=oldItems[0].transform.parent;
            foreach(var enemy in oldEnemies)Object.DestroyImmediate(enemy.gameObject);
            foreach(var item in oldItems)Object.DestroyImmediate(item.gameObject);
            var actors=MvpLayout.Enemies.Select(p=>AddEnemy(s,p,level,source,nav)).ToArray();
            var items=MvpLayout.Items.Select(p=>AddPickup(s,p,level,parent,source.Weapons.AimCamera)).ToArray();
            source.Configure(items,actors,source.Player,source.Weapons,source.Noise,source.Spawn,source.Readout);Dirty(source);
            var context=Find<LevelContext>(s).Single();var serialized=new SerializedObject(context);var id=serialized.FindProperty("levelId");if(id==null)throw new InvalidOperationException("Missing LevelContext.levelId");id.stringValue=LevelId;serialized.ApplyModifiedPropertiesWithoutUndo();
            flow.ConfigureRoute(Title,Mission,Label);Dirty(flow);
            var exit=Find<LevelExit>(s).Single();var remote=world.Doors.Single(d=>d.SwitchOnly);var label=MakeObjectiveLabel(Find<RetroDisplay>(s).Single().Image.transform,flow.Typeface);
            exit.gameObject.AddComponent<MissionObjective>().Configure(source.Player.GetComponent<PlayerInventory>(),source.Player,remote,exit.Stats,label);
            Save(s);
        }
        [MenuItem("Tools/UKUBOLA/Phase 11/Setup MVP",false,110)]
        public static void Setup()
        {
            if(!CanRun())return;bool created=false;
            try
            {
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                if(Exists(Title)||Exists(Mission))
                {
                    if(!Exists(Title)||!Exists(Mission))throw new InvalidOperationException("Partial MVP: see README recovery.");
                    UkubolaMvpValidator.CheckMission(EditorSceneManager.OpenScene(Mission,OpenSceneMode.Single));UkubolaMvpValidator.CheckTitle(EditorSceneManager.OpenScene(Title,OpenSceneMode.Single));Register();Debug.Log("[UKUBOLA P11] Existing MVP preserved. VALIDATION PASSED | 0 errors.");return;
                }
                UkubolaMenuValidator.CheckGame(EditorSceneManager.OpenScene(UkubolaMenuSetup.Lab,OpenSceneMode.Single));UkubolaMenuValidator.CheckTitle(EditorSceneManager.OpenScene(UkubolaMenuSetup.Title,OpenSceneMode.Single));
                MvpLayout.Check(Require<LevelDefinition>(UkubolaLevelSetup.Data));
                if(!AssetDatabase.IsValidFolder(Root+"/Scenes/Levels"))AssetDatabase.CreateFolder(Root+"/Scenes","Levels");
                AssetDatabase.SaveAssets();Copy(UkubolaMenuSetup.Lab,Mission);created=true;Copy(UkubolaMenuSetup.Title,Title);
                ConfigureMission(EditorSceneManager.OpenScene(Mission,OpenSceneMode.Single));
                var title=EditorSceneManager.OpenScene(Title,OpenSceneMode.Single);var flow=Find<MenuFlow>(title).Single();flow.ConfigureRoute(Title,Mission,Label);Dirty(flow);Save(title);
                UkubolaMvpValidator.CheckMission(EditorSceneManager.OpenScene(Mission,OpenSceneMode.Single));UkubolaMvpValidator.CheckTitle(EditorSceneManager.OpenScene(Title,OpenSceneMode.Single));Register();
                Debug.Log("[UKUBOLA P11] MVP generated. Previous labs preserved.");Debug.Log("[UKUBOLA P11] VALIDATION PASSED | 0 errors.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P11] Setup stopped: "+ex+(created?"\nMVP may be incomplete. See README recovery.":""));}
        }
        private static void Open(string path){if(!CanRun())return;try{Require<SceneAsset>(path);if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(path,OpenSceneMode.Single);}catch(Exception ex){Debug.LogError("[UKUBOLA P11] "+ex.Message);}}
        [MenuItem("Tools/UKUBOLA/Phase 11/Open MVP Title",false,111)]public static void OpenTitle()=>Open(Title);
        [MenuItem("Tools/UKUBOLA/Phase 11/Open MVP Mission",false,112)]public static void OpenMission()=>Open(Mission);
    }
}
