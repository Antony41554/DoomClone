using System;
using UnityEditor;
using UnityEngine;
using Ukubola.UI;
namespace Ukubola.EditorTools
{
    public static class UkubolaMenuTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 10/Run Menu Self-Tests",false,104)]
        public static void Run()
        {
            if(!UkubolaMenuSetup.CanRun())return;int checks=0;
            void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks++;}
            try
            {
                Check(MenuRules.GameplayPage(false,false)==MenuPage.Playing,"Playing");Check(MenuRules.GameplayPage(true,false)==MenuPage.Dead,"Dead");Check(MenuRules.GameplayPage(false,true)==MenuPage.Results,"Results");Check(MenuRules.GameplayPage(true,true)==MenuPage.Dead,"Death priority");
                foreach(MenuPage page in Enum.GetValues(typeof(MenuPage)))Check(MenuRules.IsTerminal(page)==(page==MenuPage.Dead||page==MenuPage.Results),"Terminal: "+page);
                var defaults=MenuPreferences.Defaults;Check(defaults.Music==1&&defaults.Effects==1&&defaults.Sensitivity==1&&!defaults.Fullscreen,"Defaults");
                var invalid=new MenuPreferences{Music=float.NaN,Effects=float.PositiveInfinity,Sensitivity=float.NegativeInfinity,Fullscreen=true}.Normalized();Check(invalid.Music==1&&invalid.Effects==1&&invalid.Sensitivity==1&&invalid.Fullscreen,"Nonfinite input");
                for(int i=-20;i<=40;i++)
                {
                    float value=i/10f;var p=new MenuPreferences{Music=value,Effects=value,Sensitivity=value}.Normalized();
                    Check(p.Music==Mathf.Clamp01(value),"Music clamp");Check(p.Effects==Mathf.Clamp01(value),"Effects clamp");Check(p.Sensitivity==Mathf.Clamp(value,.5f,2),"Sensitivity clamp");
                }
                for(int total=1;total<=32;total++)for(int count=-1;count<=total+1;count++)
                {double expected=Math.Round(Math.Max(0,Math.Min(1,(double)count/total))*100,MidpointRounding.ToEven); // Float midpoint cases use tolerance below.
                    Check(Math.Abs(MenuRules.Percent(count,total)-expected)<=1&&MenuRules.Percent(count,total)>=0&&MenuRules.Percent(count,total)<=100,"Percent bounds");}
                Check(MenuRules.Percent(0,0)==100,"No objectives");Check(MenuRules.Percent(1,2)==50,"Half");Check(MenuRules.Percent(3,4)==75,"Three quarters");
                Check(MenuRules.Clock(0)=="00:00","Zero time");Check(MenuRules.Clock(59.9f)=="00:59","Floor time");Check(MenuRules.Clock(60)=="01:00","Minute");Check(MenuRules.Clock(125.9f)=="02:05","Two minutes");Check(MenuRules.Clock(float.NaN)=="00:00","Invalid clock");
                Debug.Log($"[UKUBOLA P10] MENU SELF-TESTS PASSED | {checks} checks.");
            }
            catch(Exception ex){Debug.LogError($"[UKUBOLA P10] MENU SELF-TESTS FAILED after {checks}: {ex}");}
        }
    }
}
