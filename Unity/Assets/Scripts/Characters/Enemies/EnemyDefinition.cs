using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    [CreateAssetMenu(menuName = "Babel/Characters/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float chaseSpeed = 2.5f;
        [SerializeField] private float chaseRange = 5f;
        [SerializeField] private float attackRange = 1.25f;
        [SerializeField] private float verticalAggroTolerance = 1.5f;
        [SerializeField] private float attackCooldown = 1.25f;
        [SerializeField] private bool blocksFrontAttacks;
        [SerializeField] private bool usesProjectileAttack;
        [SerializeField] private bool isBoss;

        public int MaxHealth => maxHealth;
        public int ContactDamage => contactDamage;
        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float ChaseRange => chaseRange;
        public float AttackRange => attackRange;
        public float VerticalAggroTolerance => verticalAggroTolerance;
        public float AttackCooldown => attackCooldown;
        public bool BlocksFrontAttacks => blocksFrontAttacks;
        public bool UsesProjectileAttack => usesProjectileAttack;
        public bool IsBoss => isBoss;
    }
}
