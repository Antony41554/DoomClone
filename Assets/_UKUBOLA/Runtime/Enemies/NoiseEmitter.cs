using UnityEngine;
using Ukubola.Combat;
namespace Ukubola.Enemies
{
    // Observe successful shots without modifying Phase 05. Polling sees the pulse by next frame.
    public sealed class NoiseEmitter : MonoBehaviour
    {
        [SerializeField] private WeaponController weapons;
        private int previous;
        public bool CanHear(Vector3 listener,float range){var sectors=GetComponent<SectorNoise>();return sectors?sectors.CanReach(Position,listener,range):Vector3.Distance(listener,Position)<=range;}
        public WeaponController Weapons => weapons;
        public int Sequence { get; private set; }
        public Vector3 Position { get; private set; }
        public void Configure(WeaponController value) => weapons=value;
        private void Update()
        { if(!weapons) return; if(weapons.Shots>previous) { Position=weapons.transform.position; Sequence++; } previous=weapons.Shots; }
        public void ResetPulse() { previous=weapons ? weapons.Shots : 0; Sequence=0; }
    }
}
