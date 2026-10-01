using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class MeleeEnemyController : EnemyControllerBase
    {
        [SerializeField] private float telegraphDuration = 0.28f;

        protected override void AttackTarget()
        {
            MoveHorizontally(0f);
            BeginAnticipation(telegraphDuration);
            EnterCooldown(definition.AttackCooldown + telegraphDuration);
            Invoke(nameof(ExecuteSlash), telegraphDuration);
        }

        private void ExecuteSlash()
        {
            if(!CanAct())return;
            ReleaseAttack();
            Bable.CombatAudio.Attack(gameObject);
            var origin = GetAttackOrigin();
            origin.x = transform.position.x + FacingSign * Mathf.Max(0.6f, definition.AttackRange * 0.55f);
            var hit = TryDamageCircle(origin, definition.AttackRange, definition.ContactDamage, new Vector2(FacingSign * 2f, 0f));
        }
    }
}
