using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Persistence;
using Ukubola.UI;
using Ukubola.World;
namespace Ukubola.EditorTools
{
    public static class UkubolaPersistenceTests
    {
        private static SaveData Fixture(int stage=0)
        {
            return new SaveData{version=SaveData.Version,stage=stage,layout=SaveStore.Hash("test layout"),utc=DateTime.UtcNow.ToString("O"),entry=SavedGear.From(Loadout.Fresh()),current=SavedGear.From(new Loadout(73,41,ArmorKind.Green,true,new[]{22,8,3,40},new[]{true,false,true,true,false,false,false,false},WeaponId.Shotgun)),prior=SavedTotals.From(default),keys=1,position=new Vector3(5,.03f,5),yaw=90,elapsed=120,invulnerability=10,berserk=30,liftFraction=.5f,liftRaised=true,secret=true,width=19,depth=15,explored=Enumerable.Range(0,285).Select(i=>i%3==0).ToArray(),
                enemies=Enumerable.Range(0,6+stage*2).Select(i=>new SavedEnemy{name="Enemy "+i,hp=i==0?0:20,max=20,position=new Vector3(5,0,7+i),lastKnown=Vector3.zero,yaw=90,cooldown=.5f,alerted=i>0,rival=-1}).ToArray(),
                pickups=Enumerable.Range(0,stage==0?25:24).Select(i=>new SavedItem{name="Pickup "+i,collected=i%2==0}).ToArray(),doors=Enumerable.Range(0,4).Select(i=>new SavedDoor{name="Door "+i,fraction=i/3f,opening=i%2==0,remaining=1.5f}).ToArray(),switches=Enumerable.Range(0,2).Select(i=>new SavedSwitch{name="Switch "+i,active=i==1}).ToArray(),barrels=Enumerable.Range(0,2).Select(i=>new SavedBarrel{name="Barrel "+i,hp=i==0?0:12,max=20}).ToArray()};
        }
        [MenuItem("Tools/UKUBOLA/Phase 14/Run Persistence Self-Tests",false,143)]public static void Run()
        {
            if(!UkubolaMvpSetup.CanRun())return;int checks=0;string folder=Path.Combine(Path.GetTempPath(),"UKUBOLA_P14_TEST_"+Guid.NewGuid().ToString("N"));string path=Path.Combine(folder,"test.json");GameObject hpObject=null;
            void Assert(bool pass,string name){if(!pass)throw new InvalidOperationException(name);checks++;}
            void Reject(SaveData data,string name){Assert(!data.Validate(out _),name);}
            void Throws(Action operation,string name){bool failed=false;try{operation();}catch{failed=true;}Assert(failed,name);}
            try
            {
                for(int i=0;i<3;i++){
                    var d=Fixture(i);Assert(d.Validate(out _),"Fixture valid");var round=SaveStore.Decode(SaveStore.Encode(d));
                    Assert(JsonUtility.ToJson(round)==JsonUtility.ToJson(d),"Full JSON round trip");Assert(round.Entry().Path==SaveCampaignPaths.Missions[i]&&round.Entry().Gear.Health==100&&round.current.hp==73,"Separate retry entry and current state");
                    Assert(round.enemies[0].hp==0&&round.pickups[0].collected&&round.barrels[0].hp==0&&round.secret&&round.explored[3],"World restoration DTO");
                }
                var rng=new System.Random(1401);
                for(int i=0;i<500;i++){
                    var d=Fixture(i%3);d.current.hp=rng.Next(1,101);d.current.armor=rng.Next(1,101);d.current.ammo=new[]{rng.Next(401),rng.Next(101),rng.Next(101),rng.Next(601)};d.yaw=(float)rng.NextDouble()*360;d.elapsed=(float)rng.NextDouble()*10000;
                    var round=SaveStore.Decode(SaveStore.Encode(d));Assert(round.current.hp==d.current.hp&&round.current.ammo.SequenceEqual(d.current.ammo)&&round.current.armor==d.current.armor,"Randomized gear round trip");
                    Assert(round.current.ToLoadout().Selected==WeaponId.Shotgun&&round.current.ToLoadout().Backpack,"Loadout reconstruction");
                }
                var x=Fixture();x.version=0;Reject(x,"Old version");x=Fixture();x.stage=3;Reject(x,"Invalid stage");x=Fixture();x.current.hp=0;Reject(x,"Dead player");x=Fixture();x.current.ammo[0]=401;Reject(x,"Invalid ammo");x=Fixture();x.current.selected=7;Reject(x,"Unowned weapon");x=Fixture();x.current.armor=101;Reject(x,"Armor exceeds capacity");x=Fixture();x.keys=64;Reject(x,"Unknown keys");x=Fixture();x.position.x=float.NaN;Reject(x,"Nonfinite position");x=Fixture();x.elapsed=float.PositiveInfinity;Reject(x,"Nonfinite time");x=Fixture();x.enemies[0].rival=0;Reject(x,"Self rival");x=Fixture();x.enemies[0].hp=21;Reject(x,"Invalid enemy health");x=Fixture();x.pickups[0].name=x.pickups[1].name;Reject(x,"Duplicate identity");x=Fixture();x.doors[0].fraction=1.1f;Reject(x,"Invalid door fraction");x=Fixture();x.explored=new bool[1];Reject(x,"Map mismatch");x=Fixture();x.invulnerability=31;Reject(x,"Invalid power duration");x=Fixture();x.prior.kills=1;Reject(x,"Invalid totals");
                Assert(!SaveStore.TryRead(path,out _,out _,out _),"Empty slot");var first=Fixture();SaveStore.Write(path,first);
                Assert(SaveStore.TryRead(path,out var loaded,out var backup,out _)&&!backup&&loaded.current.hp==73,"Primary save");
                var second=Fixture(1);second.current.hp=50;SaveStore.Write(path,second);
                Assert(SaveStore.TryRead(path,out loaded,out backup,out _)&&!backup&&loaded.current.hp==50&&loaded.stage==1,"Replacement primary");
                Assert(SaveStore.Decode(File.ReadAllText(path+".bak")).current.hp==73,"Backup previous revision");
                var encoded=File.ReadAllText(path);Throws(()=>SaveStore.Decode(encoded.Replace("\"sha256\":","\"broken\":")),"Checksum missing");Throws(()=>SaveStore.Decode("{broken"),"Malformed JSON");
                File.WriteAllText(path,"corrupt");Assert(SaveStore.TryRead(path,out loaded,out backup,out _)&&backup&&loaded.current.hp==73,"Corrupt primary recovery");
                SaveStore.Write(path,second);Assert(SaveStore.Decode(File.ReadAllText(path+".bak")).current.hp==73,"Good backup retained after corrupt primary replacement");
                x=Fixture();x.current.hp=0;Throws(()=>SaveStore.Write(path,x),"Reject invalid write");Assert(SaveStore.TryRead(path,out loaded,out backup,out _)&&!backup&&loaded.current.hp==50,"Rejected write preserved primary");
                Assert(Directory.GetFiles(folder,"*.tmp-*").Length==0,"No temporary file remains");File.WriteAllText(path,"bad");File.WriteAllText(path+".bak","bad");Assert(!SaveStore.TryRead(path,out _,out _,out _),"Both corrupted: unavailable");
                hpObject=new GameObject("Silent restore test"){hideFlags=HideFlags.HideAndDontSave};var hp=hpObject.AddComponent<Health>();hp.Configure(20,null);int death=0,damage=0;hp.Died+=(h,d)=>death++;hp.Damaged+=(h,d,r)=>damage++;hp.RestoreSavedHealth(0);Assert(hp.IsDead&&death==0&&damage==0,"Silent dead state");hp.RestoreSavedHealth(12);Assert(hp.Current==12&&!hp.IsDead&&death==0&&damage==0,"Silent living state");var stats=hpObject.AddComponent<LevelStats>();stats.RestoreElapsed(40);Assert(stats.ElapsedSeconds==40&&!stats.Completed,"Restore time without completion");
                Debug.Log("[UKUBOLA P14] PERSISTENCE SELF-TESTS PASSED | "+checks+" checks.");
            }
            catch(Exception e){Debug.LogError("[UKUBOLA P14] SELF-TEST FAILED: "+e);}
            finally{if(hpObject)UnityEngine.Object.DestroyImmediate(hpObject);if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        [MenuItem("Tools/UKUBOLA/Phase 14/Check Current Snapshot (Play Mode)",false,144)]public static void Live()
        {
            if(!EditorApplication.isPlaying){Debug.LogWarning("[UKUBOLA P14] Start a saved campaign and pause first.");return;}
            try
            {
                var scene=SceneManager.GetActiveScene();var f=UkubolaPersistenceSetup.Find<MenuFlow>(scene).Single();var stage=f.GetComponent<CampaignStage>();
                if(!f.PersistenceEnabled||f.Page!=MenuPage.Paused||!stage||stage.Index<0)throw new InvalidOperationException("Pause a living campaign mission before testing");
                var data=SaveWorld.Capture(stage,f.Hud);var round=SaveStore.Decode(SaveStore.Encode(data));SaveWorld.Check(round,stage,f.Hud);
                if(JsonUtility.ToJson(data)!=JsonUtility.ToJson(round))throw new InvalidOperationException("Round trip differs");
                Debug.Log("[UKUBOLA P14] LIVE SNAPSHOT PASSED | No production save changed.");
            }catch(Exception ex){Debug.LogError("[UKUBOLA P14] LIVE SNAPSHOT FAILED: "+ex);}
        }
    }
}
