using UnityEngine;

namespace Babel.Runtime.Characters.Player
{
    [CreateAssetMenu(menuName = "Babel/Characters/Player Definition", fileName = "PlayerDefinition")]
    public sealed class PlayerDefinition : ScriptableObject
    {
        [Header("Core Stats")]
        [SerializeField] private int baseMaxHealth = 6;
        [SerializeField] private int baseMaxMana = 3;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float groundAcceleration = 60f;
        [SerializeField] private float airAcceleration = 35f;
        [SerializeField] private float jumpImpulse = 16.4f;
        [SerializeField] private int extraAirJumps = 1;
        [SerializeField] private float wallSlideSpeed = 2f;
        [SerializeField] private float wallCheckDistance = 0.1f;
        [SerializeField] private float wallJumpHorizontalImpulse = 8f;
        [SerializeField] private float wallJumpVerticalImpulse = 16.9f;

        [Header("Melee")]
        [SerializeField] private int meleeDamage = 1;
        [SerializeField] private float meleeRange = 1.35f;
        [SerializeField] private float meleeCooldown = 0.3f;

        [Header("Shockwave")]
        [SerializeField] private int shockwaveDamage = 2;
        [SerializeField] private int shockwaveManaCost = 1;
        [SerializeField] private float shockwaveRange = 10f;
        [SerializeField] private float shockwaveDuration = 2.5f;
        [SerializeField] private float shockwaveTickInterval = .3f;
        [SerializeField] private float shockwaveCooldown = 1.6f;

        [Header("Crystal Dash")]
        [SerializeField] private int crystalDashDamage = 2;
        [SerializeField] private float crystalDashSpeed = 14f;
        [SerializeField] private float crystalDashDuration = 0.18f;
        [SerializeField] private float crystalDashRange = 4.2f;
        [SerializeField] private float crystalDashCooldown = 1.2f;

        public int BaseMaxHealth => baseMaxHealth;
        public int BaseMaxMana => baseMaxMana;
        public float MoveSpeed => moveSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float AirAcceleration => airAcceleration;
        public float JumpImpulse => jumpImpulse;
        public int ExtraAirJumps => extraAirJumps;
        public float WallSlideSpeed => wallSlideSpeed;
        public float WallCheckDistance => wallCheckDistance;
        public float WallJumpHorizontalImpulse => wallJumpHorizontalImpulse;
        public float WallJumpVerticalImpulse => wallJumpVerticalImpulse;
        public int MeleeDamage => meleeDamage;
        public float MeleeRange => meleeRange;
        public float MeleeCooldown => meleeCooldown;
        public int ShockwaveDamage => shockwaveDamage;
        public int ShockwaveManaCost => shockwaveManaCost;
        public float ShockwaveRange => shockwaveRange;
        public float ShockwaveDuration => shockwaveDuration;
        public float ShockwaveTickInterval => shockwaveTickInterval;
        public float ShockwaveCooldown => shockwaveCooldown;
        public int CrystalDashDamage => crystalDashDamage;
        public float CrystalDashSpeed => crystalDashSpeed;
        public float CrystalDashDuration => crystalDashDuration;
        public float CrystalDashRange => crystalDashRange;
        public float CrystalDashCooldown => crystalDashCooldown;
    }
}
