using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class ShockwaveBossController : EnemyControllerBase
    {
        [SerializeField] private float dashSpeed = 9f;
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private float telegraphDuration = 0.65f;

        private float dashUntil;
        private bool charging;
        private int attackIndex;

        protected override void TickAI()
        {
            if (charging || Time.time < dashUntil)
            {
                return;
            }

            var delta=GetTargetDelta();
            if(Mathf.Abs(delta.x)<10 && Mathf.Abs(delta.y)<4 && Time.time>=AttackReadyTime){FaceTarget(delta.x);AttackTarget();return;}
            base.TickAI();
        }

        protected override void AttackTarget()
        {
            MoveHorizontally(0f);
            charging=true;attackIndex++;
            BeginAnticipation(telegraphDuration);
            EnterCooldown(definition.AttackCooldown + telegraphDuration + dashDuration);
            Invoke(nameof(BeginDashAttack), telegraphDuration);
        }

        private void BeginDashAttack()
        {
            charging=false;
            if (TargetTransform == null)
            {
                return;
            }

            var delta = GetTargetDelta();
            FaceTarget(delta.x);
            if(attackIndex%2==1)
            {
                Bable.BableCombatFX.Projectile(gameObject,(Vector2)transform.position+new Vector2(FacingSign*1.6f,-.5f),new Vector2(FacingSign,0),8,definition.ContactDamage,new Color(1,.4f,.25f),1.2f);
                GetComponent<Bable.CharacterPresentation>()?.Act("shockwave",.65f);
                return;
            }
            dashUntil = Time.time + dashDuration;
            MoveHorizontally(FacingSign * dashSpeed);
            var hitCount = TryDamageBox((Vector2)transform.position + new Vector2(FacingSign * 1.6f, 0f), new Vector2(3.2f, 1.8f), definition.ContactDamage, new Vector2(FacingSign * 5f, 0f));
            Invoke(nameof(EndDashAttack), dashDuration);
        }

        private void EndDashAttack()
        {
            MoveHorizontally(0f);
        }
    }
}
