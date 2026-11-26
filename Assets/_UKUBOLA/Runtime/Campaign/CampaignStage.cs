using UnityEngine;
using Ukubola.UI;
namespace Ukubola.Campaign
{
    [DisallowMultipleComponent]
    public sealed class CampaignStage:MonoBehaviour
    {
        [SerializeField] private int index=-1;
        [SerializeField] private string currentPath,nextPath,label;
        public int Index=>index; public string CurrentPath=>currentPath;public string NextPath=>nextPath;public string Label=>label;
        public bool Final=>index>=0&&string.IsNullOrEmpty(nextPath);
        public void Configure(int number,string current,string next,string title){index=number;currentPath=current;nextPath=next;label=title;}
        public void Enter(ClassicHud hud)
        {
            if(index<0){CampaignRun.Reset();return;}
            var entry=CampaignRun.Enter(currentPath);entry.Gear.Apply(hud.World.Source.Player,hud.World.Source.Weapons);
            var g=entry.Gear;Debug.Log($"[UKUBOLA P13] ENTRY {label} | HP {g.Health} ARM {g.Armor} {g.ArmorType} | B {g.Count(Ukubola.Player.AmmoType.Bullets)} S {g.Count(Ukubola.Player.AmmoType.Shells)} R {g.Count(Ukubola.Player.AmmoType.Rockets)} C {g.Count(Ukubola.Player.AmmoType.Cells)} | {g.Selected} | Backpack {g.Backpack}");
        }
        public bool TryAdvance(ClassicHud hud,string path)
        {
            if(index<0||Final||path!=nextPath||!hud||!hud.Stats.Completed||hud.World.Source.Player.IsDead)return false;
            var entry=CampaignRun.Enter(currentPath);CampaignRun.Set(new CampaignEntry(path,Loadout.Capture(hud.World.Source.Player,hud.World.Source.Weapons),entry.Prior+CampaignTotals.Read(hud)));return true;
        }
        public string TotalResults(ClassicHud hud){var t=CampaignRun.Enter(currentPath).Prior+CampaignTotals.Read(hud);return $"TOTAL KILLS   {t.Kills}/{t.Enemies}   {MenuRules.Percent(t.Kills,t.Enemies)}%\nTOTAL ITEMS   {t.Items}/{t.Pickups}   {MenuRules.Percent(t.Items,t.Pickups)}%\nTOTAL SECRETS   {t.Secrets}/{t.Rooms}   {MenuRules.Percent(t.Secrets,t.Rooms)}%\nTOTAL TIME   {MenuRules.Clock(t.Seconds)}";}
    }
}
