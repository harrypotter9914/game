using Babel.Runtime.Combat;
using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class ShieldSentinelController : EnemyControllerBase
    {
        [SerializeField] private float telegraphDuration = 0.5f;
        private float exposedUntil;

        public override bool TryReceiveDamage(DamageInfo damage)
        {
            if (damage.Source != null)
            {
                var attackerDelta = damage.Source.transform.position.x - transform.position.x;
                var frontHit = attackerDelta * FacingSign > 0f;
                if (frontHit && Time.time >= exposedUntil)
                {
                    GetComponent<Bable.CharacterPresentation>()?.Act("block",.3f);
                    CancelInvoke();EnterCooldown(.45f);
                    Bable.CombatImpact.Spawn(CombatOrigin+Vector2.right*FacingSign*.65f,Vector2.right*FacingSign,true);
                    Bable.CombatAudio.Play("shield_block",transform.position,.6f);
                    return false;
                }
            }

            return base.TryReceiveDamage(damage);
        }

        protected override void AttackTarget()
        {
            MoveHorizontally(0f);
            BeginAnticipation(telegraphDuration);
            EnterCooldown(definition.AttackCooldown + telegraphDuration);
            Invoke(nameof(ExecuteShieldBash), telegraphDuration);
        }

        private void ExecuteShieldBash()
        {
            if(!CanAct())return;
            ReleaseAttack();
            Bable.CombatAudio.Attack(gameObject);

            exposedUntil = Time.time + 1f;
            var origin = GetAttackOrigin();
            origin.x = transform.position.x + FacingSign * Mathf.Max(0.75f, definition.AttackRange * 0.55f);
            var hits = TryDamageCircle(origin, definition.AttackRange + 0.15f, definition.ContactDamage, new Vector2(FacingSign * 3f, 0f));
        }
    }
}
