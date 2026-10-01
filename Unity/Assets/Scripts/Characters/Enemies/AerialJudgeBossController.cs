using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class AerialJudgeBossController : EnemyControllerBase
    {
        [SerializeField] private float leapForceY = 9f;
        [SerializeField] private float leapForceX = 5f;
        [SerializeField] private float telegraphDuration = 0.45f;
        [SerializeField] private float upperSlashHeight = 1.3f;

        protected override void AttackTarget()
        {
            MoveHorizontally(0f);
            var delta = GetTargetDelta();
            var airborneDive = delta.y < -0.25f || Mathf.Abs(delta.x) > 1.4f;
            BeginAnticipation(telegraphDuration);
            EnterCooldown(definition.AttackCooldown + telegraphDuration + 0.35f);
            if (airborneDive)
            {
                Invoke(nameof(ExecuteDiveSlash), telegraphDuration);
            }
            else
            {
                Invoke(nameof(ExecuteUpperSlash), telegraphDuration);
            }
        }

        private void ExecuteDiveSlash()
        {
            if (TargetTransform == null)
            {
                return;
            }

            var delta = GetTargetDelta();
            FaceTarget(delta.x);
            Body.linearVelocity = new Vector2(FacingSign * leapForceX, leapForceY);
            var center = (Vector2)transform.position + new Vector2(FacingSign * 0.8f, -0.9f);
            var hitCount = TryDamageBox(center, new Vector2(1.8f, 1.5f), definition.ContactDamage, new Vector2(FacingSign * 3f, -1f));
        }

        private void ExecuteUpperSlash()
        {
            var center = (Vector2)transform.position + new Vector2(FacingSign * 0.7f, upperSlashHeight);
            var hitCount = TryDamageBox(center, new Vector2(1.6f, 1.2f), definition.ContactDamage, new Vector2(FacingSign * 3f, 2f));
        }
    }
}
