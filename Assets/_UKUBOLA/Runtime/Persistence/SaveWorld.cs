using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.UI;
using Ukubola.World;

namespace Ukubola.Persistence
{
    public static class SaveWorld
    {
        private static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
        public static Automap Map(ClassicHud hud)=>hud.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Automap>(true)).Single();
        // Ordered instance names plus static geometry and definitions are a layout identity, not runtime IDs.
        // Renaming/reordering objects or changing the map deliberately invalidates old saves.
        public static string Signature(ClassicHud hud)
        {
            var w=hud.World;var s=w.Source;var map=Map(hud).Level;var b=new StringBuilder();
            b.Append(map.Width).Append(':').Append(map.Depth).Append('\n');
            for(int z=0;z<map.Depth;z++){for(int x=0;x<map.Width;x++)b.Append(map.Tile(x,z));b.Append('\n');}
            void Add(string category,string name,string detail){b.Append(category).Append(':').Append(name.Length).Append(':').Append(name).Append(':').Append(detail).Append('\n');}
            foreach(var a in s.Enemies)Add("enemy",a.name,a.Definition.name+":"+a.GetComponent<Health>().MaxHealth);
            foreach(var p in s.Pickups)Add("item",p.name,p.Definition.name);
            foreach(var d in w.Doors)Add("door",d.name,((int)d.Required)+":"+d.SwitchOnly);
            foreach(var sw in w.Switches)Add("switch",sw.name,sw.Door?sw.Door.name:sw.Lift.name);
            foreach(var barrel in w.Barrels)Add("barrel",barrel.name,barrel.GetComponent<Health>().MaxHealth.ToString());
            return SaveStore.Hash(b.ToString());
        }
        public static SaveData Capture(CampaignStage stage,ClassicHud hud)
        {
            var w=hud.World;var s=w.Source;var hp=s.Player;var bag=hp.GetComponent<PlayerInventory>();var powers=hp.GetComponent<Powerup>();var map=Map(hud);var entry=CampaignRun.Entry;
            Require(stage&&stage.Index>=0&&stage.Index<3&&stage.CurrentPath==SaveCampaignPaths.Missions[stage.Index]&&entry!=null&&entry.Path==stage.CurrentPath&&!hp.IsDead&&!hud.Stats.Completed,"Save requires a living, unfinished campaign mission");
            var data=new SaveData{version=SaveData.Version,stage=stage.Index,layout=Signature(hud),utc=DateTime.UtcNow.ToString("O"),entry=SavedGear.From(entry.Gear),current=SavedGear.From(Loadout.Capture(hp,s.Weapons)),prior=SavedTotals.From(entry.Prior),keys=(int)bag.Keys,position=hp.transform.position,yaw=Mathf.Repeat(hp.transform.eulerAngles.y,360f),elapsed=hud.Stats.ElapsedSeconds,
                invulnerability=powers.Remaining(PowerupKind.Invulnerability),berserk=powers.Remaining(PowerupKind.Berserk),liftFraction=w.Lift.ElevationFraction,liftRaised=w.Lift.Raised,secret=w.Secret.Found,width=map.Level.Width,depth=map.Level.Depth,explored=map.CaptureExploration(),
                enemies=s.Enemies.Select(a=>new SavedEnemy{name=a.name,hp=a.GetComponent<Health>().Current,max=a.GetComponent<Health>().MaxHealth,position=a.SavePosition,yaw=Mathf.Repeat(a.transform.eulerAngles.y,360f),alerted=a.SaveAlerted,lastKnown=a.SaveLastKnown,cooldown=a.SaveCooldown,rival=Array.FindIndex(s.Enemies,e=>e.GetComponent<Health>()==a.CombatTarget)}).ToArray(),
                pickups=s.Pickups.Select(p=>new SavedItem{name=p.name,collected=p.Collected}).ToArray(),doors=w.Doors.Select(d=>new SavedDoor{name=d.name,fraction=d.Fraction,opening=d.Opening,remaining=d.CloseRemaining}).ToArray(),switches=w.Switches.Select(sw=>new SavedSwitch{name=sw.name,active=sw.Active}).ToArray(),barrels=w.Barrels.Select(b=>new SavedBarrel{name=b.name,hp=b.GetComponent<Health>().Current,max=b.GetComponent<Health>().MaxHealth}).ToArray()};
            Check(data,stage,hud);return data;
        }
        private static bool OnFloor(LevelDefinition map,Vector3 p)
        {
            int x=Mathf.FloorToInt(p.x/LevelDefinition.CellSize),z=Mathf.FloorToInt(p.z/LevelDefinition.CellSize);char t=map.Tile(x,z);
            return LevelDefinition.Walkable(t)&&p.y>=LevelDefinition.Elevation(t)-.4f&&p.y<=LevelDefinition.Ceiling+.1f;
        }
        public static void Check(SaveData data,CampaignStage stage,ClassicHud hud)
        {
            if(data==null)throw new InvalidOperationException("Missing save");if(!data.Validate(out var reason))throw new InvalidOperationException(reason);
            var w=hud.World;var s=w.Source;var map=Map(hud).Level;
            Require(stage.Index==data.stage&&stage.CurrentPath==SaveCampaignPaths.Missions[data.stage]&&data.layout==Signature(hud)&&map.Width==data.width&&map.Depth==data.depth,"Save is incompatible with this mission layout");
            Require(OnFloor(map,data.position),"Saved player position is not in a playable cell");
            Require(data.enemies.Select(e=>e.name).SequenceEqual(s.Enemies.Select(e=>e.name))&&data.pickups.Select(e=>e.name).SequenceEqual(s.Pickups.Select(e=>e.name))&&data.doors.Select(e=>e.name).SequenceEqual(w.Doors.Select(e=>e.name))&&data.switches.Select(e=>e.name).SequenceEqual(w.Switches.Select(e=>e.name))&&data.barrels.Select(e=>e.name).SequenceEqual(w.Barrels.Select(e=>e.name)),"Saved object list differs from scene");
            for(int i=0;i<s.Enemies.Length;i++)Require(data.enemies[i].max==s.Enemies[i].GetComponent<Health>().MaxHealth&&OnFloor(map,data.enemies[i].position),"Invalid saved enemy: "+s.Enemies[i].name);
            for(int i=0;i<w.Barrels.Length;i++)Require(data.barrels[i].max==w.Barrels[i].GetComponent<Health>().MaxHealth,"Invalid barrel maximum");
        }
        // Called in MenuFlow.Start, before enemy Start and before any normal gameplay frame.
        public static void PrepareNavigation(ClassicHud hud)
        {
            foreach(var d in hud.World.Doors)d.GetComponent<NavMeshObstacle>().enabled=false;
            foreach(var b in hud.World.Barrels){var o=b.GetComponent<NavMeshObstacle>();if(o)o.enabled=false;}
        }
        // Called once from LateUpdate, after all Start methods (enemy, map, audio) have run.
        public static void Apply(SaveData data,CampaignStage stage,ClassicHud hud)
        {
            Check(data,stage,hud);var w=hud.World;var s=w.Source;var hp=s.Player;
            // Restore actors against the base NavMesh before reinstating dynamic obstacles.
            for(int i=0;i<s.Enemies.Length;i++){var e=data.enemies[i];s.Enemies[i].RestoreEnemy(e.hp,e.position,e.yaw,e.alerted,e.lastKnown,e.cooldown,null);}
            for(int i=0;i<s.Enemies.Length;i++){int j=data.enemies[i].rival;s.Enemies[i].RestoreRival(j>=0?s.Enemies[j].GetComponent<Health>():null);}
            for(int i=0;i<w.Doors.Length;i++){var d=data.doors[i];w.Doors[i].RestoreDoor(d.fraction,d.opening,d.remaining);}
            for(int i=0;i<w.Switches.Length;i++)w.Switches[i].RestoreSwitch(data.switches[i].active);
            w.Lift.RestoreLift(data.liftFraction,data.liftRaised);
            for(int i=0;i<w.Barrels.Length;i++)w.Barrels[i].RestoreBarrel(data.barrels[i].hp);
            for(int i=0;i<s.Pickups.Length;i++)s.Pickups[i].RestoreCollected(data.pickups[i].collected);
            w.Secret.ResetSecret();if(data.secret)w.Secret.TryDiscover();w.Zone.ResetZone();w.Teleport.ResetTeleport();
            data.current.ToLoadout().Apply(hp,s.Weapons);var bag=hp.GetComponent<PlayerInventory>();if(data.keys!=0)bag.TryGrantKeys((KeyId)data.keys);
            var power=hp.GetComponent<Powerup>();if(data.invulnerability>0)power.Grant(PowerupKind.Invulnerability,data.invulnerability);if(data.berserk>0)power.Grant(PowerupKind.Berserk,data.berserk);
            var cc=hp.GetComponent<CharacterController>();cc.enabled=false;hp.transform.SetPositionAndRotation(data.position,Quaternion.Euler(0,data.yaw,0));cc.enabled=true;Physics.SyncTransforms();
            hud.Stats.RestoreElapsed(data.elapsed);Map(hud).RestoreExploration(data.explored);
            hud.Audio.RefreshAfterLoad();hud.RefreshAfterLoad();CampaignRun.Set(data.Entry());
        }
    }
}
