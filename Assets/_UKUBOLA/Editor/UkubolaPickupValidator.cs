using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Enemies;
using Ukubola.Rendering;
using Ukubola.World;
using Setup=Ukubola.EditorTools.UkubolaPickupSetup;
namespace Ukubola.EditorTools
{
    public static class UkubolaPickupValidator
    {
        private static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
        [MenuItem("Tools/UKUBOLA/Phase 07/Validate Pickup Lab",false,82)]
        public static void Validate()
        {
            if(!Setup.CanRun())return;
            try{if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;CheckScene(Setup.OpenInternal());Debug.Log("[UKUBOLA P07] VALIDATION PASSED | 0 errors. Play checks still required.");}
            catch(Exception ex){Debug.LogError("[UKUBOLA P07] VALIDATION FAILED: "+ex.Message);}
        }
        internal static void CheckScene(Scene scene)
        {
            Check(scene.IsValid()&&scene.isLoaded&&scene.path==Setup.Lab,"Incorrect PickupLab scene.");
            var weapons=Setup.Find<WeaponController>(scene).Single();var hp=weapons.GetComponent<Health>();var display=Setup.Find<RetroDisplay>(scene).Single();var diag=Setup.Find<PickupDiagnostics>(scene).Single();var items=Setup.Find<Pickup>(scene);var actors=Setup.Find<EnemyBrain>(scene);
            Check(hp&&hp.Armor&&weapons.GetComponent<PlayerInventory>()&&weapons.GetComponent<PlayerMotor>()&&weapons.GetComponent<PlayerLife>()&&weapons.GetComponent<CharacterController>(),"Player components missing.");
            UkubolaCombatValidator.CheckDefinition(weapons.Pistol,WeaponId.Pistol,AmmoType.Bullets,1);UkubolaCombatValidator.CheckDefinition(weapons.Shotgun,WeaponId.Shotgun,AmmoType.Shells,7);
            Check(display.WorldCamera&&display.WorldCamera==weapons.AimCamera&&display.Target&&display.Target.width==320&&display.Target.height==200&&display.WorldCamera.targetTexture==display.Target&&display.Image&&display.Image.texture==display.Target,"Retro display invalid.");
            Check(Setup.Find<Camera>(scene).Count(c=>c.isActiveAndEnabled)==2&&Setup.Find<AudioListener>(scene).Count(a=>a.isActiveAndEnabled)==1,"Unexpected cameras or listeners.");
            Check(diag.Player==hp&&diag.Weapons==weapons&&diag.Spawn&&diag.Readout&&diag.Readout.font&&diag.Readout.transform.IsChildOf(display.Image.transform)&&diag.Noise==Setup.Find<NoiseEmitter>(scene).Single(),"Diagnostic references invalid.");
            var nav=Setup.Find<EnemyNavigation>(scene).Single();Check(nav.Data&&nav.isActiveAndEnabled&&diag.Noise.Weapons==weapons,"Navigation or noise missing.");
            Check(actors.Length==2&&diag.Enemies!=null&&diag.Enemies.Length==2&&actors.All(e=>diag.Enemies.Contains(e)&&e.Target==hp&&e.Viewer==display.WorldCamera&&e.Navigation==nav&&e.Noise==diag.Noise),"Enemy references invalid.");
            Check(Setup.Find<EnemyDiagnostics>(scene).Length==0&&Setup.Find<CombatDiagnostics>(scene).Length==0,"Old diagnostics still active.");
            Check(items.Length==Setup.Specs.Length&&diag.Pickups!=null&&diag.Pickups.Length==items.Length&&items.All(i=>diag.Pickups.Contains(i)),"Pickup count or diagnostics references invalid.");
            int layer=LayerMask.NameToLayer("Pickup"),playerLayer=LayerMask.NameToLayer("Player");
            Check(layer>=0&&playerLayer>=0&&!Physics.GetIgnoreLayerCollision(layer,playerLayer)&&(display.WorldCamera.cullingMask&(1<<layer))!=0&&hp.gameObject.layer==playerLayer,"Pickup layer, collision matrix or camera mask invalid.");
            var level=Setup.Require<LevelDefinition>(UkubolaLevelSetup.Data);
            Check(Setup.Specs.Select(s=>s.Cell).Distinct().Count()==Setup.Specs.Length,"Duplicate pickup cells.");
            foreach(var spec in Setup.Specs)
            {
                var item=items.Single(i=>i.name==spec.Name);var def=item.Definition;var trigger=item.GetComponent<SphereCollider>();var body=item.GetComponent<Rigidbody>();
                string prefix=spec.Name+": ";
                Check(def&&def.IsValid&&EditorUtility.IsPersistent(def)&&def.DisplayName==spec.Name&&def.Kind==spec.Kind&&def.Amount==spec.Amount&&def.Ammo==spec.Ammo&&def.Armor==spec.Armor&&def.Weapon==spec.Weapon&&def.Key==spec.Key,prefix+"definition fields invalid.");
                Check(item.isActiveAndEnabled&&item.gameObject.layer==layer&&trigger&&trigger.enabled&&trigger.isTrigger&&trigger.radius>0&&body&&body.isKinematic&&!body.useGravity,prefix+"invalid trigger or rigidbody.");
                Check(item.Visual&&item.Visual.enabled&&item.Visual.sprite==def.Sprite&&item.Visual.sharedMaterial&&item.Visual.sharedMaterial.shader.isSupported&&item.Visual.sprite.texture.filterMode==FilterMode.Point&&item.Visual.GetComponent<Billboard>()&&item.Visual.GetComponent<Billboard>().Viewer==display.WorldCamera,prefix+"invalid sprite or billboard.");
                Check(LevelDefinition.Walkable(level.Tile(spec.Cell.x,spec.Cell.y))&&Vector3.Distance(item.transform.position,level.Center(spec.Cell))<.01f,prefix+"wrong cell position.");
                Check(item.GetComponent<EntityId>()&&PrefabUtility.IsPartOfPrefabInstance(item),prefix+"missing instance ID or prefab.");
                Check(item.GetComponentsInChildren<Collider>(true).Length==1,prefix+"unexpected solid collider.");
            }
            var context=Setup.Find<LevelContext>(scene).Single();Check(context.LevelId==Setup.LevelId,"Wrong LevelContext ID.");var errors=context.RebuildRegistry();Check(errors.Count==0,string.Join("\n",errors));
            Check(Setup.Find<GameSession>(scene).Length==1&&Setup.Find<LevelExit>(scene).Single().Stats==Setup.Find<LevelStats>(scene).Single(),"Level flow missing.");
            foreach(var t in Setup.Find<Transform>(scene))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
        }
    }
}
