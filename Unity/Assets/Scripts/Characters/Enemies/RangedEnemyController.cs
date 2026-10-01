using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    public sealed class RangedEnemyController : EnemyControllerBase
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float preferredMinRange = 2.2f;
        [SerializeField] private float preferredMaxRange = 4.6f;
        [SerializeField] private float telegraphDuration = 1f;
        private float aimingUntil;
        private bool aimedUp;

        protected override void TickAI()
        {
            if(Time.time<aimingUntil){BehaviourState="Aim";MoveHorizontally(0);return;}
            var delta = GetTargetDelta();
            var horizontalDistance = Mathf.Abs(delta.x);
            var verticalDistance = Mathf.Abs(delta.y);
            FaceTarget(delta.x);
            if(horizontalDistance > definition.ChaseRange) { BehaviourState="Patrol";Patrol(); return; }

            if (horizontalDistance > preferredMaxRange)
            {
                BehaviourState="Pursue";
                ChaseTarget(Mathf.Sign(delta.x));
                return;
            }

            if (horizontalDistance < preferredMinRange)
            {
                BehaviourState="Reposition";
                if(CanWalk(-FacingSign))MoveHorizontally(-FacingSign * definition.ChaseSpeed);
                else {MoveHorizontally(0);if(Time.time>=AttackReadyTime)AttackTarget();}
                return;
            }

            if (Time.time >= AttackReadyTime)
            {
                AttackTarget();
                return;
            }

            BehaviourState="Hold attack distance";
            MoveHorizontally(0f);
        }

        protected override void AttackTarget()
        {
            BehaviourState="Aim";
            MoveHorizontally(0f);
            BeginAnticipation(telegraphDuration);
            aimingUntil=Time.time+telegraphDuration;
            aimedUp=GetTargetDelta().y>1;
            GetComponent<Bable.CharacterPresentation>()?.ActSegment(aimedUp?"upattack":"attack",telegraphDuration,0,.49f);
            EnterCooldown(definition.AttackCooldown + telegraphDuration);
            Invoke(nameof(FireProjectile), telegraphDuration);
        }

        private void FireProjectile()
        {
            if(!CanAct())return;
            // The same locked facing used by the bow pose owns the release socket.
            Vector2 dir=TargetTransform!=null?(Vector2)TargetTransform.GetComponent<Collider2D>().bounds.center-CombatOrigin:Vector2.right*FacingSign;
            if(dir.x*FacingSign<0)dir=Vector2.right*FacingSign;
            dir.Normalize();
            Vector2 origin;
            bool clear=Bable.CombatMuzzle.TryResolve(gameObject,FacingSign,aimedUp,out origin);
            ReleaseAttack(aimedUp?"upattack":"attack");
            if(!clear)return;
            Bable.CombatAudio.Attack(gameObject);
            Bable.MagicArrowVisual.Launch(gameObject,origin,dir,projectileSpeed,definition.ContactDamage);

        }
    }
}
