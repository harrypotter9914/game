using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class BurrowBossController : EnemyControllerBase
    {
        [SerializeField] private float burrowDelay = 0.65f;
        [SerializeField] private float recoverDelay = 0.9f;
        [SerializeField] private float emergeOffset = 1.4f;

        protected override void AttackTarget()
        {
            MoveHorizontally(0f);
            BeginAnticipation(burrowDelay);
            EnterCooldown(definition.AttackCooldown + burrowDelay + recoverDelay);
            Invoke(nameof(BurrowBurst), burrowDelay);
        }

        private void BurrowBurst()
        {
            if (TargetTransform == null)
            {
                return;
            }

            var targetPos = TargetTransform.position;
            var offset = targetPos.x > transform.position.x ? -emergeOffset : emergeOffset;
            transform.position = new Vector3(targetPos.x + offset, transform.position.y, transform.position.z);
            var delta = GetTargetDelta();
            FaceTarget(delta.x);
            var hitCount = TryDamageBox((Vector2)transform.position + new Vector2(FacingSign * 0.8f, 0f), new Vector2(2.2f, 1.6f), definition.ContactDamage, new Vector2(FacingSign * 4f, 0f));
        }
    }
}
