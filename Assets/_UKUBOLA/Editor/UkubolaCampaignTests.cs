using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
namespace Ukubola.EditorTools
{
    public static class UkubolaCampaignTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 13/Run Campaign Self-Tests",false,136)]
        public static void Run()
        {
            if(!UkubolaMvpSetup.CanRun())return;int checks=0;GameObject go=null;var old=CampaignRun.Entry;
            void Assert(bool pass,string name){if(!pass)throw new InvalidOperationException(name);checks++;}
            try
            {
                for(int i=1;i<3;i++){var l=CampaignLayout.For(i);var map=ScriptableObject.CreateInstance<LevelDefinition>();try{map.SetLayout(l.Id,l.Rows());l.Check(map);Assert(true,"Layout");Assert(l.Reach(map,true,true,true,true).Count==Enumerable.Range(0,map.Width).Sum(x=>Enumerable.Range(0,map.Depth).Count(z=>LevelDefinition.Walkable(map.Tile(x,z)))),"Full connectivity");}finally{UnityEngine.Object.DestroyImmediate(map);}}
                var fresh=Loadout.Fresh();Assert(fresh.Health==100&&fresh.Armor==0&&!fresh.Backpack&&fresh.Count(AmmoType.Bullets)==50&&fresh.Selected==WeaponId.Pistol,"Fresh loadout");
                for(int i=0;i<=1000;i++)
                {
                    var a=new[]{i,i-100,i*2,-i};var flags=new bool[8];flags[(int)WeaponId.Shotgun]=i%2==0;bool pack=i%3==0;var x=new Loadout(i-50,i-100,ArmorKind.Blue,pack,a,flags,WeaponId.Shotgun);a[0]=-1;flags[(int)WeaponId.Shotgun]=false;
                    Assert(x.Health==Mathf.Clamp(i-50,1,100)&&x.Armor==Mathf.Clamp(i-100,0,200),"Health/armor normalization");Assert(x.Count(AmmoType.Bullets)==Mathf.Min(i,pack?400:200)&&x.Count(AmmoType.Shells)==Mathf.Clamp(i-100,0,pack?100:50)&&x.Count(AmmoType.Rockets)==Mathf.Min(i*2,pack?100:50)&&x.Count(AmmoType.Cells)==0,"Ammo bounds and immutable copy");Assert(x.Owns(WeaponId.Fist)&&x.Owns(WeaponId.Pistol)&&x.Owns(WeaponId.Shotgun)==(i%2==0)&&x.Selected==(i%2==0?WeaponId.Shotgun:WeaponId.Pistol),"Ownership and selected weapon");
                }
                CampaignRun.Reset();var entry=CampaignRun.Enter("one");Assert(ReferenceEquals(entry,CampaignRun.Enter("one")),"Retry uses checkpoint");var totals=new CampaignTotals(3,6,10,25,1,1,40);var next=new CampaignEntry("two",fresh,totals);CampaignRun.Set(next);Assert(ReferenceEquals(next,CampaignRun.Enter("two")),"Next mission entry");var combined=totals+new CampaignTotals(2,8,4,24,0,1,20);Assert(combined.Kills==5&&combined.Enemies==14&&combined.Items==14&&combined.Pickups==49&&combined.Secrets==1&&combined.Rooms==2&&combined.Seconds==60,"Accumulate results once");CampaignRun.Reset();Assert(CampaignRun.Enter("one").Prior.Enemies==0,"New campaign clears prior totals");
                go=new GameObject("Phase13 restore tests"){hideFlags=HideFlags.HideAndDontSave};var armor=go.AddComponent<Armor>();var hp=go.AddComponent<Health>();hp.Configure(100,armor);int damaged=0;hp.Damaged+=(h,d,r)=>damaged++;armor.RestoreState(ArmorKind.Green,73);hp.RestoreLiving(41);Assert(hp.Current==41&&armor.Points==73&&armor.Kind==ArmorKind.Green&&damaged==0,"Silent living restore");armor.RestoreState(ArmorKind.None,50);Assert(armor.Points==0&&armor.Kind==ArmorKind.None,"Armor normalization");
                Debug.Log("[UKUBOLA P13] CAMPAIGN SELF-TESTS PASSED | "+checks+" checks.");
            }
            catch(Exception e){Debug.LogError("[UKUBOLA P13] SELF-TEST FAILED: "+e);}
            finally{if(go)UnityEngine.Object.DestroyImmediate(go);CampaignRun.Set(old);}
        }
    }
}
