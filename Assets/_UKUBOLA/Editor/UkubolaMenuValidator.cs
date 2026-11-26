using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Ukubola.UI;
using Ukubola.Audio;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
using Ukubola.Rendering;
using Setup=Ukubola.EditorTools.UkubolaMenuSetup;

namespace Ukubola.EditorTools
{
    public static class UkubolaMenuValidator
    {
        private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        internal static void CheckBuildList()
        {
            var list=EditorBuildSettings.scenes.Where(s=>s.enabled).ToArray();
            Check(list.Length>=2&&list[0].path==Setup.Title&&list[1].path==Setup.Lab,"Active build scene list must begin with MainMenu and MenuLab.");
            Check(list.Count(s=>s.path==Setup.Title)==1&&list.Count(s=>s.path==Setup.Lab)==1,"Duplicate menu scene entries.");
        }
        private static MenuFlow Common(Scene scene,string path,bool title,int cameras)
        {
            Check(scene.IsValid()&&scene.isLoaded&&scene.path==path,"Wrong or unloaded scene: "+path);
            var flow=Setup.Find<MenuFlow>(scene).Single();Check(flow&&flow.isActiveAndEnabled&&flow.TitleScene==title&&flow.Typeface,"Invalid menu controller/font.");
            Check(Setup.Find<Camera>(scene).Count(c=>c.isActiveAndEnabled)==cameras&&Setup.Find<AudioListener>(scene).Count(a=>a.isActiveAndEnabled)==1,"Wrong active camera/listener count.");
            Check(Setup.Find<EventSystem>(scene).Length==0,"MenuFlow creates its own EventSystem at runtime; remove duplicates.");
            foreach(var t in Setup.Find<Transform>(scene))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
            return flow;
        }
        internal static void CheckTitle(Scene scene)
        {
            var flow=Common(scene,Setup.Title,true,1);Check(!flow.Hud&&flow.TitleMusic&&flow.TitleMusic.clip&&flow.TitleMusic.loop&&!flow.TitleMusic.playOnAwake&&flow.TitleMusic.spatialBlend==0,"Invalid title music/flow.");
            var cam=Setup.Find<Camera>(scene).Single();Check(cam.CompareTag("MainCamera")&&cam.cullingMask==0&&cam.targetTexture==null,"Invalid title camera.");
            Check(Setup.Find<PlayerMotor>(scene).Length==0,"Title must not contain a player.");
        }
        internal static void CheckGame(Scene scene)
        {
            var flow=Common(scene,Setup.Lab,false,2);var hud=Setup.Find<ClassicHud>(scene).Single();var audio=Setup.Find<GameAudio>(scene).Single();var world=Setup.Find<WorldDiagnostics>(scene).Single();var stats=Setup.Find<LevelStats>(scene).Single();var display=Setup.Find<RetroDisplay>(scene).Single();
            Check(flow.Hud==hud&&!flow.TitleMusic&&hud.Audio==audio&&hud.World==world&&hud.Stats==stats&&audio.World==world&&audio.Stats==stats,"Broken gameplay menu links.");
            var source=world.Source;Check(source&&source.Player&&source.Weapons&&source.Player.GetComponent<PlayerMotor>()&&source.Player.GetComponent<PlayerInputReader>()&&source.Player.GetComponent<PlayerInventory>(),"Missing gameplay controls.");
            Check(!source.enabled&&!world.enabled&&!hud.DiagnosticsEnabled&&!audio.DiagnosticsEnabled&&!source.Readout.transform.parent.gameObject.activeSelf,"Debug input must be disabled in MenuLab.");
            Check(source.Enemies!=null&&source.Enemies.Length==2&&source.Enemies.All(e=>e&&e.GetComponent<NavMeshAgent>()&&e.Target==source.Player)&&source.Pickups!=null&&source.Pickups.Length==16&&source.Pickups.All(p=>p&&p.Definition&&p.Definition.IsValid)&&world.Secret,"Missing enemies, pickups or secret.");
            Check(display.WorldCamera==source.Weapons.AimCamera&&display.Target&&display.Target.width==320&&display.Target.height==200&&display.Image&&display.Image.texture==display.Target&&display.WorldCamera.targetTexture==display.Target,"Retro display invalid.");
            Check(hud.Labels.Length==9&&hud.Labels.All(t=>t&&t.font)&&hud.Faces.Length==HudRules.FaceCount&&hud.Faces.All(t=>t)&&audio.Music&&audio.Music.clip&&audio.Local&&audio.Spatial.Length==8&&audio.Spatial.All(a=>a),"HUD/audio missing references.");
            UkubolaCombatValidator.CheckDefinition(source.Weapons.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(source.Weapons.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
            Check(Setup.Find<LevelExit>(scene).Single().Stats==stats,"Broken level exit.");var context=Setup.Find<LevelContext>(scene).Single();Check(context.LevelId==Setup.LevelId,"Wrong level ID.");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
        }
        [MenuItem("Tools/UKUBOLA/Phase 10/Validate Menus",false,103)]
        public static void Validate()
        {
            if(!Setup.CanRun())return;try{if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;CheckBuildList();CheckGame(EditorSceneManager.OpenScene(Setup.Lab,OpenSceneMode.Single));CheckTitle(EditorSceneManager.OpenScene(Setup.Title,OpenSceneMode.Single));Debug.Log("[UKUBOLA P10] VALIDATION PASSED | 0 errors. Play checks still required.");}catch(Exception ex){Debug.LogError("[UKUBOLA P10] VALIDATION FAILED: "+ex);}
        }
    }
}
