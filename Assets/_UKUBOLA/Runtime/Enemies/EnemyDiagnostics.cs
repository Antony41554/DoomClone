using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
namespace Ukubola.Enemies
{
    [DefaultExecutionOrder(-50)]
    public sealed class EnemyDiagnostics : MonoBehaviour
    {
        [SerializeField] private EnemyBrain[] enemies;
        [SerializeField] private Health player;
        [SerializeField] private WeaponController weapons;
        [SerializeField] private NoiseEmitter noise;
        [SerializeField] private PlayerSpawn spawn;
        [SerializeField] private Text readout;
        private bool paused;
        public EnemyBrain[] Enemies=>enemies; public Health Player=>player; public WeaponController Weapons=>weapons; public NoiseEmitter Noise=>noise; public PlayerSpawn Spawn=>spawn; public Text Readout=>readout;
        public void Configure(EnemyBrain[] actors,Health hp,WeaponController gun,NoiseEmitter emitter,PlayerSpawn start,Text text)
        {enemies=actors;player=hp;weapons=gun;noise=emitter;spawn=start;readout=text;}
        private void Start() { FillInventory(); }
        private void FillInventory() {var bag=player.GetComponent<PlayerInventory>();bag.ResetState();bag.TryGrantWeapon(WeaponId.Shotgun);bag.AddAmmo(AmmoType.Shells,20);weapons.ResetDiagnostic();noise.ResetPulse();}
        private void ResetLab()
        {
            player.ResetState();var cc=player.GetComponent<CharacterController>();var motor=player.GetComponent<PlayerMotor>();motor.enabled=false;cc.enabled=false;
            player.transform.SetPositionAndRotation(spawn.Position+Vector3.up*.03f,spawn.Rotation);cc.enabled=true;motor.enabled=true;
            FillInventory();foreach(var e in enemies) {e.ResetEnemy();e.Suspended=paused;} Physics.SyncTransforms();
        }
        private void Update()
        {
            var k=Keyboard.current;if(k==null || !Application.isFocused)return;
            if(k.f8Key.wasPressedThisFrame){paused=!paused;foreach(var e in enemies){e.Suspended=paused;if(paused)e.GetComponent<EnemyAttack>().ClearProjectiles();}}
            if(k.f9Key.wasPressedThisFrame)ResetLab();
            if(k.f10Key.wasPressedThisFrame)readout.transform.parent.gameObject.SetActive(!readout.transform.parent.gameObject.activeSelf);
            if(k.f6Key.wasPressedThisFrame&&!player.IsDead){var bag=player.GetComponent<PlayerInventory>();bag.AddAmmo(AmmoType.Bullets,40);bag.AddAmmo(AmmoType.Shells,20);}
        }
        private void LateUpdate()
        {
            var bag=player.GetComponent<PlayerInventory>();string text=$"ENEMY LAB | HP {player.Current} | {(paused?"AI STOPPED":"AI ON")}\nBullets {bag.Count(AmmoType.Bullets)} | Shells {bag.Count(AmmoType.Shells)}\n";
            foreach(var e in enemies)text+=$"{e.name}: {e.State} HP {e.GetComponent<Health>().Current} ATK {e.Attacks}\n";
            readout.text=text+"F6 ammo | F8 AI toggle | F9 reset | F10 panel";
        }
    }
}
