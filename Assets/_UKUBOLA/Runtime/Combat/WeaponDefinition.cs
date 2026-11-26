using UnityEngine;
using Ukubola.Player;

namespace Ukubola.Combat
{
    public enum WeaponMode { Hitscan, Melee, Projectile }
    [CreateAssetMenu(menuName = "UKUBOLA/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private WeaponId weapon = WeaponId.Pistol;
        [SerializeField] private AmmoType ammo = AmmoType.Bullets;
        [SerializeField] private int ammoCost = 1, pellets = 1;
        [SerializeField] private float interval = .4f, spread = 0f, range = 60f;
        [SerializeField] private Texture2D idle, firing;
        [SerializeField] private WeaponMode mode;
        [SerializeField] private int fixedDamage;
        [SerializeField] private float projectileSpeed = 12f, blastRadius;
        public WeaponMode Mode => mode;
        public int FixedDamage => fixedDamage;
        public float ProjectileSpeed => projectileSpeed;
        public float BlastRadius => blastRadius;
        public bool UsesAmmo => mode != WeaponMode.Melee;
        public WeaponId Weapon => weapon;
        public AmmoType Ammo => ammo;
        public int AmmoCost => ammoCost;
        public int Pellets => pellets;
        public float Interval => interval;
        public float Spread => spread;
        public float Range => range;
        public Texture2D Idle => idle;
        public Texture2D Firing => firing;
        public bool IsRuleValid => (mode == WeaponMode.Melee ? ammoCost == 0 : PlayerInventory.BaseCapacity(ammo) > 0 && ammoCost > 0) && System.Enum.IsDefined(typeof(WeaponMode),mode) && fixedDamage >= 0 && (mode != WeaponMode.Projectile || projectileSpeed > 0 && fixedDamage > 0 && blastRadius >= 0) && pellets >= 1 && pellets <= 32 &&
            interval >= .05f && interval <= 10f && spread >= 0f && spread <= 30f && range > 0f && range <= 200f;
        public bool IsValid => IsRuleValid && idle && firing;
        public void Configure(WeaponId id, AmmoType ammunition, int count, float delay, float degrees, Texture2D ready, Texture2D flash)
        { weapon = id; ammo = ammunition; ammoCost = 1; pellets = count; interval = delay; spread = degrees; range = 60f; idle = ready; firing = flash; mode=WeaponMode.Hitscan; fixedDamage=0; blastRadius=0; }
        public void ConfigureAdvanced(WeaponId id, AmmoType ammunition, WeaponMode attackMode, int cost, float delay, float distance, float degrees, int damage, float speed, float radius, Texture2D ready, Texture2D flash)
        {weapon=id;ammo=ammunition;mode=attackMode;ammoCost=cost;pellets=1;interval=delay;range=distance;spread=degrees;fixedDamage=damage;projectileSpeed=speed;blastRadius=radius;idle=ready;firing=flash;}
    }
}
