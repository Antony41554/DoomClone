using UnityEngine;
namespace Ukubola.Enemies
{
    public enum EnemyAttackKind { Hitscan, Projectile, Melee }
    public enum EnemyState { Dormant, Chase, Attack, Pain, Dead }
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private EnemyAttackKind attackKind;
        [SerializeField] private int health = 20, damage = 8;
        [SerializeField] private float speed = 2.5f, sight = 18f, hearing = 14f, attackRange = 9f, cooldown = 1.4f, windup = .35f, pain = .18f;
        public EnemyAttackKind AttackKind => attackKind;
        public int Health => health; public int Damage => damage;
        public float Speed => speed; public float Sight => sight; public float Hearing => hearing; public float AttackRange => attackRange;
        public float Cooldown => cooldown; public float Windup => windup; public float Pain => pain;
        public bool IsValid => (attackKind == EnemyAttackKind.Hitscan || attackKind == EnemyAttackKind.Projectile || attackKind == EnemyAttackKind.Melee) && health > 0 && damage > 0 && speed > 0 && sight >= attackRange && hearing > 0 && (attackKind==EnemyAttackKind.Melee?attackRange>0:attackRange>2) && windup > 0 && cooldown > windup && pain > 0;
        public void Configure(EnemyAttackKind kind, int hp, int dmg, float movement, float range, float interval)
        { attackKind=kind; health=hp; damage=dmg; speed=movement; attackRange=range; cooldown=interval; }
    }
}
