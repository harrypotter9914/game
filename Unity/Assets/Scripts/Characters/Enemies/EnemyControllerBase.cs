using System.Collections.Generic;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyControllerBase : MonoBehaviour
    {
        [SerializeField] protected EnemyDefinition definition;
        [SerializeField] protected Transform target;
        [SerializeField] protected Transform attackOrigin;
        [SerializeField] protected float patrolDistance = 2f;
        [SerializeField] protected TeamAlignment team = TeamAlignment.Enemy;

        protected Rigidbody2D Body { get; private set; }
        protected HealthComponent Health { get; private set; }
        protected GameSession Session { get; private set; }
        protected Transform TargetTransform => target;
        protected Vector2 PatrolOrigin { get; private set; }
        protected int FacingSign { get; set; } = 1;
        protected float AttackReadyTime { get; set; }

        public EnemyDefinition Definition => definition;
        public int LookDirection=>FacingSign;
        public string BehaviourState {get;protected set;}="Patrol";
        private PlatformAwareness platform;
        private int patrolSign=1;
        private float turnReady,attackBusyUntil;
        private bool platformLocked;
        private bool NativeBoss=>this is Bable.BossBrain;
        private HitRecoil recoil;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            ActorBodyCollision.Configure(gameObject, false);
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Health = GetComponent<HealthComponent>();
            if(!NativeBoss&&!(this is GiantEnemyController))recoil=GetComponent<HitRecoil>()??gameObject.AddComponent<HitRecoil>();
            Health.Damaged += InterruptMinionAttack;
            ResolveSession();
            PatrolOrigin = transform.position;
            if(!NativeBoss)platform=GetComponent<PlatformAwareness>()??gameObject.AddComponent<PlatformAwareness>();
            if (definition != null)
            {
                Health.Configure(definition.MaxHealth, definition.MaxHealth);
            }
        }

        protected virtual void Update()
        {
            ResolveSession();
            AcquireTarget();
            if (!CanAct())
            {
                return;
            }

            if(!NativeBoss){
                if(Time.time<attackBusyUntil){BehaviourState="Attack";MoveHorizontally(0);return;}
                platformLocked=Mathf.Abs(GetTargetDelta().x)<=definition.ChaseRange&&platform.SameReachablePlatform(target);
                if(!platformLocked){BehaviourState="Patrol";Patrol();return;}
                BehaviourState="Pursue";
            }
            TickAI();
        }

        void InterruptMinionAttack(DamageInfo info) {
            if(NativeBoss)return;
            CancelInvoke();attackBusyUntil=Time.time+.28f;
            AttackReadyTime=Mathf.Max(AttackReadyTime,attackBusyUntil+.15f);
        }
        protected virtual void OnDestroy(){if(Health!=null)Health.Damaged-=InterruptMinionAttack;}
        protected virtual void OnDisable()
        {
            CancelInvoke();
        }

        protected virtual void TickAI()
        {
            var delta = GetTargetDelta();
            var horizontalDistance = Mathf.Abs(delta.x);
            var verticalDistance = Mathf.Abs(delta.y);
            FaceTarget(delta.x);

            if (verticalDistance <= definition.VerticalAggroTolerance &&
                horizontalDistance <= definition.AttackRange)
            {
                MoveHorizontally(0);
                BehaviourState="Hold attack distance";
                if(Time.time >= AttackReadyTime)AttackTarget();
                return;
            }

            if (!NativeBoss && platformLocked && horizontalDistance <= definition.AttackRange)
            {
                MoveHorizontally(0);BehaviourState="Watch airborne target";return;
            }
            if ((NativeBoss ? verticalDistance <= definition.VerticalAggroTolerance : platformLocked) &&
                horizontalDistance <= definition.ChaseRange)
            {
                ChaseTarget(Mathf.Sign(delta.x));
                return;
            }

            Patrol();
        }

        public virtual bool TryReceiveDamage(DamageInfo damage)
        {
            if (!Health.CanReceiveDamage(damage.SourceTeam))
            {
                return false;
            }

            int before = Health.CurrentHealth;
            Health.ReceiveDamage(damage);
            return Health.CurrentHealth < before;
        }

        protected virtual void Patrol()
        {
            if(Time.time<turnReady){MoveHorizontally(0);return;}
            float x=transform.position.x;
            if(x>PatrolOrigin.x+patrolDistance)patrolSign=-1;
            else if(x<PatrolOrigin.x-patrolDistance)patrolSign=1;
            if(platform!=null&&!platform.CanStep(patrolSign)){
                patrolSign=-patrolSign;FacingSign=patrolSign;turnReady=Time.time+.45f;MoveHorizontally(0);return;
            }
            FacingSign=patrolSign;MoveHorizontally(patrolSign*definition.PatrolSpeed);
        }

        protected virtual void ChaseTarget(float directionSign)
        {
            var sign = directionSign == 0f ? FacingSign : (int)Mathf.Sign(directionSign);
            FacingSign = sign == 0 ? FacingSign : sign;
            float speed=definition.ChaseSpeed;
            var player=target!=null?target.GetComponent<PlayerController2D>():null;
            if(!NativeBoss&&player!=null&&player.Definition!=null)
                speed=Mathf.Max(speed,player.Definition.MoveSpeed*.85f);
            MoveHorizontally(FacingSign * speed);
        }
        protected bool CanWalk(int sign)=>platform==null||platform.CanStep(sign);

        protected virtual void AttackTarget()
        {
            MoveHorizontally(0f);
            AttackReadyTime = Time.time + definition.AttackCooldown;
            var origin = GetAttackOrigin();
            origin.x = transform.position.x + FacingSign * Mathf.Max(0.5f, definition.AttackRange * 0.5f);
            var hitPlayer = TryDamageCircle(origin, definition.AttackRange, definition.ContactDamage, new Vector2(FacingSign * 2f, 0f));
        }

        protected void BeginAnticipation(float duration = 0.6f)
        {
            attackBusyUntil=Time.time+duration+.4f;
            GetComponent<Bable.CharacterPresentation>()?.ActSegment("attack", duration, 0, .49f);
        }

        protected void ReleaseAttack(string animation="attack",float seconds=.35f){
            attackBusyUntil=Time.time+seconds;
            GetComponent<Bable.CharacterPresentation>()?.ActSegment(animation,seconds,.5f,.999f);
        }
        protected void ResolveSession()
        {
            if (Session == null)
            {
                Session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            }
        }

        protected void AcquireTarget()
        {
            if (target == null)
            {
                var player = FindObjectOfType<PlayerRuntimeState>();
                target = player != null ? player.transform : null;
            }
        }

        protected bool CanAct()
        {
            if(recoil!=null&&recoil.IsRecoiling){BehaviourState="Recoil";return false;}
            if (Bable.TowerLoading.Busy || (Session != null && Session.IsPaused) || Bable.TowerDialogue.StoryActive)
            {
                MoveHorizontally(0f);
                return false;
            }

            return target != null && definition != null && Health != null && Health.CurrentHealth > 0;
        }

        protected Vector2 GetTargetDelta()
        {
            return target == null ? Vector2.zero : (Vector2)(target.position - transform.position);
        }

        protected void FaceTarget(float deltaX)
        {
            if (Mathf.Abs(deltaX) > 0.45f)
            {
                FacingSign = deltaX < 0f ? -1 : 1;
            }
        }

        protected void MoveHorizontally(float speed)
        {
            if(recoil!=null&&recoil.IsRecoiling)return;
            if(!NativeBoss&&platform!=null&&Mathf.Abs(speed)>.01f&&!platform.CanStep(speed<0?-1:1)){speed=0;BehaviourState="Wait at edge";}
            Body.linearVelocity = new Vector2(speed, Body.linearVelocity.y);
        }

        protected Vector2 GetAttackOrigin()
        {
            return attackOrigin != null ? attackOrigin.position : transform.position;
        }

        protected void EnterCooldown(float seconds)
        {
            AttackReadyTime = Time.time + seconds;
        }

        protected bool TryDamageCircle(Vector2 origin, float radius, int damageAmount, Vector2 force)
        {
            var damage = new DamageInfo(damageAmount, origin, force, gameObject, team);
            var hits = Physics2D.OverlapCircleAll(origin, radius);
            var hitPlayer = false;
            var processed = new HashSet<int>();
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanReceiveDamage(team) || !Bable.CombatGeometry.Clear(CombatOrigin, hit.bounds.center))
                {
                    continue;
                }

                var root = hit.transform.root.gameObject;
                if (!processed.Add(root.GetEntityId().GetHashCode()))
                {
                    continue;
                }

                damageable.ReceiveDamage(damage);
                hitPlayer = true;
            }

            return hitPlayer;
        }

        protected int TryDamageBox(Vector2 center, Vector2 size, int damageAmount, Vector2 force)
        {
            var damage = new DamageInfo(damageAmount, center, force, gameObject, team);
            var hits = Physics2D.OverlapBoxAll(center, size, 0f);
            var processed = new HashSet<int>();
            var hitCount = 0;
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanReceiveDamage(team) || !Bable.CombatGeometry.Clear(CombatOrigin, hit.bounds.center))
                {
                    continue;
                }

                var root = hit.transform.root.gameObject;
                if (!processed.Add(root.GetEntityId().GetHashCode()))
                {
                    continue;
                }

                damageable.ReceiveDamage(damage);
                hitCount++;
            }

            return hitCount;
        }

        protected Vector2 CombatOrigin => GetComponent<Collider2D>() != null
            ? (Vector2)GetComponent<Collider2D>().bounds.center : (Vector2)transform.position;

        // Body contact is physical separation, never an attack.
        protected virtual void OnCollisionStay2D(Collision2D collision) { }

        protected virtual void OnDrawGizmosSelected()
        {
            if (definition == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, definition.ChaseRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetAttackOrigin(), definition.AttackRange);
        }
    }
}
