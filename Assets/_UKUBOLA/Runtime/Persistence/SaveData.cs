using System;
using System.Linq;
using UnityEngine;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Player;

namespace Ukubola.Persistence
{
    // Plain DTOs, not Unity objects. Explicit version and fixed campaign path allowlist.
    [Serializable] public sealed class SavedGear
    {
        public int hp, armor, armorKind, selected;
        public bool backpack;
        public int[] ammo;
        public bool[] weapons;
        public static SavedGear From(Loadout g)=>new SavedGear{hp=g.Health,armor=g.Armor,armorKind=(int)g.ArmorType,selected=(int)g.Selected,backpack=g.Backpack,ammo=Enumerable.Range(0,4).Select(i=>g.Count((AmmoType)i)).ToArray(),weapons=Enumerable.Range(0,8).Select(i=>g.Owns((WeaponId)i)).ToArray()};
        public bool Valid()=>hp>=1&&hp<=100&&armorKind>=0&&armorKind<=2&&armor>=0&&armor<=Armor.Capacity((ArmorKind)armorKind)&&(armor==0)==(armorKind==0)&&ammo!=null&&ammo.Length==4&&ammo.Select((n,i)=>n>=0&&n<=PlayerInventory.BaseCapacity((AmmoType)i)*(backpack?2:1)).All(x=>x)&&weapons!=null&&weapons.Length==8&&weapons[0]&&weapons[2]&&selected>=0&&selected<8&&weapons[selected];
        public Loadout ToLoadout(){if(!Valid())throw new InvalidOperationException("Invalid saved loadout");return new Loadout(hp,armor,(ArmorKind)armorKind,backpack,ammo,weapons,(WeaponId)selected);}
    }
    [Serializable] public sealed class SavedTotals
    {
        public int kills,enemies,items,pickups,secrets,rooms; public float seconds;
        public static SavedTotals From(CampaignTotals t)=>new SavedTotals{kills=t.Kills,enemies=t.Enemies,items=t.Items,pickups=t.Pickups,secrets=t.Secrets,rooms=t.Rooms,seconds=t.Seconds};
        public CampaignTotals ToTotals()=>new CampaignTotals(kills,enemies,items,pickups,secrets,rooms,seconds);
        // Direct editor entry to later missions may legitimately have zero previous totals.
        public bool Valid(int stage)=>rooms>=0&&rooms<=stage&&enemies>=0&&enemies<=14&&kills>=0&&kills<=enemies&&pickups>=0&&pickups<=49&&items>=0&&items<=pickups&&secrets>=0&&secrets<=rooms&&SaveData.Range(seconds,0,SaveData.MaxSeconds);
    }
    [Serializable] public sealed class SavedEnemy { public string name;public int hp,max,rival;public Vector3 position,lastKnown;public float yaw,cooldown;public bool alerted; }
    [Serializable] public sealed class SavedItem { public string name;public bool collected; }
    [Serializable] public sealed class SavedDoor { public string name;public float fraction,remaining;public bool opening; }
    [Serializable] public sealed class SavedSwitch { public string name;public bool active; }
    [Serializable] public sealed class SavedBarrel { public string name;public int hp,max; }
    [Serializable] public sealed class SaveData
    {
        public const int Version=1;
        public const float MaxSeconds=10_000_000f;
        public int version,stage,keys,width,depth;
        public string layout,utc;
        public SavedGear entry,current;
        public SavedTotals prior;
        public Vector3 position;
        public float yaw,elapsed,invulnerability,berserk,liftFraction;
        public bool liftRaised,secret;
        public SavedEnemy[] enemies;public SavedItem[] pickups;public SavedDoor[] doors;public SavedSwitch[] switches;public SavedBarrel[] barrels;
        public bool[] explored;
        public static bool Range(float n,float min,float max)=>!float.IsNaN(n)&&!float.IsInfinity(n)&&n>=min&&n<=max;
        public static bool Point(Vector3 p)=>Range(p.x,-1,129)&&Range(p.y,-2,5)&&Range(p.z,-1,129);
        public static bool Names<T>(T[] array,int count,Func<T,string> name) where T:class=>array!=null&&array.Length==count&&array.All(x=>x!=null&&!string.IsNullOrEmpty(name(x))&&name(x).Length<=256)&&array.Select(name).Distinct(StringComparer.Ordinal).Count()==count;
        public bool Validate(out string reason)
        {
            reason="Incompatible save version or mission";
            if(version!=Version||stage<0||stage>2)return false;
            reason="Invalid player, entry or statistics";
            if(entry==null||current==null||prior==null||!entry.Valid()||!current.Valid()||!prior.Valid(stage)||keys<0||keys>63||!Point(position)||!Range(yaw,0,360)||!Range(elapsed,0,MaxSeconds)||!Range(invulnerability,0,30.01f)||!Range(berserk,0,60.01f))return false;
            reason="Invalid world or map";
            if(string.IsNullOrEmpty(layout)||layout.Length!=64||string.IsNullOrEmpty(utc)||utc.Length>64||width<3||width>64||depth<3||depth>64||explored==null||explored.Length!=width*depth||!Range(liftFraction,0,1))return false;
            int n=6+stage*2;
            if(!Names(enemies,n,x=>x.name)||!Names(pickups,stage==0?25:24,x=>x.name)||!Names(doors,4,x=>x.name)||!Names(switches,2,x=>x.name)||!Names(barrels,2,x=>x.name))return false;
            for(int i=0;i<n;i++){var e=enemies[i];if(e.max<=0||e.max>10000||e.hp<0||e.hp>e.max||!Point(e.position)||!Point(e.lastKnown)||!Range(e.yaw,0,360)||!Range(e.cooldown,0,60)||e.rival< -1||e.rival>=n||e.rival==i)return false;}
            if(doors.Any(d=>!Range(d.fraction,0,1)||!Range(d.remaining,0,4.01f))||barrels.Any(b=>b.max<=0||b.max>10000||b.hp<0||b.hp>b.max))return false;
            reason=null;return true;
        }
        public CampaignEntry Entry()=>new CampaignEntry(SaveCampaignPaths.Missions[stage],entry.ToLoadout(),prior.ToTotals());
    }
    public static class SaveCampaignPaths
    {
        public const string Root="Assets/_UKUBOLA/Scenes/";
        public const string Title=Root+"Menus/SaveCampaignTitle.unity";
        public static readonly string[] Missions={Root+"Levels/E1M1_Save.unity",Root+"Levels/E1M2_Save.unity",Root+"Levels/E1M3_Save.unity"};
    }
}
