using Babel.Runtime.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Babel.Runtime.Characters.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DefaultExecutionOrder(-500)] // Sample direction before combat consumes this frame's attack.
    public sealed class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private PlayerDefinition definition;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float groundCheckRadius = 0.18f;

        private Rigidbody2D body;
        private Collider2D selfCollider;
        private GameSession session;
        private float moveInput;
        private float verticalInput;
        private bool jumpPressed;
        private bool isGrounded;
        private bool isTouchingWall;
        private int wallDirection;
        private int jumpsRemaining;
        private int facingSign = 1;
        private readonly RaycastHit2D[] floorContacts = new RaycastHit2D[16];
        private readonly RaycastHit2D[] wallContacts = new RaycastHit2D[16];
        private float wallSurfaceX;
        private float dashUntilTime;
        private float coyoteUntil;
        private float bufferedJumpUntil;
        private float wallPushUntil;
        private float savedGravity;
        private bool wasDashing;
        private bool charging;
        private Babel.Runtime.Combat.HitRecoil recoil;
        public bool IsRecoiling => recoil!=null&&recoil.IsRecoiling;
        public void InterruptForHit()
        {
            dashUntilTime=0;charging=false;bufferedJumpUntil=0;jumpPressed=false;
            if(wasDashing){body.gravityScale=savedGravity;wasDashing=false;}
        }
        public bool IsCharging => charging;
        public bool IsDashing => Time.time<dashUntilTime;
        public void SetCharging(bool value){charging=value;}
        public void PogoBounce(){body.linearVelocity=new Vector2(body.linearVelocity.x,definition.JumpImpulse*.85f);jumpsRemaining=HasDoubleJumpAbility()?definition.ExtraAirJumps:0;}
#if UNITY_EDITOR
        public bool UseTestInput;
        public void SetTestInput(float horizontal, float vertical, bool jump = false)
        { UseTestInput = true; moveInput=horizontal; verticalInput=vertical; if(horizontal!=0)facingSign=horizontal<0?-1:1; if(jump) bufferedJumpUntil=Time.time+.14f; }
