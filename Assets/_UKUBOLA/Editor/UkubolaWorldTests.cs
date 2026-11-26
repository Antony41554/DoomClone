using System;
using UnityEditor;
using UnityEngine;
using Ukubola.World;
using Ukubola.Player;
using Ukubola.Combat;
namespace Ukubola.EditorTools
{
    public static class UkubolaWorldTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 08/Run World Self-Tests",false,93)]
        public static void Run()
        {
            if(!UkubolaWorldSetup.CanRun())return;
            int count=0;
            void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);count++;}
            try
            {
                for(int i=0;i<64;i++)
                {
                    var keys=(KeyId)i;bool card=(i&1)!=0,skull=(i&8)!=0;
                    Check(KeyRequirement.Allows(keys,KeyId.BlueCard|KeyId.BlueSkull,true)==(card||skull),"Blue key OR: "+i);
                    Check(KeyRequirement.Allows(keys,KeyId.BlueCard|KeyId.BlueSkull,false)==(card&&skull),"Blue key AND: "+i);
                    Check(KeyRequirement.Allows(keys,KeyId.None,false),"Free door: "+i);
                }
                for(int i=0;i<=100;i++)
                {
                    float f=i/100f;Check(Mathf.Abs(Door.RemainingHeight(3.2f,f)-(3.2f-3.2f*f))<.00001f,"Door height: "+i);
                }
                Check(Door.RemainingHeight(3.2f,-1)==3.2f,"Door negative fraction");Check(Door.RemainingHeight(3.2f,2)==0,"Door excess fraction");
                Check(!DamageZone.Due(.999,1),"Early damage");Check(DamageZone.Due(1,1),"Damage at interval");Check(DamageZone.Due(1.01,1),"Delayed damage");
                Check(ExplosiveBarrel.BlastDamage(0,3,60)==60,"Blast center");Check(ExplosiveBarrel.BlastDamage(1.5f,3,60)==30,"Blast half radius");Check(ExplosiveBarrel.BlastDamage(3,3,60)==0,"Blast radius");Check(ExplosiveBarrel.BlastDamage(4,3,60)==0,"Outside blast");Check(ExplosiveBarrel.BlastDamage(0,0,60)==0,"Zero radius");
                var go=new GameObject("World test (temporary)");try{var secret=go.AddComponent<SecretTrigger>();Check(secret.TryDiscover(),"Discover first");Check(!secret.TryDiscover(),"Discover once");secret.ResetSecret();Check(secret.TryDiscover(),"Discover after reset");}finally{UnityEngine.Object.DestroyImmediate(go);}
                Debug.Log("[UKUBOLA P08] WORLD SELF-TESTS PASSED | "+count+" checks.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P08] SELF-TEST FAILED after "+count+" checks: "+ex.Message);}
        }
    }
}
