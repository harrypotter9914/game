using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Combat
{
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 6;
        [SerializeField] private TeamAlignment alignment = TeamAlignment.Neutral;
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private float invulnerabilityDuration = 0.2f;

        private float invulnerableUntil;
        private SpriteRenderer spriteRenderer;
        private Color defaultColor = Color.white;

        public int CurrentHealth { get; private set; }
        public bool Invincible { get; set; }
        public int MaxHealth => maxHealth;
        public TeamAlignment Alignment => alignment;

        public event System.Action<int, int> Changed;
        public event System.Action<DamageInfo> Damaged;
        public event System.Action Died;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }
        }

        public void Configure(int newMaxHealth, int? currentHealthOverride = null)
        {
            maxHealth = Mathf.Max(1, newMaxHealth);
            CurrentHealth = currentHealthOverride.HasValue ? Mathf.Clamp(currentHealthOverride.Value, 0, maxHealth) : Mathf.Min(CurrentHealth, maxHealth);
            Changed?.Invoke(CurrentHealth, maxHealth);
        }

        public bool CanReceiveDamage(TeamAlignment sourceTeam)
        {
            return sourceTeam != alignment;
        }

        public void ReceiveDamage(DamageInfo damage)
        {
            if (Invincible || Time.timeScale == 0f || Time.time < invulnerableUntil)
            {
                return;
            }

            ApplyDamage(damage.Amount, damage);
        }

        public void ApplyDamage(int amount)
        {
            ApplyDamage(amount, new DamageInfo(amount, transform.position, Vector2.zero, null, TeamAlignment.Neutral));
        }

        public void ApplyDamage(int amount, DamageInfo damage)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            invulnerableUntil = Time.time + invulnerabilityDuration;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Damaged?.Invoke(damage);
            Changed?.Invoke(CurrentHealth, maxHealth);
            FlashDamage();
            var col=GetComponent<Collider2D>();
            Vector2 point=col!=null?col.ClosestPoint(damage.Point):(Vector2)transform.position;
            Bable.CombatImpact.Spawn(point, ((Vector2)transform.position-damage.Point).normalized,false);

            if (alignment == TeamAlignment.Player)
            {
                var session = Babel.Runtime.Core.GameSession.Instance;
                if (session != null && session.DamageReflectAmount > 0f)
                {
                    // The design specifies a retaliation around the victim, including
                    // projectile hits. Do not depend on a projectile retaining its owner.
                    var touched = new System.Collections.Generic.HashSet<HealthComponent>();
                    foreach (var nearby in Physics2D.OverlapCircleAll(transform.position, session.DamageReflectRadius))
                    {
                        var reflected = nearby.GetComponentInParent<HealthComponent>();
                        if (reflected == null || reflected == this || reflected.Alignment != TeamAlignment.Enemy || !touched.Add(reflected)) continue;
                        bool blocked = false;
                        foreach (var hit in Physics2D.LinecastAll(transform.position, nearby.bounds.center))
                            if (Bable.TerrainMotion.Solid(hit.collider)) { blocked = true; break; }
                        if (!blocked)
                            reflected.ReceiveDamage(new DamageInfo(Mathf.Max(1, Mathf.RoundToInt(amount * session.DamageReflectAmount)), nearby.bounds.center, Vector2.zero, gameObject, alignment));
                    }
                }
            }

            if (CurrentHealth == 0)
            {
                Died?.Invoke();
                if (destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            Changed?.Invoke(CurrentHealth, maxHealth);
        }

        private void FlashDamage()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = Color.white;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.08f);
        }

        private void RestoreColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
        }
    }
}
