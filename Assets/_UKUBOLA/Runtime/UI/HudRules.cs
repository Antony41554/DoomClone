using UnityEngine;
using Ukubola.Player;
namespace Ukubola.UI
{
    public enum FaceExpression { Center, Left, Right, Pain, Fire }
    public static class HudRules
    {
        public const int FaceCount=16;
        public static int FaceIndex(int hp,int max,FaceExpression expression)
        {
            if(hp<=0)return 15;
            long scaled=100L*hp; long maximum=Mathf.Max(1,max);
            int tier=scaled>66L*maximum?0:scaled>33L*maximum?1:2;
            return tier*5+Mathf.Clamp((int)expression,0,4);
        }
        public static FaceExpression IdleFace(float time)
        {
            int phase=Mathf.FloorToInt(Mathf.Max(0,time)/1.5f)%4;
            return phase==1?FaceExpression.Left:phase==3?FaceExpression.Right:FaceExpression.Center;
        }
        public static string KeyLabel(KeyId keys,KeyId card,KeyId skull)
        {return ((keys&card)!=0?"C":"-")+((keys&skull)!=0?"S":"-");}
    }
}
