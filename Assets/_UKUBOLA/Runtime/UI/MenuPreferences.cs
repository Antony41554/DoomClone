using UnityEngine;

namespace Ukubola.UI
{
    // Settings are preferences, not saved gameplay. The prefix isolates this project.
    public struct MenuPreferences
    {
        public float Music, Effects, Sensitivity;
        public bool Fullscreen;
        private const string Prefix="UKUBOLA.P10.";
        public static MenuPreferences Defaults => new MenuPreferences { Music=1f, Effects=1f, Sensitivity=1f, Fullscreen=false };
        public static float Limit(float value,float min,float max,float fallback)
            => float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
        public MenuPreferences Normalized()
        {
            return new MenuPreferences { Music=Limit(Music,0,1,1), Effects=Limit(Effects,0,1,1), Sensitivity=Limit(Sensitivity,.5f,2f,1), Fullscreen=Fullscreen };
        }
        public static MenuPreferences Load()
        {
            return new MenuPreferences { Music=PlayerPrefs.GetFloat(Prefix+"Music",1), Effects=PlayerPrefs.GetFloat(Prefix+"Effects",1), Sensitivity=PlayerPrefs.GetFloat(Prefix+"Sensitivity",1), Fullscreen=PlayerPrefs.GetInt(Prefix+"Fullscreen",0)==1 }.Normalized();
        }
        public void Save()
        {
            var p=Normalized(); PlayerPrefs.SetFloat(Prefix+"Music",p.Music); PlayerPrefs.SetFloat(Prefix+"Effects",p.Effects);
            PlayerPrefs.SetFloat(Prefix+"Sensitivity",p.Sensitivity); PlayerPrefs.SetInt(Prefix+"Fullscreen",p.Fullscreen?1:0); PlayerPrefs.Save();
        }
    }

    public enum MenuPage { Title, Playing, Paused, Options, ConfirmRestart, ConfirmTitle, ConfirmExit, Dead, Results, Loading, ConfirmNew, ConfirmLoad }
    public static class MenuRules
    {
        public static MenuPage GameplayPage(bool dead,bool complete) => dead?MenuPage.Dead:complete?MenuPage.Results:MenuPage.Playing;
        public static bool IsTerminal(MenuPage page)=>page==MenuPage.Dead||page==MenuPage.Results;
        public static int Percent(int count,int total)=>total<=0?100:Mathf.RoundToInt(Mathf.Clamp01((float)count/total)*100f);
        public static string Clock(float seconds)
        {
            int whole=Mathf.FloorToInt(MenuPreferences.Limit(seconds,0,359999,0));
            return $"{whole/60:00}:{whole%60:00}";
        }
    }
}
