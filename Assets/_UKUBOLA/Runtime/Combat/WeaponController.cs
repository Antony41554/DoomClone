using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Ukubola.Player;

namespace Ukubola.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerInventory), typeof(Health))]
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition pistol, shotgun;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private WeaponView weaponView;
        [SerializeField] private LayerMask hitMask, targetMask;
        [SerializeField] private WeaponDefinition[] advanced = Array.Empty<WeaponDefinition>();
        [SerializeField] private Projectile advancedProjectile;
        public WeaponDefinition[] Advanced => advanced;
        public Projectile AdvancedProjectile => advancedProjectile;
        public void ConfigureAdvanced(WeaponDefinition[] definitions, Projectile prefab){advanced=definitions;advancedProjectile=prefab;}
        private WeaponDefinition FindWeapon(WeaponId id){if(id==WeaponId.Pistol)return pistol;if(id==WeaponId.Shotgun)return shotgun;foreach(var w in advanced??Array.Empty<WeaponDefinition>())if(w&&w.Weapon==id)return w;return null;}
        private PlayerInputReader input;
        private PlayerInventory inventory;
        private Health health;
        private PlayerMotor motor;
        private InputActionAsset actions;
        private InputAction fire, slot;
        private int queuedSlot;
        private bool armed;
        private double nextShot, flashUntil;
        private WeaponDefinition selected;
        private System.Random random;
        public WeaponDefinition Pistol => pistol;
        public WeaponDefinition Shotgun => shotgun;
        public Camera AimCamera => aimCamera;
        public WeaponView View => weaponView;
        public int HitMask => hitMask.value;
        public int TargetMask => targetMask.value;
        public WeaponDefinition Selected => selected;
        public int Shots { get; private set; }
        public int DamageHits { get; private set; }
        public string LastResult { get; private set; } = "Ready";
        public float RemainingCooldown=>(float)Math.Max(0,nextShot-Time.timeAsDouble);
        public void SetQaSeed(int seed)
        {if(!QaEnvironment.Active)throw new InvalidOperationException("Weapon test seed requires isolated QA.");random=new System.Random(seed);}
        public void RestoreCooldown(float value)
        {
            if(!Ukubola.Persistence.SaveData.Range(value,0,60))throw new ArgumentOutOfRangeException(nameof(value));
            nextShot=Time.timeAsDouble+value;flashUntil=0;armed=false;queuedSlot=0;
            if(weaponView)weaponView.ResetTransient();
        }
        public bool CanControl => input && input.isActiveAndEnabled && input.IsReady && input.Captured && health && !health.IsDead && (Application.isFocused||input.HasQaCommands) && Time.timeScale > 0f;
        public void Configure(WeaponDefinition a, WeaponDefinition b, Camera camera, WeaponView view, int mask, int targets)
        { pistol = a; shotgun = b; aimCamera = camera; weaponView = view; hitMask = mask; targetMask = targets; }
        private void Awake()
        { input = GetComponent<PlayerInputReader>(); inventory = GetComponent<PlayerInventory>(); health = GetComponent<Health>(); motor = GetComponent<PlayerMotor>(); random = new System.Random(); selected = pistol; }
        private void OnEnable()
        {
            try
            {
                if (!pistol || !shotgun || !pistol.IsRuleValid || !shotgun.IsRuleValid || !aimCamera || !weaponView || !input.Actions) throw new InvalidOperationException("Invalid weapon references.");
                actions = Instantiate(input.Actions); actions.Disable();
                var map = actions.FindActionMap("Gameplay", true);
                fire = map.FindAction("Fire", true); slot = map.FindAction("WeaponSlot", true);
                slot.performed += OnSlot; fire.Enable(); slot.Enable();
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P05] Weapon input: " + ex.Message, this); enabled = false; }
        }
        private void OnSlot(InputAction.CallbackContext context)
        {
            float value = context.ReadValue<float>();
            if (value >= .5f && value <= 7.5f) queuedSlot = Mathf.RoundToInt(value);
        }
        private void OnDisable()
        {
            if (slot != null) slot.performed -= OnSlot;
            if (actions) { actions.Disable(); Destroy(actions); }
            actions = null; fire = slot = null; armed = false; queuedSlot = 0;
            if (weaponView) weaponView.Present(null, false, false, 0f, 0f);
        }
        public bool Select(WeaponDefinition weapon)
        {
            if (!weapon || (FindWeapon(weapon.Weapon) != weapon) || !inventory.HasWeapon(weapon.Weapon)) return false;
            if (selected == weapon) return true;
            selected = weapon; flashUntil = 0; return true; // Keep cooldown across switches.
        }
        private void LateUpdate()
        {
            // PlayerMotor.Update has already processed capture/escape and applied horizontal look.
            double now = Time.timeAsDouble;
            if (!CanControl || fire == null) { armed = false; queuedSlot = 0; }
            else
            {
                if(input.HasQaCommands&&input.QaCommands.WeaponSlot>0)queuedSlot=input.QaCommands.WeaponSlot;
                if(queuedSlot==1){var saw=FindWeapon(WeaponId.Chainsaw);var fist=FindWeapon(WeaponId.Fist);if(selected==saw||!Select(saw))Select(fist);}
                if (queuedSlot == 2) Select(pistol);
                if (queuedSlot == 3) Select(shotgun);
                if(queuedSlot==4)Select(FindWeapon(WeaponId.Chaingun));
                if(queuedSlot==5)Select(FindWeapon(WeaponId.RocketLauncher));
                if(queuedSlot==6)Select(FindWeapon(WeaponId.PlasmaRifle));
                if(queuedSlot==7)Select(FindWeapon(WeaponId.EnergyCannon));
                queuedSlot = 0;
                bool pressed=input.HasQaCommands?input.QaCommands.FireHeld:fire.IsPressed();
                if (!pressed) armed = true; // First capture click is never a shot.
                else if (armed && HitscanAttack.Ready(now, nextShot)) Shoot(now);
            }
            if (!selected || !inventory.HasWeapon(selected.Weapon)) selected = pistol;
            weaponView.Present(selected, !health.IsDead, now < flashUntil, motor ? motor.HorizontalVelocity.magnitude : 0f, Time.time);
        }
        private void Shoot(double now)
        {
            if (!selected || !inventory.HasWeapon(selected.Weapon)) return;
            nextShot = HitscanAttack.NextShot(now, selected.Interval);
            if(selected.Mode==WeaponMode.Projectile&&!advancedProjectile){LastResult="Missing projectile";return;}
            if (selected.UsesAmmo && !inventory.TryConsumeAmmo(selected.Ammo, selected.AmmoCost)) { LastResult = "No ammo"; return; }
            Shots++; flashUntil = now + .09;
            Vector3 origin = aimCamera.transform.position-(selected.Mode==WeaponMode.Melee?Vector3.up*.55f:Vector3.zero);
            Ukubola.Release.ReleasePresentation.Shot(this,origin);
            Vector3 aim = AutoAim.Resolve(origin, aimCamera.transform.forward, selected.Range, HitMask, TargetMask, gameObject);
            if(selected.Mode==WeaponMode.Projectile){var p=Instantiate(advancedProjectile,origin,Quaternion.identity);p.LaunchAdvanced(gameObject,aim,HitMask,selected.ProjectileSpeed,selected.Range,selected.FixedDamage,selected.BlastRadius);p.PresentAdvanced(selected.Weapon==WeaponId.EnergyCannon?new Color(.3f,1f,.25f):selected.Weapon==WeaponId.PlasmaRifle?new Color(.2f,.75f,1f):new Color(1f,.5f,.15f),selected.Weapon==WeaponId.EnergyCannon?1.7f:selected.Weapon==WeaponId.PlasmaRifle?.65f:1f);LastResult="Projectile launched";return;}
            int applied = 0, blocked = 0;
            for (int i = 0; i < selected.Pellets; i++)
            {
                var direction = HitscanAttack.SpreadDirection(aim, HitscanAttack.RollSpread(random, selected.Spread));
                if (!HitscanAttack.FirstHit(origin, direction, selected.Range, HitMask, gameObject, out var hit)) continue;
                var target = HitscanAttack.Damageable(hit.collider);
                Ukubola.Release.ReleasePresentation.Impact(hit.point,hit.normal,HitscanAttack.Live(target));
                if (HitscanAttack.Live(target) && target.TakeDamage(new DamageInfo(AttackDamage(), gameObject, selected.Mode==WeaponMode.Melee?DamageType.Melee:DamageType.Hitscan, hit.point)).Applied) applied++;
                else blocked++;
            }
            DamageHits += applied; LastResult = $"Shot {Shots}: {applied} damaging rays / {blocked} solid hits";
        }
        private int AttackDamage(){int amount=selected.FixedDamage>0?selected.FixedDamage:HitscanAttack.RollDamage(random);var power=GetComponent<Powerup>();return selected.Weapon==WeaponId.Fist&&power&&power.Berserk?amount*4:amount;}
        public Projectile LaunchDiagnostic(Projectile prefab)
        {
            if (!CanControl || !prefab) return null;
            var origin = aimCamera.transform.position;
            var aim = AutoAim.Resolve(origin, aimCamera.transform.forward, 30f, HitMask, TargetMask, gameObject);
            var projectile = Instantiate(prefab, origin, Quaternion.identity);
            projectile.Launch(gameObject, aim, HitMask, 12f, 30f, 25);
            LastResult = "Diagnostic projectile launched"; return projectile;
        }
        public void ResetDiagnostic()
        { selected = pistol; nextShot = flashUntil = 0; armed = false; queuedSlot = 0; Shots = DamageHits = 0; LastResult = "Ready"; }
    }
}
