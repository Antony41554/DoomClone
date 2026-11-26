using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.Enemies;

namespace Ukubola.World
{
    [DefaultExecutionOrder(-50)]
    public sealed class PickupDiagnostics : MonoBehaviour
    {
        [SerializeField] private Pickup[] pickups;
        [SerializeField] private EnemyBrain[] enemies;
        [SerializeField] private Health player;
        [SerializeField] private WeaponController weapons;
        [SerializeField] private NoiseEmitter noise;
        [SerializeField] private PlayerSpawn spawn;
        [SerializeField] private Text readout;
        private bool paused = true;
        private string last = "Walk over items";
        public Pickup[] Pickups => pickups; public EnemyBrain[] Enemies => enemies; public Health Player => player; public WeaponController Weapons => weapons; public NoiseEmitter Noise => noise; public PlayerSpawn Spawn => spawn; public Text Readout => readout;
        public void Configure(Pickup[] items, EnemyBrain[] actors, Health hp, WeaponController guns, NoiseEmitter emitter, PlayerSpawn start, Text text)
        { pickups=items; enemies=actors; player=hp; weapons=guns; noise=emitter; spawn=start; readout=text; }
        private void OnEnable() { if (pickups != null) foreach (var item in pickups) if (item) item.Taken += OnTaken; }
        private void OnDisable() { if (pickups != null) foreach (var item in pickups) if (item) item.Taken -= OnTaken; }
        private void Start() { foreach (var enemy in enemies) enemy.Suspended = paused; }
        private void OnTaken(Pickup item) { last = "Picked up: " + item.Definition.DisplayName; Debug.Log("[UKUBOLA P07] " + last, item); }
        private void ResetLab()
        {
            player.ResetState();
            var controller=player.GetComponent<CharacterController>(); var motor=player.GetComponent<PlayerMotor>();
            motor.enabled=false; controller.enabled=false;
            player.transform.SetPositionAndRotation(spawn.Position+Vector3.up*.03f,spawn.Rotation);
            controller.enabled=true; motor.enabled=true;
            player.GetComponent<PlayerInventory>().ResetState(); weapons.ResetDiagnostic(); noise.ResetPulse();
            foreach(var item in pickups) item.ResetPickup();
            foreach(var enemy in enemies) { enemy.ResetEnemy(); enemy.Suspended=paused; }
            Physics.SyncTransforms(); last="Reset complete";
        }
        private void Update()
        {
            var k=Keyboard.current; if(k==null || !Application.isFocused)return;
            if(k.f8Key.wasPressedThisFrame) { paused=!paused; foreach(var enemy in enemies){enemy.Suspended=paused;if(paused)enemy.GetComponent<EnemyAttack>().ClearProjectiles();} }
            if(k.f9Key.wasPressedThisFrame) ResetLab();
            if(k.f10Key.wasPressedThisFrame) readout.transform.parent.gameObject.SetActive(!readout.transform.parent.gameObject.activeSelf);
            if(player.IsDead)return;
            if(k.f2Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(40));
            if(k.f7Key.wasPressedThisFrame) { var bag=player.GetComponent<PlayerInventory>(); var weapon=weapons.Selected; var ammo=weapon ? weapon.Ammo : AmmoType.Bullets; int count=bag.Count(ammo); if(count>0)bag.TryConsumeAmmo(ammo,count); }
        }
        private void LateUpdate()
        {
            var bag=player.GetComponent<PlayerInventory>(); var armor=player.Armor; int taken=0; foreach(var item in pickups) if(item.Collected)taken++;
            readout.text=$"PICKUP LAB | HP {player.Current} | ARM {armor.Points} {armor.Kind} | AI {(paused?"OFF":"ON")}\n"+
                $"B {bag.Count(AmmoType.Bullets)}/{bag.Capacity(AmmoType.Bullets)} S {bag.Count(AmmoType.Shells)}/{bag.Capacity(AmmoType.Shells)} R {bag.Count(AmmoType.Rockets)} C {bag.Count(AmmoType.Cells)}\n"+
                $"Shotgun {(bag.HasWeapon(WeaponId.Shotgun)?"YES":"NO")} | Backpack {(bag.HasBackpack?"YES":"NO")} | Items {taken}/{pickups.Length}\n"+
                $"Keys: {bag.Keys}\n{last}\nF2 damage | F7 empty | F8 AI | F9 reset | F10 panel";
        }
    }
}
