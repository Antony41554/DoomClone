using UnityEngine;
using UnityEngine.UI;
using Ukubola.Player;
using Ukubola.Combat;
namespace Ukubola.World
{
    public enum MissionStage { FindKey, ActivateSwitch, ReachExit, Complete }
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LevelExit))]
    public sealed class MissionObjective : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private Health health;
        [SerializeField] private Door remoteDoor;
        [SerializeField] private LevelStats stats;
        [SerializeField] private Text objectiveLabel;
        private float blockedUntil;
        [SerializeField] private string keyInstruction, switchInstruction, exitInstruction;
        public void ConfigureInstructions(string key, string remote, string exit)
        {keyInstruction=key;switchInstruction=remote;exitInstruction=exit;}
        private string CurrentInstruction(MissionStage stage)
        {
            string custom=stage==MissionStage.FindKey?keyInstruction:stage==MissionStage.ActivateSwitch?switchInstruction:stage==MissionStage.ReachExit?exitInstruction:null;
            return string.IsNullOrEmpty(custom)?Instruction(stage):custom;
        }
        public PlayerInventory Inventory=>inventory;
        public Health Health=>health;
        public Door RemoteDoor=>remoteDoor;
        public LevelStats Stats=>stats;
        public Text ObjectiveLabel=>objectiveLabel;
        public void Configure(PlayerInventory bag,Health hp,Door remote,LevelStats state,Text text)
        {inventory=bag;health=hp;remoteDoor=remote;stats=state;objectiveLabel=text;}
        public static MissionStage Stage(KeyId keys,bool remoteOpen,bool complete)
        {
            if(complete)return MissionStage.Complete;
            if((keys & KeyId.BlueCard)==0)return MissionStage.FindKey;
            return remoteOpen?MissionStage.ReachExit:MissionStage.ActivateSwitch;
        }
        public MissionStage CurrentStage=>Stage(inventory?inventory.Keys:KeyId.None,remoteDoor&&remoteDoor.IsOpen,stats&&stats.Completed);
        public static bool AllowsExit(MissionStage stage,bool alive,bool correctPlayer)=>stage==MissionStage.ReachExit&&alive&&correctPlayer;
        public bool TryExit(Collider other)
        {
            bool ok=AllowsExit(CurrentStage,health&&!health.IsDead,other&&inventory&&other.GetComponentInParent<PlayerInventory>()==inventory);
            if(!ok && other && inventory && other.GetComponentInParent<PlayerInventory>()==inventory)blockedUntil=Time.unscaledTime+1.2f;
            return ok;
        }
        public static string Instruction(MissionStage stage)
        {
            switch(stage){case MissionStage.FindKey:return "1/3 FIND BLUE CARD - EAST WING";case MissionStage.ActivateSwitch:return "2/3 USE ORANGE DOOR SWITCH - EAST WING";case MissionStage.ReachExit:return "3/3 REACH GREEN EXIT - SOUTH-EAST";default:return "MISSION COMPLETE";}
        }
        private void LateUpdate()
        {
            if(!objectiveLabel)return;
            var stage=CurrentStage;
            objectiveLabel.text=(Time.unscaledTime<blockedUntil&&stage!=MissionStage.Complete?"EXIT LOCKED | ":"")+CurrentInstruction(stage);
            objectiveLabel.color=stage==MissionStage.ReachExit||stage==MissionStage.Complete?new Color(.55f,1f,.58f):new Color(1f,.83f,.42f);
        }
    }
}
