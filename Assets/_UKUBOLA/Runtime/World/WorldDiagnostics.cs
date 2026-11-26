using UnityEngine;
using UnityEngine.InputSystem;
namespace Ukubola.World
{
    [DefaultExecutionOrder(100)]
    public sealed class WorldDiagnostics : MonoBehaviour
    {
        [SerializeField] private PickupDiagnostics source;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private Door[] doors;
        [SerializeField] private WorldSwitch[] switches;
        [SerializeField] private Lift lift;
        [SerializeField] private DamageZone zone;
        [SerializeField] private Teleport teleport;
        [SerializeField] private SecretTrigger secret;
        [SerializeField] private ExplosiveBarrel[] barrels;
        public PickupDiagnostics Source=>source; public PlayerInteractor Interactor=>interactor; public Door[] Doors=>doors; public WorldSwitch[] Switches=>switches; public Lift Lift=>lift; public DamageZone Zone=>zone; public Teleport Teleport=>teleport; public SecretTrigger Secret=>secret; public ExplosiveBarrel[] Barrels=>barrels;
        public void Configure(PickupDiagnostics s,PlayerInteractor p,Door[] d,WorldSwitch[] sw,Lift l,DamageZone z,Teleport t,SecretTrigger st,ExplosiveBarrel[] b){source=s;interactor=p;doors=d;switches=sw;lift=l;zone=z;teleport=t;secret=st;barrels=b;}
        private void Update()
        {
            if(!Application.isFocused || Keyboard.current==null || !Keyboard.current.f9Key.wasPressedThisFrame)return;
            foreach(var d in doors)d.ResetDoor();foreach(var s in switches)s.ResetSwitch();lift.ResetLift();zone.ResetZone();teleport.ResetTeleport();secret.ResetSecret();foreach(var b in barrels)b.ResetBarrel();interactor.ResetMessage();Physics.SyncTransforms();
        }
        private void LateUpdate()
        {
            // PickupDiagnostics updates this same text earlier (execution order -50).
            var text=source.Readout; text.text=text.text.Replace("PICKUP LAB","WORLD LAB")+"\nSecrets: "+(secret.Found?"1":"0")+"/1 | "+interactor.LastMessage;
        }
    }
}
