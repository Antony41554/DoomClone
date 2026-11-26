using System;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.World
{
    public sealed class WorldFlag : PersistentBehaviour,IWorldActionTarget
    {
        [Serializable] private sealed class Data{public bool value;}
        [SerializeField] private string key;
        [SerializeField] private bool initial;
        private bool value;
        public string Key=>key;
        public bool Value=>value;
        public override string StateKind=>"flag";
        public override int RestoreOrder=>0;
        public event Action Changed;
        public void Configure(string flag,bool start=false){key=flag;initial=start;value=start;ValidateConfiguration();}
        private void Awake(){ValidateConfiguration();value=initial;if(Context)Context.SetFlag(key,value);}
        public void ValidateConfiguration(){if(string.IsNullOrWhiteSpace(key)||key.Length>128)throw new InvalidOperationException("Invalid world flag.");}
        public bool Accepts(WorldVerb verb)=>verb==WorldVerb.On||verb==WorldVerb.Off||verb==WorldVerb.Toggle||verb==WorldVerb.AdvanceObjective;
        public void Execute(WorldVerb verb,float amount)
        {
            if(!Accepts(verb))throw new InvalidOperationException("Unsupported flag command.");
            bool next=verb==WorldVerb.Toggle?!value:verb!=WorldVerb.Off;if(next==value)return;
            value=next;if(Context)Context.SetFlag(key,value);Changed?.Invoke();Emit(WorldSignal.Activated);
        }
        public override EntitySnapshot CaptureEntity()=>State(new Data{value=value});
        public override void ValidateEntity(EntitySnapshot state){if(Read<Data>(state)==null)throw new InvalidOperationException("Missing flag state.");}
        public override void RestoreEntity(EntitySnapshot state){ValidateEntity(state);value=Read<Data>(state).value;if(Context)Context.SetFlag(key,value);Changed?.Invoke();}
    }
}
