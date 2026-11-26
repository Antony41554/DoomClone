using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
using Ukubola.Enemies;
using Ukubola.Rendering;
using Object=UnityEngine.Object;
namespace Ukubola.EditorTools {
 public static class UkubolaAdvancedTests {
  [MenuItem("Tools/UKUBOLA/Phase 12/Test Infighting (Play Mode)",false,126)]public static void TestInfighting(){
   if(!EditorApplication.isPlaying){Debug.LogWarning("[UKUBOLA P12] Start Advanced mission in Play first.");return;}
   try{var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=UkubolaAdvancedSetup.Mission||Time.timeScale<=0)throw new InvalidOperationException("Advanced mission must be running and unpaused.");
    var actors=UkubolaAdvancedSetup.Find<EnemyBrain>(scene).Where(x=>x.GetComponent<Health>().Current>1&&x.isActiveAndEnabled).ToArray();
    var victim=actors.FirstOrDefault(a=>actors.Any(b=>a!=b&&EnemyFaction.Hostile(a.GetComponent<EnemyFaction>().Faction,b.GetComponent<EnemyFaction>().Faction)));
    if(!victim)throw new InvalidOperationException("At least two living hostile factions required.");
    var attacker=actors.First(b=>victim!=b&&EnemyFaction.Hostile(victim.GetComponent<EnemyFaction>().Faction,b.GetComponent<EnemyFaction>().Faction));
    var hp=victim.GetComponent<Health>();var result=hp.TakeDamage(new DamageInfo(1,attacker.gameObject,DamageType.Projectile,victim.transform.position));
    if(!result.Applied||victim.CombatTarget!=attacker.GetComponent<Health>()||victim.State!=EnemyState.Pain)throw new InvalidOperationException("Retaliation did not select the attacker.");
    Debug.Log("[UKUBOLA P12] INFIGHTING TEST PASSED | "+victim.name+" targets "+attacker.name+". Restart level to clear this test.");
   }catch(Exception ex){Debug.LogError("[UKUBOLA P12] INFIGHTING TEST FAILED: "+ex.Message);}}
  [MenuItem("Tools/UKUBOLA/Phase 12/Run Advanced Self-Tests",false,124)]public static void Run(){if(!UkubolaMvpSetup.CanRun())return;int n=0;var cleanup=new List<Object>();void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);n++;}
   try{
    for(int i=0;i<=500;i++){float distance=i*.01f;int damage=Explosion.Damage(distance,5,100);Check(damage>=0&&damage<=100,"Blast bounds");if(i>0)Check(damage<=Explosion.Damage((i-1)*.01f,5,100),"Blast monotonic");}
    Check(Explosion.Damage(0,5,100)==100&&Explosion.Damage(5,5,100)==0&&Explosion.Damage(1,0,100)==0,"Blast edges");
    for(int a=0;a<=4;a++)for(int b=0;b<=4;b++)Check(EnemyFaction.Hostile(a,b)==(a>0&&b>0&&a!=b),"Faction rule");
    for(int i=0;i<100;i++){Check(Powerup.Extend(i,i+20,5)==i+20,"No timer shortening");Check(Powerup.Extend(i,i+5,30)==i+30,"Timer extension");}
    for(int i=0;i<8;i++){Check(DirectionalSprite.IndexFromAngle(i*45)==i,"Direction center");Check(DirectionalSprite.IndexFromAngle(i*45-360)==i,"Direction wrap");}
    Check(SectorLighting.Brightness(new Vector3(5,0,5))==1&&SectorLighting.Brightness(new Vector3(21,0,5))==.76f&&SectorLighting.Brightness(new Vector3(5,0,21))==.55f,"Sector light");
    var go=EditorUtility.CreateGameObjectWithHideFlags("Advanced test actor",HideFlags.HideAndDontSave,typeof(Health),typeof(PlayerInventory),typeof(Powerup));cleanup.Add(go);var hp=go.GetComponent<Health>();var bag=go.GetComponent<PlayerInventory>();hp.Configure(100,null);
    var power=go.GetComponent<Powerup>();Check(power.Grant(PowerupKind.Invulnerability,30)&&power.Invulnerable,"Grant invulnerability");Check(!hp.TakeDamage(new DamageInfo(100)).Applied&&hp.Current==100,"Invulnerability blocks damage");power.Clear();hp.TakeDamage(new DamageInfo(20));Check(hp.Current==80,"Damage restored");
    var def=ScriptableObject.CreateInstance<PickupDefinition>();cleanup.Add(def);def.Configure("Berserk",PickupKind.Powerup,null,60);def.ConfigurePowerup(PowerupKind.Berserk);Check(PickupEffect.TryApply(def,hp,bag)&&power.Berserk&&hp.Current==100,"Berserk effect and heal");hp.ResetState();Check(!power.Berserk&&!power.Invulnerable,"Reset powerups");
    foreach(var id in UkubolaAdvancedSetup.Ids.Where(x=>x!=WeaponId.Fist)){AmmoType ammo=id==WeaponId.RocketLauncher?AmmoType.Rockets:id==WeaponId.PlasmaRifle||id==WeaponId.EnergyCannon?AmmoType.Cells:AmmoType.Bullets;int amount=id==WeaponId.Chainsaw?0:40;def.Configure(id.ToString(),PickupKind.Weapon,null,amount,ammo,weaponType:id);Check(def.IsRuleValid&&PickupEffect.TryApply(def,hp,bag)&&bag.HasWeapon(id),"Unlock "+id);}
    var level=UkubolaAdvancedSetup.Require<LevelDefinition>(UkubolaLevelSetup.Data);var used=new HashSet<Vector2Int>(MvpLayout.Enemies.Concat(MvpLayout.Items).Select(p=>p.Cell));foreach(var c in UkubolaAdvancedSetup.Cells.Concat(UkubolaAdvancedSetup.EnemyCells))Check(LevelDefinition.Walkable(level.Tile(c.x,c.y))&&used.Add(c),"Advanced layout "+c);
    foreach(var id in UkubolaAdvancedSetup.Ids){var d=UkubolaAdvancedSetup.Require<WeaponDefinition>(UkubolaAdvancedSetup.Data+"/"+id+".asset");Check(d.IsValid&&d.Weapon==id,"Weapon asset "+id);Check(d.UsesAmmo?d.AmmoCost>0:d.AmmoCost==0,"Ammo cost "+id);}
    Debug.Log("[UKUBOLA P12] ADVANCED SELF-TESTS PASSED | "+n+" checks.");
   }catch(Exception e){Debug.LogError("[UKUBOLA P12] SELF-TEST FAILED after "+n+" checks: "+e);}finally{for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i])Object.DestroyImmediate(cleanup[i]);}}
 }
}
