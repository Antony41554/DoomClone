using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using Ukubola.Campaign;
using Ukubola.Combat;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    public sealed class WorldSaveController:MonoBehaviour
    {
        [SerializeField] private MissionContext context;
        [SerializeField] private Health player;
        [SerializeField] private WeaponController weapons;
        [SerializeField] private bool preview=true;
        private SaveRepository repository;
        private SavedGear entry;
        private CampaignProgress prior=new CampaignProgress();
        private MissionSnapshot fresh;
        private float elapsed,exploreAt;
        private int difficulty=1;
        public bool Ready{get;private set;}
        public bool Busy{get;private set;}
        public bool CanSave=>Ready&&!Busy&&!player.IsDead;
        public string LastStatus{get;private set;}="";
        public MissionContext Context=>context;
        public Health Player=>player;
        public SaveRepository Repository=>repository;
        public void Configure(MissionContext world,Health life,WeaponController guns,bool isolatedPreview)
        {context=world;player=life;weapons=guns;preview=isolatedPreview;}
        private IEnumerator Start()
        {
            yield return null;yield return null;
            if(!context||!player||!weapons)throw new InvalidOperationException("Missing mission save references.");
            repository=QaEnvironment.Active?SaveRepository.Default:preview?new SaveRepository(Path.Combine(Application.persistentDataPath,"UKUBOLA","EvolutionPreview")):SaveRepository.Default;
            entry=SavedGear.From(Loadout.Fresh());context.RebuildRegistry();
            fresh=SaveRepository.Decode(SaveRepository.Encode(CaptureState()));Ready=true;
        }
        private void Update()
        {
            if(!Ready||Busy||Time.timeScale<=0)return;elapsed=Mathf.Min(SaveData.MaxSeconds,elapsed+Time.deltaTime);
            if(Time.time<exploreAt)return;exploreAt=Time.time+.2f;
            foreach(var sector in context.Definition.sectors.Where(s=>s.Contains(player.transform.position,.1f)))context.Explore(sector.key);
        }
        public MissionSnapshot Capture()
        {
            if(!CanSave)throw new InvalidOperationException("Cannot save now.");
            return CaptureState();
        }
        private MissionSnapshot CaptureState()
        {
            var power=player.GetComponent<Powerup>();var bag=player.GetComponent<PlayerInventory>();
            // Rollback can start from death. Keep its inventory in a valid living
            // loadout and separately restore the original death state on failure.
            var gear=new Loadout(Mathf.Max(1,player.Current),player.Armor.Points,player.Armor.Kind,bag.HasBackpack,
                Enumerable.Range(0,4).Select(i=>bag.Count((AmmoType)i)).ToArray(),Enumerable.Range(0,8).Select(i=>bag.HasWeapon((WeaponId)i)).ToArray(),weapons.Selected?weapons.Selected.Weapon:WeaponId.Pistol);
            var snapshot=new MissionSnapshot{missionId=context.Definition.missionId,revision=context.Definition.revision,difficulty=difficulty,utc=DateTime.UtcNow.ToString("O"),position=player.transform.position,yaw=Mathf.Repeat(player.transform.eulerAngles.y,360),elapsed=elapsed,gear=SavedGear.From(gear),entry=entry,prior=prior,entities=context.CaptureEntities(),flags=context.Flags,explored=context.Explored,keys=(int)bag.Keys,weaponCooldown=weapons.RemainingCooldown,invulnerability=power?power.Remaining(PowerupKind.Invulnerability):0,berserk=power?power.Remaining(PowerupKind.Berserk):0};
            snapshot.ValidateMission(context);return snapshot;
        }
        public bool Save()
        {
            try{repository.Write(Capture());LastStatus="GAME SAVED";return true;}
            catch(Exception error){LastStatus="SAVE FAILED: "+error.Message;return false;}
        }
        public IEnumerator LoadStored(Action<bool,string> done)
        {
            if(!Ready||Busy){done?.Invoke(false,"NOT READY");yield break;}
            if(!repository.TryRead(out var data,out bool backup,out string reason)){LastStatus=reason;done?.Invoke(false,reason);yield break;}
            yield return Restore(data,(ok,text)=>{LastStatus=ok?(backup?"BACKUP RESTORED":"GAME LOADED"):text;done?.Invoke(ok,LastStatus);});
        }
        public IEnumerator Restart(Action<bool,string> done)
        {
            if(fresh==null){done?.Invoke(false,"NOT READY");yield break;}
            yield return Restore(fresh,done);
        }
        public IEnumerator Restore(MissionSnapshot data,Action<bool,string> done)
        {
            if(!Ready||Busy){done?.Invoke(false,"NOT READY");yield break;}
            string invalid=null;
            try
            {
                data.ValidateMission(context);
                var selected=Definition((WeaponId)data.gear.selected);
                if(!selected||!selected.IsRuleValid)throw new InvalidDataException("Saved weapon unavailable.");
            }
            catch(Exception error){invalid=error.Message;}
            if(invalid!=null){done?.Invoke(false,invalid);yield break;}
            MissionSnapshot previous=null;
            try{previous=CaptureState();}catch(Exception error){invalid="Cannot capture rollback: "+error.Message;}
            if(invalid!=null){done?.Invoke(false,invalid);yield break;}
            Busy=true;var input=player.GetComponent<PlayerInputReader>();var motor=player.GetComponent<PlayerMotor>();var body=player.GetComponent<CharacterController>();
            bool inputEnabled=input.enabled,motorEnabled=motor.enabled,bodyEnabled=body.enabled,wasDead=player.IsDead;
            float previousScale=Time.timeScale;Time.timeScale=0;
            input.enabled=false;motor.enabled=false;body.enabled=false;
            Exception failure=null;
            void ApplyPlayer(MissionSnapshot snapshot)
            {
                player.ResetState();
                // Revived may re-enable the components through PlayerLife. Keep
                // controls and collision stopped until every restored field is set.
                input.enabled=false;motor.enabled=false;body.enabled=false;
                snapshot.gear.ToLoadout().Apply(player,weapons);
                var bag=player.GetComponent<PlayerInventory>();bag.ClearKeys();if(snapshot.keys!=0)bag.TryGrantKeys((KeyId)snapshot.keys);
                player.transform.SetPositionAndRotation(snapshot.position,Quaternion.Euler(0,snapshot.yaw,0));
                weapons.RestoreCooldown(snapshot.weaponCooldown);var power=player.GetComponent<Powerup>();power.Clear();
                if(snapshot.invulnerability>0)power.Grant(PowerupKind.Invulnerability,snapshot.invulnerability);
                if(snapshot.berserk>0)power.Grant(PowerupKind.Berserk,snapshot.berserk);
                elapsed=snapshot.elapsed;difficulty=snapshot.difficulty;entry=snapshot.entry;prior=snapshot.prior;
                Physics.SyncTransforms();
            }
            try
            {
                foreach(var projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))if(projectile.gameObject.scene==gameObject.scene)Destroy(projectile.gameObject);
                context.RestoreFlags(data.flags,data.explored);
                yield return context.RestoreEntitiesRoutine(data.entities,error=>failure=error);
                if(failure==null)
                {
                    try
                    {
                        if(!context.Flags.SequenceEqual(data.flags.OrderBy(s=>s,StringComparer.Ordinal)))
                            throw new InvalidDataException("Objective flags disagree with entity state.");
                        Vector3 center=data.position+Quaternion.Euler(0,data.yaw,0)*body.center;
                        float radius=body.radius*.95f,span=Mathf.Max(0,body.height*.5f-radius);
                        if(Physics.CheckCapsule(center-Vector3.up*span,center+Vector3.up*span,radius,LayerMask.GetMask("World"),QueryTriggerInteraction.Ignore))
                            throw new InvalidDataException("Saved player position is obstructed.");
                        ApplyPlayer(data);LastStatus="GAME RESTORED";
                    }
                    catch(Exception error){failure=error;}
                }
                if(failure!=null)
                {
                    context.RestoreFlags(previous.flags,previous.explored);Exception rollback=null;
                    yield return context.RestoreEntitiesRoutine(previous.entities,error=>rollback=error);
                    try{ApplyPlayer(previous);if(wasDead)player.RestoreSavedHealth(0);}
                    catch(Exception error){rollback=rollback==null?error:new AggregateException(rollback,error);}
                    if(rollback!=null)failure=new AggregateException("Load and rollback failed.",failure,rollback);
                    LastStatus="LOAD FAILED: "+failure.Message;
                }
            }
            finally
            {
                // A successful restore revives a dead player. The session controls cursor recapture.
                bool living=!player.IsDead;
                body.enabled=living&&(bodyEnabled||wasDead);motor.enabled=living&&(motorEnabled||wasDead);input.enabled=living&&(inputEnabled||wasDead);
                Time.timeScale=previousScale;Busy=false;
            }
            done?.Invoke(failure==null,LastStatus);
        }
        private WeaponDefinition Definition(WeaponId id)=>id==WeaponId.Pistol?weapons.Pistol:id==WeaponId.Shotgun?weapons.Shotgun:weapons.Advanced.FirstOrDefault(d=>d&&d.Weapon==id);
    }
}
