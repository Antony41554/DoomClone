using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Ukubola.World;
using Ukubola.Player;
using Ukubola.Combat;
using Ukubola.Rendering;
using Ukubola.Enemies;
using Setup=Ukubola.EditorTools.UkubolaWorldSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaWorldValidator
    {
        private static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        [MenuItem("Tools/UKUBOLA/Phase 08/Validate World Lab",false,92)]
        public static void Validate()
        {if(!Setup.CanRun())return;try{if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;CheckScene(Setup.OpenInternal());Debug.Log("[UKUBOLA P08] VALIDATION PASSED | 0 errors. Play checks still required.");}catch(Exception ex){Debug.LogError("[UKUBOLA P08] VALIDATION FAILED: "+ex.Message);}}
        internal static void CheckScene(Scene scene)
        {
            Check(scene.IsValid()&&scene.isLoaded&&scene.path==Setup.Lab,"Incorrect WorldLab scene.");
            var diag=Setup.Find<WorldDiagnostics>(scene).Single();var old=Setup.Find<PickupDiagnostics>(scene).Single();var player=old.Player;var weapons=old.Weapons;var display=Setup.Find<RetroDisplay>(scene).Single();
            Check(diag.Source==old&&old.Readout&&old.Readout.font&&player&&weapons&&diag.Interactor&&diag.Interactor.gameObject==player.gameObject,"Invalid diagnostics or player references.");
            Check(player.GetComponent<PlayerInventory>()&&player.GetComponent<PlayerLife>()&&player.GetComponent<PlayerMotor>()&&player.GetComponent<CharacterController>()&&player.GetComponent<PlayerInputReader>(),"Missing player components.");
            var actions=player.GetComponent<PlayerInputReader>().Actions;Check(actions&&actions.FindActionMap("Gameplay")?.FindAction("Use")!=null,"Missing Gameplay/Use input action.");
            UkubolaCombatValidator.CheckDefinition(weapons.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(weapons.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
            Check(display.WorldCamera==weapons.AimCamera&&display.Target&&display.WorldCamera.targetTexture==display.Target&&display.Image&&display.Image.texture==display.Target,"Invalid retro display.");
            Check(Setup.Find<Camera>(scene).Count(c=>c.isActiveAndEnabled)==2&&Setup.Find<AudioListener>(scene).Count(c=>c.isActiveAndEnabled)==1,"Incorrect cameras or audio listeners.");
            Check(old.Pickups!=null&&old.Pickups.Length==16&&old.Pickups.All(p=>p&&p.Definition&&p.Definition.IsValid)&&Setup.Find<Pickup>(scene).Length==16,"Pickup system missing.");
            Check(old.Enemies!=null&&old.Enemies.Length==2&&old.Enemies.All(e=>e&&e.Target==player&&e.Navigation&&e.Navigation.Data)&&old.Noise&&old.Spawn,"Enemy or navigation references missing.");
            int layer=LayerMask.NameToLayer("Interactable"),pl=LayerMask.NameToLayer("Player"),enemy=LayerMask.NameToLayer("Enemy"),world=LayerMask.NameToLayer("World");
            Check(layer>=0&&pl>=0&&enemy>=0&&world>=0&&!Physics.GetIgnoreLayerCollision(layer,pl)&&!Physics.GetIgnoreLayerCollision(enemy,pl)&&!Physics.GetIgnoreLayerCollision(world,pl),"Collision layers invalid.");
            Check((weapons.HitMask&(1<<layer))!=0&&(weapons.HitMask&(1<<world))!=0&&(weapons.HitMask&(1<<enemy))!=0&&(weapons.TargetMask&(1<<enemy))!=0&&(display.WorldCamera.cullingMask&(1<<layer))!=0,"Invalid combat or camera masks.");
            var doors=Setup.Find<Door>(scene);Check(doors.Length==4&&diag.Doors!=null&&diag.Doors.Length==4&&doors.All(d=>diag.Doors.Contains(d)),"Door count/references invalid.");
            foreach(var d in doors)
            {
                var obstacle=d.GetComponent<NavMeshObstacle>();Check(d.isActiveAndEnabled&&d.Panel&&d.Panel.GetComponent<BoxCollider>()&&d.Panel.GetComponent<BoxCollider>().enabled&&!d.Panel.GetComponent<BoxCollider>().isTrigger&&d.Panel.gameObject.layer==layer&&d.Panel.GetComponent<Renderer>()&&d.Panel.GetComponent<Renderer>().sharedMaterial&&obstacle&&obstacle.enabled&&obstacle.carving&&d.GetComponent<EntityId>(),"Invalid door: "+d.name);
                Check(Mathf.Abs(d.Panel.localScale.y-3.2f)<.01f&&Mathf.Abs(d.Panel.localPosition.y-1.6f)<.01f,"Door must start fully closed: "+d.name);
            }
            Check(doors.Single(d=>d.name=="Blue Door").Required==(KeyId.BlueCard|KeyId.BlueSkull)&&doors.Single(d=>d.name=="Remote Door").SwitchOnly,"Invalid access rules.");
            var sw=Setup.Find<WorldSwitch>(scene);Check(sw.Length==2&&diag.Switches!=null&&diag.Switches.Length==2&&sw.All(s=>diag.Switches.Contains(s)&&s.Indicator&&s.Indicator.GetComponent<BoxCollider>()&&s.Indicator.gameObject.layer==layer&&s.GetComponent<EntityId>()),"Switches invalid.");
            Check(sw.Single(s=>s.name=="Door Switch").Door==doors.Single(d=>d.name=="Remote Door")&&sw.Single(s=>s.name=="Lift Switch").Lift==diag.Lift,"Switch links invalid.");
            Check(diag.Lift==Setup.Find<Lift>(scene).Single()&&diag.Lift.Rider==player.GetComponent<CharacterController>()&&diag.Lift.Platform&&diag.Lift.Platform.GetComponent<BoxCollider>()&&diag.Lift.GetComponent<EntityId>(),"Lift invalid.");
            Check(diag.Zone==Setup.Find<DamageZone>(scene).Single()&&diag.Zone.Damage==10&&diag.Zone.Interval==1f,"Damage zone invalid.");
            Check(diag.Teleport==Setup.Find<Teleport>(scene).Single()&&diag.Teleport.Destination&&Vector3.Distance(diag.Teleport.transform.position,diag.Teleport.Destination.position)>2,"Teleport invalid.");
            Check(diag.Secret==Setup.Find<SecretTrigger>(scene).Single(),"Secret invalid.");
            foreach(var go in new[]{diag.Zone.gameObject,diag.Teleport.gameObject,diag.Secret.gameObject}){var c=go.GetComponent<BoxCollider>();var rb=go.GetComponent<Rigidbody>();Check(go.layer==layer&&go.GetComponent<EntityId>()&&c&&c.enabled&&c.isTrigger&&rb&&rb.isKinematic&&!rb.useGravity,"Invalid trigger: "+go.name);}
            var barrels=Setup.Find<ExplosiveBarrel>(scene);Check(barrels.Length==2&&diag.Barrels!=null&&diag.Barrels.Length==2&&barrels.All(b=>diag.Barrels.Contains(b)&&b.GetComponent<Health>()&&b.GetComponent<Health>().MaxHealth==20&&b.GetComponent<BoxCollider>()&&b.GetComponent<MeshRenderer>()&&b.GetComponent<EntityId>()&&b.gameObject.layer==enemy&&b.Mask==weapons.HitMask),"Invalid barrels.");
            var secretWall=scene.GetRootGameObjects().Single(g=>g.name=="Phase08 World").transform.Find("Secret South Wall");Check(secretWall&&secretWall.GetComponentInChildren<BoxCollider>()&&secretWall.GetComponent<NavMeshObstacle>()&&secretWall.GetComponent<NavMeshObstacle>().carving,"Secret room wall missing.");
            var context=Setup.Find<LevelContext>(scene).Single();Check(context.LevelId==Setup.LevelId,"Incorrect LevelContext ID.");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
            Check(Setup.Find<GameSession>(scene).Length==1&&Setup.Find<LevelExit>(scene).Single().Stats==Setup.Find<LevelStats>(scene).Single(),"Level flow missing.");
            foreach(var t in Setup.Find<Transform>(scene))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
        }
    }
}
