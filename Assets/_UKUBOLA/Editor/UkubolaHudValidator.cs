using System;
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
using Ukubola.Player;
using Ukubola.Combat;
using Setup=Ukubola.EditorTools.UkubolaHudSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaHudValidator
    {
        private static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        [MenuItem("Tools/UKUBOLA/Phase 09/Validate HUD Lab",false,102)]
        public static void Validate(){if(!Setup.CanRun())return;try{if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;CheckScene(Setup.OpenInternal());Debug.Log("[UKUBOLA P09] VALIDATION PASSED | 0 errors. Play checks still required.");}catch(Exception ex){Debug.LogError("[UKUBOLA P09] VALIDATION FAILED: "+ex.Message);}}
        private static void Texture(Texture2D t,int w,int h,string label){Check(t&&t.width==w&&t.height==h&&t.filterMode==FilterMode.Point&&t.mipmapCount==1&&t.wrapMode==TextureWrapMode.Clamp&&EditorUtility.IsPersistent(t),"Invalid texture: "+label);}
        private static void Clip(AudioClip clip,string name){Check(clip&&clip.name==name&&clip.channels==1&&clip.samples>0&&EditorUtility.IsPersistent(clip),"Missing or invalid audio: "+name);}
        internal static void CheckScene(Scene scene)
        {
            Check(scene.IsValid()&&scene.isLoaded&&scene.path==Setup.Lab,"Incorrect HudLab scene.");
            var hud=Setup.Find<ClassicHud>(scene).Single();var audio=Setup.Find<GameAudio>(scene).Single();var world=Setup.Find<WorldDiagnostics>(scene).Single();var stats=Setup.Find<LevelStats>(scene).Single();var display=Setup.Find<RetroDisplay>(scene).Single();
            Check(hud.World==world&&hud.Stats==stats&&hud.Audio==audio&&audio.World==world&&audio.Stats==stats,"Broken HUD/audio links.");
            var old=world.Source;Check(old&&old.Player&&old.Player.Armor&&old.Player.GetComponent<PlayerInventory>()&&old.Weapons&&old.Readout&&old.Readout.font,"Missing player or diagnostics.");
            Check(old.Pickups!=null&&old.Pickups.Length==16&&old.Pickups.All(p=>p&&p.Definition&&p.Definition.IsValid)&&old.Enemies!=null&&old.Enemies.Length==2&&old.Enemies.All(e=>e&&e.Definition&&e.Definition.IsValid&&e.Target==old.Player),"Broken pickup/enemy references.");
            Check(world.Doors!=null&&world.Doors.Length==4&&world.Doors.All(d=>d&&d.Panel)&&world.Switches!=null&&world.Switches.Length==2&&world.Switches.All(s=>s&&s.Indicator)&&world.Lift&&world.Zone&&world.Teleport&&world.Teleport.Destination&&world.Secret&&world.Interactor&&world.Barrels!=null&&world.Barrels.Length==2&&world.Barrels.All(b=>b),"Broken world references.");
            UkubolaCombatValidator.CheckDefinition(old.Weapons.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(old.Weapons.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
            Check(display.WorldCamera==old.Weapons.AimCamera&&display.Image&&display.Target&&display.Target.width==320&&display.Target.height==200&&display.Image.texture==display.Target&&display.WorldCamera.targetTexture==display.Target,"Retro display invalid.");
            Check(Setup.Find<Camera>(scene).Count(c=>c.isActiveAndEnabled)==2&&Setup.Find<AudioListener>(scene).Count(c=>c.isActiveAndEnabled)==1,"Incorrect cameras or listeners.");
            var bar=hud.GetComponent<RawImage>();Check(bar&&bar.transform.parent==display.Image.transform&&bar.rectTransform.anchorMin==Vector2.zero&&bar.rectTransform.anchorMax==new Vector2(1,.2f),"Status bar layout invalid.");Texture(bar.texture as Texture2D,320,40,"StatusBar");
            Check(hud.Faces!=null&&hud.Faces.Length==HudRules.FaceCount&&hud.Face&&hud.Face.transform.IsChildOf(bar.transform)&&hud.Face.texture==hud.Faces[0],"Face configuration invalid.");for(int i=0;i<hud.Faces.Length;i++)Texture(hud.Faces[i],32,32,"Face "+i);
            Check(hud.Labels.Length==9&&hud.Labels.Distinct().Count()==9&&hud.Labels.All(t=>t&&t.font&&!t.raycastTarget&&t.transform.IsChildOf(display.Image.transform)),"HUD labels invalid.");
            Check(hud.Flash&&!hud.Flash.raycastTarget&&hud.Flash.transform.parent==display.Image.transform,"Damage flash invalid.");
            var viewport=old.Weapons.View.Image.rectTransform.parent as RectTransform;Check(viewport&&viewport.anchorMin==new Vector2(0,.2f)&&viewport.anchorMax==Vector2.one,"Weapon viewport not above HUD.");
            Check(!old.Readout.transform.parent.gameObject.activeSelf&&old.isActiveAndEnabled&&world.isActiveAndEnabled,"Diagnostics must be hidden, not disabled.");
            Check(audio.Music&&audio.Local&&audio.Music!=audio.Local&&audio.Music.loop&&!audio.Music.playOnAwake&&audio.Music.spatialBlend==0&&audio.Local.spatialBlend==0&&!audio.Local.playOnAwake,"Audio sources invalid.");Clip(audio.Music.clip,"MusicLoop");
            Check(audio.Spatial!=null&&audio.Spatial.Length==8&&audio.Spatial.Distinct().Count()==8&&audio.Spatial.All(a=>a&&a.spatialBlend==1&&!a.playOnAwake&&!a.loop&&a!=audio.Local&&a!=audio.Music),"Spatial voices invalid.");
            string[] names=Enum.GetNames(typeof(Cue));Check(audio.Clips!=null&&audio.Clips.Length==names.Length,"Effect clip array invalid.");for(int i=0;i<names.Length;i++)Clip(audio.Clips[i],names[i]);
            Check(Setup.Find<LevelExit>(scene).Single().Stats==stats&&Setup.Find<GameSession>(scene).Length==1,"Level flow invalid.");
            var context=Setup.Find<LevelContext>(scene).Single();Check(context.LevelId==Setup.LevelId,"Incorrect LevelContext ID.");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
            foreach(var t in Setup.Find<Transform>(scene))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
        }
    }
}
