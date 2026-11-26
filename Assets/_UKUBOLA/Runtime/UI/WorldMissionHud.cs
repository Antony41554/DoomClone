using UnityEngine;
using UnityEngine.UI;
using Ukubola.World;

namespace Ukubola.UI
{
    public sealed class WorldMissionHud:MonoBehaviour
    {
        [SerializeField] private MissionContext context;
        [SerializeField] private WorldSaveController saves;
        [SerializeField] private Text objectives,status;
        private string previousStatus="";
        private float statusUntil;
        public void Configure(MissionContext world,WorldSaveController repository,Text tasks,Text feedback)
        {context=world;saves=repository;objectives=tasks;status=feedback;}
        private string Task(string key,string label)=>context.Flag(key)?"<color=#68D2C9>[+] "+label+"</color>":"[ ] "+label;
        private void LateUpdate()
        {
            if(!saves.Ready)return;
            objectives.text="RESTORE INSTALLATION ACCESS\n"+Task("power_online","Restore control-room power")+"\n"+Task("coolant_drained","Drain the coolant depot")+"\n"+Task("transfer_secured","Secure the control room");
            if(context.Restoring){previousStatus=saves.LastStatus;statusUntil=0;}
            else if(previousStatus!=saves.LastStatus){previousStatus=saves.LastStatus;statusUntil=Time.unscaledTime+4;}
            status.text=Time.unscaledTime<statusUntil?previousStatus:"F5 SAVE  /  F9 LOAD  /  ESC PAUSE";
        }
    }
}
