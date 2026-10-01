using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class GiantEnemyController : EnemyControllerBase
    {
        [SerializeField] private float telegraphDuration = 1f;

        protected override void AttackTarget()
        {
            MoveHorizontally(0f);
            BeginAnticipation(telegraphDuration);
            Bable.CombatAudio.Play("giant_windup",transform.position,.6f);
            GetComponent<Bable.CharacterPresentation>()?.Act("windup",telegraphDuration);
            EnterCooldown(definition.AttackCooldown + telegraphDuration + 0.2f);
            Invoke(nameof(ExecuteHeavySlam), telegraphDuration);
        }

        private void ExecuteHeavySlam()
        {
            if(!CanAct())return;
            ReleaseAttack();
            Bable.CombatAudio.Play("giant_slam",transform.position,.85f);

            Bable.CombatImpact.Spawn(CombatOrigin+new Vector2(FacingSign, -GetComponent<Collider2D>().bounds.extents.y+.1f),Vector2.up,false);
            var center = GetAttackOrigin() + new Vector2(FacingSign * 0.8f, -0.2f);
            var delta=GetTargetDelta();var hitCount=0;
            if(delta.magnitude<=definition.AttackRange*1.4f&&Vector2.Angle(Vector2.right*FacingSign,delta)<=75){if(TryDamageCircle(center,definition.AttackRange,definition.ContactDamage,new Vector2(FacingSign*3,1)))hitCount=1;}
        }
    }
}
