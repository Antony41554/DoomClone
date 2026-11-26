using System;
using UnityEditor;
using UnityEngine;
using Ukubola.Enemies;
using Ukubola.Combat;
namespace Ukubola.EditorTools
{
    public static class UkubolaEnemyTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 06/Run Enemy Self-Tests",false,73)]
        public static void Run()
        {
            if(!UkubolaEnemySetup.CanRun())return;
            int count=0;
            void Check(bool ok,string msg){if(!ok)throw new InvalidOperationException(msg);count++;}
            try
            {
                foreach(float radius in new[]{1f,9f,14f,18f})foreach(float distance in new[]{0f,radius*.5f,radius,radius+.01f})foreach(bool visible in new[]{false,true})foreach(bool cone in new[]{false,true})
                    Check(EnemyBrain.Detect(distance,radius,visible,cone)==(distance<=radius&&visible&&cone),"Vision threshold");
                foreach(float radius in new[]{1f,14f})foreach(float distance in new[]{0f,radius,radius+.01f})foreach(bool pulse in new[]{false,true})
                    Check(EnemyBrain.Hear(distance,radius,pulse)==(pulse&&distance<=radius),"Noise threshold");
                foreach(float range in new[]{9f,10f})foreach(float distance in new[]{0f,range,range+.01f})foreach(bool visible in new[]{false,true})foreach(float time in new[]{0f,1f,2f})
                    Check(EnemyBrain.CanAttack(distance,range,visible,time,1f)==(visible&&distance<=range&&time>=1f),"Attack threshold");
                var d=ScriptableObject.CreateInstance<EnemyDefinition>();
                try {d.Configure(EnemyAttackKind.Hitscan,20,8,2.5f,9,1.4f);Check(d.IsValid,"Guard definition");d.Configure(EnemyAttackKind.Projectile,60,15,2.8f,10,1.6f);Check(d.IsValid,"Imp definition");d.Configure(EnemyAttackKind.Projectile,0,15,2.8f,10,1.6f);Check(!d.IsValid,"Invalid health");d.Configure(EnemyAttackKind.Projectile,60,15,2.8f,10,.2f);Check(!d.IsValid,"Invalid interval");}finally{UnityEngine.Object.DestroyImmediate(d);}
                // Health is the actual Phase 04 implementation. No Play-mode simulation claimed.
                var go=new GameObject("Temporary Enemy Test");
                try {var hp=go.AddComponent<Health>();hp.Configure(20,null);int deaths=0;hp.Died+=(_,__)=>deaths++;Check(hp.Current==20,"Initial enemy HP");Check(hp.TakeDamage(new DamageInfo(8)).HealthLost==8&&hp.Current==12,"Enemy damage");Check(hp.TakeDamage(new DamageInfo(100)).Killed&&hp.IsDead,"Enemy death");Check(!hp.TakeDamage(new DamageInfo(1)).Applied&&deaths==1,"Death only once");hp.ResetState();Check(!hp.IsDead&&hp.Current==20,"Enemy reset");}finally{UnityEngine.Object.DestroyImmediate(go);}
                Debug.Log("[UKUBOLA P06] ENEMY SELF-TESTS PASSED | "+count+" checks.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P06] SELF-TEST FAILED after "+count+" checks: "+ex.Message);}
        }
    }
}