#endif

        public float MoveInput => moveInput;
        public float VerticalInput => verticalInput;
        public bool IsGrounded => isGrounded;
        public bool IsWallSliding => HasWallJumpAbility() && isTouchingWall && !isGrounded && body.linearVelocity.y < 0f;
        public int FacingSign => facingSign;
        public int WallDirection => isTouchingWall ? wallDirection : 0;
        public float WallSurfaceX => wallSurfaceX;
        public Vector2 Velocity => body.linearVelocity;
        public PlayerDefinition Definition => definition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            selfCollider = GetComponent<Collider2D>();
            Babel.Runtime.Combat.ActorBodyCollision.Configure(gameObject, true);
            recoil=GetComponent<Babel.Runtime.Combat.HitRecoil>()??gameObject.AddComponent<Babel.Runtime.Combat.HitRecoil>();
            TryResolveSession();
            if (session != null && definition != null)
            {
                session.ConfigurePlayer(definition);
            }
        }

        private void Start()
        {
            TryResolveSession();
            if (session != null && definition != null)
            {
                session.ConfigurePlayer(definition);
            }
        }

        private void Update()
        {
            TryResolveSession();
            if (Bable.TowerLoading.Busy || (session != null && session.IsPaused))
            {
                moveInput = 0f;
                verticalInput = 0f;
                jumpPressed = false;
                bufferedJumpUntil = 0;
                return;
            }
#if UNITY_EDITOR
            if (UseTestInput) return;
#endif

            moveInput = 0f;
            if (Bable.GameInput.Held(Bable.GameAction.Left))
            {
                moveInput -= 1f;
            }
            if (Bable.GameInput.Held(Bable.GameAction.Right))
            {
                moveInput += 1f;
            }

            verticalInput = 0f;
            if (Bable.GameInput.Held(Bable.GameAction.Up))
            {
                verticalInput += 1f;
            }
            if (Bable.GameInput.Held(Bable.GameAction.Down))
            {
                verticalInput -= 1f;
            }

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                facingSign = moveInput < 0f ? -1 : 1;
            }

            jumpPressed |= Bable.GameInput.Down(Bable.GameAction.Jump);
            if (jumpPressed) bufferedJumpUntil = Time.time + .14f;
        }

        private void TryResolveSession()
        {
            if (session == null)
            {
                session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            }
        }

        private void FixedUpdate()
        {
            if (definition == null)
            {
                return;
            }

            UpdateContacts();
            if(IsRecoiling){bufferedJumpUntil=0;return;}
            if (isGrounded) coyoteUntil=Time.time+.1f;

            if (Time.time < dashUntilTime)
            {
                return;
            }
            if (wasDashing) { body.gravityScale=savedGravity; wasDashing=false; }

            var currentVelocity = body.linearVelocity;
            var targetSpeed = moveInput * definition.MoveSpeed;
            if(charging)targetSpeed=0;
            var acceleration = isGrounded ? definition.GroundAcceleration : definition.AirAcceleration;
            if (Time.time >= wallPushUntil) currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);

            if (IsWallSliding)
            {
                currentVelocity.y = Mathf.Max(currentVelocity.y, -definition.WallSlideSpeed);
            }

            if (Time.time < bufferedJumpUntil && (isGrounded || Time.time < coyoteUntil || IsWallSliding || jumpsRemaining > 0))
            {
                TryJump(ref currentVelocity);
                bufferedJumpUntil=0;
            }

            if (isGrounded && currentVelocity.y <= .1f && Mathf.Abs(currentVelocity.x) > .1f)
                TrySmallStep(currentVelocity.x);
            body.linearVelocity = currentVelocity;
            jumpPressed = false;
        }

        private void TrySmallStep(float speed)
        {
            // Only contact lips, not full-height tiles or ability-gated jump platforms.
            const float maxRise = .3f;
            const float skin = .025f;
            var bounds = selfCollider.bounds;
            Vector2 size = (Vector2)bounds.size - Vector2.one * .04f;
            Vector2 direction = speed < 0 ? Vector2.left : Vector2.right;
            float advance = Mathf.Abs(speed) * Time.fixedDeltaTime + .06f;
            bool blocked = false;
            foreach (var hit in Physics2D.BoxCastAll(bounds.center, size, 0, direction, advance))
                if (IsTerrainCollider(hit.collider) && hit.normal.y < .5f) { blocked = true; break; }
            if (!blocked) return;
            foreach (var hit in Physics2D.BoxCastAll(bounds.center, size, 0, Vector2.up, maxRise + skin))
                if (IsTerrainCollider(hit.collider)) return;
            Vector2 raised = (Vector2)bounds.center + Vector2.up * (maxRise + skin);
            foreach (var hit in Physics2D.BoxCastAll(raised, size, 0, direction, advance))
                if (IsTerrainCollider(hit.collider)) return;
            float nearest = float.MaxValue;
            foreach (var hit in Physics2D.BoxCastAll(raised + direction * advance, size, 0, Vector2.down, maxRise + skin))
                if (IsTerrainCollider(hit.collider) && hit.normal.y > .8f && hit.distance > 0)
                    nearest = Mathf.Min(nearest, hit.distance);
            float rise = maxRise + skin - nearest;
            if (rise > .025f && rise <= maxRise)
                body.position += direction * advance + Vector2.up * (rise + skin);
        }

        public void StartDash(Vector2 direction, float speed, float duration)
        {
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = new Vector2(facingSign == 0 ? 1 : facingSign, 0f);
            }

            direction = direction.normalized;
            if (Mathf.Abs(direction.x) > 0.01f)
            {
                facingSign = direction.x < 0f ? -1 : 1;
            }

            dashUntilTime = Time.time + duration;
            if (!wasDashing) savedGravity=body.gravityScale;
            body.gravityScale=0; wasDashing=true;
            body.linearVelocity = direction * speed;
        }

        private void TryJump(ref Vector2 velocity)
        {
            if (isGrounded || Time.time < coyoteUntil)
            {
                coyoteUntil=0;
                jumpsRemaining = HasDoubleJumpAbility() ? definition.ExtraAirJumps : 0;
                velocity.y = definition.JumpImpulse;
                Bable.CombatAudio.Play("jump",transform.position,.65f);
                return;
            }

            if (HasWallJumpAbility() && IsWallSliding)
            {
                facingSign = -wallDirection;
                wallPushUntil=Time.time+.16f;
                velocity.x = -wallDirection * definition.WallJumpHorizontalImpulse;
                velocity.y = definition.WallJumpVerticalImpulse;
                Bable.CombatAudio.Play("wall_jump",transform.position,.65f);
                jumpsRemaining = HasDoubleJumpAbility() ? definition.ExtraAirJumps : 0;
                return;
            }

            if (!HasDoubleJumpAbility() || jumpsRemaining <= 0)
            {
                return;
            }

            jumpsRemaining--;
            velocity.y = definition.JumpImpulse;
            Bable.CombatAudio.Play("double_jump",transform.position,.65f);
        }

        private void UpdateContacts()
        {
            var wasGrounded = isGrounded;
            isGrounded = false;
            int contactCount=selfCollider.Cast(Vector2.down,floorContacts,.08f);
            for(int i=0;i<contactCount;i++)
            {
                var hit=floorContacts[i];
                if (!IsTerrainCollider(hit.collider)||hit.normal.y<.65f)
                {
                    continue;
                }

                isGrounded = true;
                break;
            }

            if (isGrounded && !wasGrounded)
            {
                jumpsRemaining = HasDoubleJumpAbility() ? definition.ExtraAirJumps : 0;
                if(Time.timeSinceLevelLoad>1&&!Bable.TowerLoading.Busy)Bable.CombatAudio.Play("land",transform.position,.4f);
            }

            isTouchingWall = false;
            wallDirection = 0;
            // Prefer the facing wall in narrow shafts; record its actual surface,
            // not the centre of an overlap probe or the bounds of a whole tilemap.
            if (!FindWall(facingSign)) FindWall(-facingSign);
        }

        private bool FindWall(int direction)
        {
            int count = selfCollider.Cast(Vector2.right * direction, wallContacts, definition.WallCheckDistance);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = wallContacts[i];
                if (!IsTerrainCollider(hit.collider) || (groundMask.value & (1 << hit.collider.gameObject.layer)) == 0
                    || hit.normal.x * direction > -.8f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                wallSurfaceX = hit.point.x;
            }
            if (float.IsPositiveInfinity(nearest)) return false;
            isTouchingWall = true;
            wallDirection = direction;
            return true;
        }

        private bool IsTerrainCollider(Collider2D hit)
        {
            if (hit == null || hit.isTrigger || hit == selfCollider || hit.transform.IsChildOf(transform))
            {
                return false;
            }

            if (hit.GetComponentInParent<Babel.Runtime.Combat.HealthComponent>() != null)
                return false;

            if (hit.attachedRigidbody != null && hit.attachedRigidbody.bodyType == RigidbodyType2D.Static)
            {
                return true;
            }

            return Bable.TerrainMotion.Solid(hit);
        }

        private bool HasWallJumpAbility()
        {
            return session != null && session.HasAbility(AbilityId.WallJump);
        }

        private bool HasDoubleJumpAbility()
        {
            return session != null && session.HasAbility(AbilityId.DoubleJump);
        }

        private void OnDrawGizmosSelected()
        {
            var probeOrigin = groundCheck != null ? groundCheck.position : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(probeOrigin, groundCheckRadius);
            if (selfCollider == null)
            {
                return;
            }

            var bounds = selfCollider.bounds;
            var checkSize = new Vector2(0.05f, bounds.size.y * 0.8f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector2(bounds.min.x - 0.05f, bounds.center.y), checkSize);
            Gizmos.DrawWireCube(new Vector2(bounds.max.x + 0.05f, bounds.center.y), checkSize);
        }
    }
}
