using System;
using UnityEditor;
using UnityEngine;
using Ukubola.Player;
using Ukubola.World;
namespace Ukubola.EditorTools
{
    public static class UkubolaMvpTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 11/Run MVP Self-Tests",false,114)]
        public static void Run()
        {
            if(!UkubolaMvpSetup.CanRun())return;
            int count=0;
            try
            {
                void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);count++;}
                // All combinations of six key flags, door state, completed, alive and collider owner.
                for(int mask=0;mask<64;mask++)for(int open=0;open<2;open++)for(int completed=0;completed<2;completed++)for(int alive=0;alive<2;alive++)for(int owner=0;owner<2;owner++)
                {
                    var stage=MissionObjective.Stage((KeyId)mask,open==1,completed==1);
                    var expected=completed==1?MissionStage.Complete:(mask&1)==0?MissionStage.FindKey:open==0?MissionStage.ActivateSwitch:MissionStage.ReachExit;
                    Check(stage==expected,"Stage mismatch");
                    Check(MissionObjective.AllowsExit(stage,alive==1,owner==1)==(completed==0&&(mask&1)!=0&&open==1&&alive==1&&owner==1),"Exit permission mismatch");
                }
                MvpLayout.Check(UkubolaMvpSetup.Require<LevelDefinition>(UkubolaLevelSetup.Data));count++;
                Check(MvpLayout.Enemies.Length==4&&MvpLayout.Items.Length==16,"Unexpected layout size");
                foreach(MissionStage stage in Enum.GetValues(typeof(MissionStage)))Check(!string.IsNullOrWhiteSpace(MissionObjective.Instruction(stage)),"Missing instruction");
                Debug.Log($"[UKUBOLA P11] MVP SELF-TESTS PASSED | {count} checks.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P11] SELF-TEST FAILED: "+ex);}
        }
    }
}
