using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class FinalBossController : EnemyControllerBase
    {
        [SerializeField] private float phaseTwoThreshold = 0.5f;
        [SerializeField] private float dashSpeedPhaseOne = 7f;
        [SerializeField] private float dashSpeedPhaseTwo = 11f;
        [SerializeField] private float telegraphPhaseOne = 0.55f;
        [SerializeField] private float telegraphPhaseTwo = 0.4f;
        [SerializeField] private float dashDuration = 0.24f;

        private bool announcedPhaseTwo;
        private bool casting;
        private int pattern;
        private float dashRunningUntil;
        public bool PhaseTwo => IsPhaseTwo();

        protected override void TickAI()
        {
            var phaseTwo = IsPhaseTwo();
            if(casting || Time.time < dashRunningUntil) return;
            if (phaseTwo && !announcedPhaseTwo)
            {
                announcedPhaseTwo = true;
            }

            var distance=GetTargetDelta();
            if(Mathf.Abs(distance.x)<13 && Mathf.Abs(distance.y)<5 && Time.time>=AttackReadyTime)
            {
                pattern++;
                if(pattern%2==1){casting=true;MoveHorizontally(0);BeginAnticipation(2);GetComponent<Bable.CharacterPresentation>()?.Act("cast",2);EnterCooldown(4.2f);Invoke(nameof(CastJudgement),2);return;}
                AttackTarget();return;
            }
            base.TickAI();
        }
        private void CastJudgement()
        {
            casting=false;if(!CanAct())return;
            Vector2 direction=(TargetTransform.position-transform.position).normalized;
            int count=IsPhaseTwo()?9:7;
            for(int i=0;i<count;i++)
            {
                float angle=(-75+150f*i/(count-1))*Mathf.Deg2Rad;
                var d=new Vector2(direction.x*Mathf.Cos(angle)-direction.y*Mathf.Sin(angle),direction.x*Mathf.Sin(angle)+direction.y*Mathf.Cos(angle));
                Bable.BableCombatFX.Projectile(gameObject,(Vector2)transform.position+d*1.7f,d,IsPhaseTwo()?7:5,1,new Color(.85f,.2f,.45f),.7f);
            }
        }

        protected override void ChaseTarget(float directionSign)
        {
            var sign = directionSign == 0f ? FacingSign : (int)Mathf.Sign(directionSign);
            if (sign != 0)
            {
                FacingSign = sign;
            }

            MoveHorizontally(FacingSign * definition.ChaseSpeed * (IsPhaseTwo() ? 1.35f : 1f));
        }

        protected override void AttackTarget()
        {
            casting=true;
            var phaseTwo = IsPhaseTwo();
            MoveHorizontally(0f);
            BeginAnticipation(phaseTwo ? telegraphPhaseTwo : telegraphPhaseOne);
            EnterCooldown(definition.AttackCooldown + (phaseTwo ? telegraphPhaseTwo : telegraphPhaseOne) + dashDuration + (phaseTwo ? 0.4f : 0.7f));
            Invoke(nameof(ExecuteDash), phaseTwo ? telegraphPhaseTwo : telegraphPhaseOne);
        }

        private bool IsPhaseTwo()
        {
            return Health.CurrentHealth <= Mathf.CeilToInt(Health.MaxHealth * phaseTwoThreshold);
        }

        private void ExecuteDash()
        {
            casting=false;dashRunningUntil=Time.time+dashDuration;
            if (TargetTransform == null)
            {
                return;
            }

            var phaseTwo = IsPhaseTwo();
            var delta = GetTargetDelta();
            FaceTarget(delta.x);
            MoveHorizontally(FacingSign * (phaseTwo ? dashSpeedPhaseTwo : dashSpeedPhaseOne));
            var damage = phaseTwo ? definition.ContactDamage : Mathf.Max(1, definition.ContactDamage - 1);
            var hitCount = TryDamageBox((Vector2)transform.position + new Vector2(FacingSign * 1.8f, 0f), new Vector2(3.6f, 1.9f), damage, new Vector2(FacingSign * 6f, 0f));
            if (phaseTwo)
            {
                hitCount += TryDamageBox((Vector2)transform.position + new Vector2(FacingSign * 2.6f, 0f), new Vector2(2.2f, 1.5f), 1, new Vector2(FacingSign * 4f, 0f));
            }
            Invoke(nameof(StopDash), dashDuration);
        }

        private void StopDash()
        {
            MoveHorizontally(0f);
        }
    }
}
