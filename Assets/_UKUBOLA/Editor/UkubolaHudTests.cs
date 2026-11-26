using System;
using UnityEditor;
using UnityEngine;
using Ukubola.UI;
using Ukubola.Audio;
using Ukubola.Player;
namespace Ukubola.EditorTools
{
    public static class UkubolaHudTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 09/Run HUD Self-Tests",false,103)]
        public static void Run()
        {
            if(!UkubolaHudSetup.CanRun())return;int checks=0;
            void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks++;}
            try{
                for(int hp=0;hp<=100;hp++)for(int exp=0;exp<5;exp++){
                    int expected=hp==0?15:hp>=67?exp:hp>=34?5+exp:10+exp;
                    Check(HudRules.FaceIndex(hp,100,(FaceExpression)exp)==expected,$"Face hp {hp}, exp {exp}");}
                for(int mask=0;mask<64;mask++)for(int c=0;c<3;c++){
                    int card=1<<c,skull=1<<(c+3);string expected=((mask&card)>0?"C":"-")+((mask&skull)>0?"S":"-");
                    Check(HudRules.KeyLabel((KeyId)mask,(KeyId)card,(KeyId)skull)==expected,"Key mask "+mask);}
                var sequence=new[]{FaceExpression.Center,FaceExpression.Left,FaceExpression.Center,FaceExpression.Right};for(int i=0;i<16;i++)Check(HudRules.IdleFace(i*1.5f+.25f)==sequence[i%4],"Idle sequence");
                for(int i=0;i<=10;i++)for(int j=0;j<=10;j++)Check(GameAudio.MotionDirection(i/10f,j/10f)==Math.Sign(j-i),"Door direction");
                Check(HudRules.FaceIndex(0,0,FaceExpression.Fire)==15,"Dead overrides expression");Check(HudRules.FaceIndex(50,50,FaceExpression.Center)==0,"Relative health");
                Debug.Log("[UKUBOLA P09] HUD SELF-TESTS PASSED | "+checks+" checks.");
            }catch(Exception ex){Debug.LogError("[UKUBOLA P09] HUD SELF-TESTS FAILED after "+checks+" checks: "+ex.Message);}
        }
    }
}
