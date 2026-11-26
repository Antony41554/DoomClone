using System;
using System.Linq;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.UI;
using Ukubola.Enemies;
namespace Ukubola.Campaign
{
    // Immutable in-memory checkpoint. Scene-owned state is never retained.
    public sealed class Loadout
    {
        public int Health {get;} public int Armor {get;} public ArmorKind ArmorType {get;}
        public bool Backpack {get;} public WeaponId Selected {get;}
        private readonly int[] ammunition; private readonly bool[] owned;
        public Loadout(int health,int armor,ArmorKind kind,bool backpack,int[] ammo,bool[] weapons,WeaponId selected)
        {
            if(ammo==null||ammo.Length!=4||weapons==null||weapons.Length!=8)throw new ArgumentException("Invalid loadout dimensions");
            Health=Mathf.Clamp(health,1,100);Armor=Mathf.Clamp(armor,0,Ukubola.Combat.Armor.Capacity(kind));ArmorType=Armor>0?kind:ArmorKind.None;Backpack=backpack;
            ammunition=ammo.Select((n,i)=>Mathf.Clamp(n,0,PlayerInventory.BaseCapacity((AmmoType)i)*(Backpack?2:1))).ToArray();owned=(bool[])weapons.Clone();owned[(int)WeaponId.Pistol]=owned[(int)WeaponId.Fist]=true;
            Selected=(int)selected>=0&&(int)selected<owned.Length&&owned[(int)selected]?selected:WeaponId.Pistol;
        }
        public int Count(AmmoType type)=>ammunition[(int)type]; public bool Owns(WeaponId id)=>owned[(int)id];
        public static Loadout Fresh()=>new Loadout(100,0,ArmorKind.None,false,new[]{50,0,0,0},new bool[8],WeaponId.Pistol);
        public static Loadout Capture(Ukubola.Combat.Health hp,WeaponController weapons)
        {
            if(!hp||hp.IsDead||!weapons)throw new InvalidOperationException("Only a living player can advance.");
            var bag=hp.GetComponent<PlayerInventory>();return new Loadout(hp.Current,hp.Armor.Points,hp.Armor.Kind,bag.HasBackpack,Enumerable.Range(0,4).Select(i=>bag.Count((AmmoType)i)).ToArray(),Enumerable.Range(0,8).Select(i=>bag.HasWeapon((WeaponId)i)).ToArray(),weapons.Selected?weapons.Selected.Weapon:WeaponId.Pistol);
        }
        public void Apply(Ukubola.Combat.Health hp,WeaponController guns)
        {
            var bag=hp.GetComponent<PlayerInventory>();bag.ResetState();if(Backpack)bag.TryGrantBackpack();
            for(int i=0;i<4;i++){var t=(AmmoType)i;int current=bag.Count(t);if(current>0)bag.TryConsumeAmmo(t,current);bag.AddAmmo(t,Count(t));}
            for(int i=0;i<8;i++)if(Owns((WeaponId)i))bag.TryGrantWeapon((WeaponId)i);
            bag.ClearKeys();var power=hp.GetComponent<Powerup>();if(power)power.Clear();hp.Armor.RestoreState(ArmorType,Armor);hp.RestoreLiving(Health);
            var d=Selected==WeaponId.Pistol?guns.Pistol:Selected==WeaponId.Shotgun?guns.Shotgun:guns.Advanced.FirstOrDefault(x=>x&&x.Weapon==Selected);if(!guns.Select(d))guns.Select(guns.Pistol);
        }
    }
    public readonly struct CampaignTotals
    {
        public readonly int Kills,Enemies,Items,Pickups,Secrets,Rooms;public readonly float Seconds;
        public CampaignTotals(int kills,int enemies,int items,int pickups,int secrets,int rooms,float seconds){Kills=kills;Enemies=enemies;Items=items;Pickups=pickups;Secrets=secrets;Rooms=rooms;Seconds=seconds;}
        public static CampaignTotals operator +(CampaignTotals a,CampaignTotals b)=>new CampaignTotals(a.Kills+b.Kills,a.Enemies+b.Enemies,a.Items+b.Items,a.Pickups+b.Pickups,a.Secrets+b.Secrets,a.Rooms+b.Rooms,a.Seconds+b.Seconds);
        public static CampaignTotals Read(ClassicHud hud){var w=hud.World;return new CampaignTotals(w.Source.Enemies.Count(x=>x.State==EnemyState.Dead),w.Source.Enemies.Length,w.Source.Pickups.Count(x=>x.Collected),w.Source.Pickups.Length,w.Secret.Found?1:0,1,hud.Stats.ElapsedSeconds);}
    }
    public sealed class CampaignEntry
    {
        public string Path {get;} public Loadout Gear {get;} public CampaignTotals Prior {get;}
        public CampaignEntry(string path,Loadout gear,CampaignTotals prior){Path=path;Gear=gear;Prior=prior;}
    }
    public static class CampaignRun
    {
        public static CampaignEntry Entry {get;private set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] public static void Reset()=>Entry=null;
        public static void Set(CampaignEntry entry)=>Entry=entry;
        public static CampaignEntry Enter(string path){if(Entry==null||Entry.Path!=path)Entry=new CampaignEntry(path,Loadout.Fresh(),default);return Entry;}
    }
}
