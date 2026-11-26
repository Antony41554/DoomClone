using System;
using UnityEngine;
using UnityEngine.AI;
using Ukubola.Persistence;
using Ukubola.Player;

namespace Ukubola.World
{
    /// <summary>Rigid door, lift, hydraulic platform or bridge on a declared rail.</summary>
    public sealed class WorldMover : PersistentBehaviour, IWorldActionTarget, IWorldActionParameters, IInteractable
    {
        [Serializable] private sealed class Data
        {
            public Vector3 from;
            public int target;
            public float fraction, hold;
            public bool powered;
        }
        [SerializeField] private Transform panel;
        [SerializeField] private BoxCollider solid;
        [SerializeField] private Vector3[] stops;
        [SerializeField] private float speed=1.5f, autoClose;
        [SerializeField] private bool carryPlayer, protectClosing=true, powered=true;
        [SerializeField] private KeyId requiredKey;
        [SerializeField] private bool remoteOnly;
        [SerializeField] private NavMeshObstacle doorway;
        private Vector3 from;
        private int target;
        private float fraction=1, hold;
        private readonly Collider[] contacts=new Collider[32];
        private readonly RaycastHit[] clearance=new RaycastHit[32];
        public override string StateKind=>"mover";
        public override int RestoreOrder=>50;
        public float Fraction=>fraction;
        public int TargetStop=>target;
        public bool Moving=>fraction<1;
        public bool Powered=>powered;
        public bool AtFinalStop=>!Moving&&target==stops.Length-1;
        public Transform Panel=>panel;
        public event Action Changed;
        public void Configure(Transform movingPanel,BoxCollider collision,Vector3[] positions,float metresPerSecond,bool transport,bool obstructionProtection,KeyId key=KeyId.None,float closeAfter=0,NavMeshObstacle obstacle=null,bool externalControlOnly=false)
        {
            panel=movingPanel;solid=collision;stops=positions;speed=metresPerSecond;carryPlayer=transport;
            protectClosing=obstructionProtection;requiredKey=key;autoClose=closeAfter;doorway=obstacle;remoteOnly=externalControlOnly;
            target=0;fraction=1;from=stops[0];hold=0;powered=true;ValidateConfiguration();ApplyPose();
        }
        public void ValidateConfiguration()
        {
            if(!panel||!solid||solid.transform!=panel||stops==null||stops.Length<2||stops.Length>16||!MissionDefinition.Finite(speed)||speed<=0||!MissionDefinition.Finite(autoClose)||autoClose<0)
                throw new InvalidOperationException("Invalid mover configuration: "+name);
            for(int i=0;i<stops.Length;i++)
                if(!MissionDefinition.Finite(stops[i])||i>0&&Vector3.Distance(stops[i],stops[i-1])<.05f)throw new InvalidOperationException("Invalid mover stops: "+name);
            Vector3 axis=(stops[1]-stops[0]).normalized;
            if(Array.Exists(stops,p=>Vector3.Cross(p-stops[0],axis).magnitude>.001f))throw new InvalidOperationException("Mover stops must share a straight rail.");
        }
        private void Awake(){ValidateConfiguration();from=stops[0];target=0;fraction=1;ApplyPose();}
        private void FixedUpdate(){if(Time.timeScale>0)Tick(Time.fixedDeltaTime);}
        public void SetPower(bool value){powered=value;Changed?.Invoke();}
        public bool Accepts(WorldVerb verb)=>verb==WorldVerb.On||verb==WorldVerb.Off||verb==WorldVerb.Toggle||verb==WorldVerb.MoveToStop;
        public void ValidateAction(WorldVerb verb,float value)
        {
            if(!Accepts(verb)||!MissionDefinition.Finite(value))throw new InvalidOperationException("Invalid mover command.");
            int stop=Mathf.RoundToInt(value);
            if(verb==WorldVerb.MoveToStop&&(Mathf.Abs(value-Mathf.Round(value))>.0001f||stop<0||stop>=stops.Length))throw new InvalidOperationException("Mover command refers to an invalid stop.");
        }
        public void Execute(WorldVerb verb,float value)
        {
            ValidateAction(verb,value);
            int destination=verb==WorldVerb.On?stops.Length-1:verb==WorldVerb.Off?0:verb==WorldVerb.Toggle?(target==0?stops.Length-1:0):Mathf.RoundToInt(value);
            MoveTo(destination);
        }
        public string Interact(InteractionContext actor)
        {
            if(!actor.Health||actor.Health.IsDead)return "UNAVAILABLE";
            if(!powered)return "NO POWER";
            if(remoteOnly)return "REMOTE CONTROL";
            if(!actor.Inventory||!actor.Inventory.HasKeys(requiredKey))return requiredKey.ToString().ToUpperInvariant()+" REQUIRED";
            MoveTo(target==0?stops.Length-1:0);return carryPlayer?"PLATFORM CALLED":"ACCESS GRANTED";
        }
        public void MoveTo(int destination)
        {
            if(destination<0||destination>=stops.Length)throw new ArgumentOutOfRangeException(nameof(destination));
            if(!powered)return;
            if(target==destination&&Moving)return;
            if(Vector3.Distance(panel.localPosition,stops[destination])<.001f)
            {target=destination;fraction=1;from=stops[destination];hold=AtFinalStop?autoClose:0;ApplyPose();return;}
            from=panel.localPosition;target=destination;fraction=0;hold=0;UpdateNavigation();Changed?.Invoke();Emit(WorldSignal.Activated);
        }
        public void Tick(float seconds)
        {
            if(!MissionDefinition.Finite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!powered||seconds==0)return;
            if(!Moving)
            {
                if(hold>0){hold=Mathf.Max(0,hold-seconds);if(hold==0)MoveTo(0);}
                return;
            }
            float next=Mathf.Min(1,fraction+speed*seconds/Mathf.Max(.001f,Vector3.Distance(from,stops[target])));
            Vector3 local=Vector3.Lerp(from,stops[target],next);
            Vector3 delta=panel.parent?panel.parent.TransformVector(local-panel.localPosition):local-panel.localPosition;
            if(Application.isPlaying&&protectClosing&&!carryPlayer&&target==0&&Occupied(delta)){MoveTo(stops.Length-1);return;}
            int riders=Application.isPlaying&&carryPlayer?FindRiders():0;
            if(riders>0&&delta.y>0&&CeilingBlocked(riders,delta.y))return;
            if(Application.isPlaying&&carryPlayer&&delta.y<0&&PersonBelow(delta))return;
            fraction=next;ApplyPose();
            if(Application.isPlaying)
            {
                Physics.SyncTransforms();
                for(int i=0;i<riders;i++){var rider=contacts[i].GetComponentInParent<CharacterController>();if(rider&&rider.enabled)rider.Move(delta);}
            }
            Changed?.Invoke();
            if(!Moving){hold=AtFinalStop?autoClose:0;UpdateNavigation();Emit(WorldSignal.Arrived);}
        }
        private bool Occupied(Vector3 delta)
        {
            var bounds=solid.bounds;int count=Physics.OverlapBoxNonAlloc(bounds.center+delta*.5f,bounds.extents+new Vector3(Mathf.Abs(delta.x),Mathf.Abs(delta.y),Mathf.Abs(delta.z))*.5f,contacts,Quaternion.identity,LayerMask.GetMask("Player","Enemy"),QueryTriggerInteraction.Ignore);
            return count>0; // A full buffer is conservatively occupied as well.
        }
        private int FindRiders()
        {
            var bounds=solid.bounds;int count=Physics.OverlapBoxNonAlloc(new Vector3(bounds.center.x,bounds.max.y+.07f,bounds.center.z),new Vector3(bounds.extents.x+.02f,.12f,bounds.extents.z+.02f),contacts,Quaternion.identity,LayerMask.GetMask("Player"),QueryTriggerInteraction.Ignore);
            int valid=0;
            for(int i=0;i<count;i++)
            {
                var body=contacts[i].GetComponentInParent<CharacterController>();
                if(body&&body.enabled&&body.bounds.min.y>=bounds.max.y-.14f&&body.bounds.min.y<=bounds.max.y+.2f)contacts[valid++]=contacts[i];
            }
            return valid;
        }
        private bool CeilingBlocked(int riders,float distance)
        {
            for(int i=0;i<riders;i++)
            {
                var body=contacts[i].GetComponentInParent<CharacterController>();var b=body.bounds;float radius=Mathf.Min(b.extents.x,b.extents.z)*.95f;
                Vector3 low=b.center-Vector3.up*Mathf.Max(0,b.extents.y-radius),high=b.center+Vector3.up*Mathf.Max(0,b.extents.y-radius);
                int count=Physics.CapsuleCastNonAlloc(low,high,radius,Vector3.up,clearance,distance+.06f,LayerMask.GetMask("World"),QueryTriggerInteraction.Ignore);
                for(int h=0;h<count;h++)if(clearance[h].collider!=solid)return true;
                if(count==clearance.Length)return true;
            }
            return false;
        }
        private bool PersonBelow(Vector3 delta)
        {
            var b=solid.bounds;int count=Physics.OverlapBoxNonAlloc(b.center+delta*.5f,b.extents+Vector3.up*Mathf.Abs(delta.y)*.5f,overlapsBelow,Quaternion.identity,LayerMask.GetMask("Player"),QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var body=overlapsBelow[i].GetComponentInParent<CharacterController>();
                if(body&&body.enabled&&body.bounds.max.y<=b.min.y+.06f)return true;
            }
            return count==overlapsBelow.Length;
        }
        private readonly Collider[] overlapsBelow=new Collider[16];
        private void ApplyPose()
        {
            if(panel)panel.localPosition=Vector3.Lerp(from,stops[target],fraction);
            UpdateNavigation();
        }
        private void UpdateNavigation(){if(doorway)doorway.enabled=!AtFinalStop;}
        private bool OnRail(Vector3 point)
        {
            for(int i=1;i<stops.Length;i++)
            {
                Vector3 segment=stops[i]-stops[i-1];float along=Mathf.Clamp01(Vector3.Dot(point-stops[i-1],segment)/segment.sqrMagnitude);
                if(Vector3.Distance(point,stops[i-1]+segment*along)<.01f)return true;
            }
            return false;
        }
        public override EntitySnapshot CaptureEntity()=>State(new Data{from=from,target=target,fraction=fraction,hold=hold,powered=powered});
        public override void ValidateEntity(EntitySnapshot state)
        {
            var d=Read<Data>(state);
            if(d==null||!MissionDefinition.Finite(d.from)||!OnRail(d.from)||d.target<0||d.target>=stops.Length||!SaveData.Range(d.fraction,0,1)||!SaveData.Range(d.hold,0,autoClose))throw new InvalidOperationException("Invalid mover state: "+name);
        }
        public override void RestoreEntity(EntitySnapshot state)
        {ValidateEntity(state);var d=Read<Data>(state);from=d.from;target=d.target;fraction=d.fraction;hold=d.hold;powered=d.powered;ApplyPose();Changed?.Invoke();}
    }
}
