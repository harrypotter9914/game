using System.Collections;
using System.Collections.Generic;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Player
{
    [RequireComponent(typeof(PlayerController2D))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private PlayerDefinition definition;
        [SerializeField] private Transform attackOrigin;
        private const float ShockwaveThickness = 1.3f;
        [SerializeField] private TeamAlignment team = TeamAlignment.Player;

        private PlayerController2D controller;
        private GameSession session;
        private float meleeReadyTime;
        private float shockwaveReadyTime;
        private float dashReadyTime;
        private Sprite cachedSprite;
        private float chargeStarted=-1;
        public void InterruptForHit()
        {
            StopAllCoroutines();EndShockwave();chargeStarted=-1;
            if(controller!=null)controller.SetCharging(false);
        }

        private void Awake()
        {
            controller = GetComponent<PlayerController2D>();
            ResolveSession();
            definition = definition != null ? definition : controller.Definition;
            var ownRenderer = GetComponent<SpriteRenderer>();
            cachedSprite = ownRenderer != null ? ownRenderer.sprite : null;
        }

        private void Update()
        {
            ResolveSession();
            if (IsShockwaveActive && (!Alive || !controller.enabled || Bable.TowerLoading.Busy))
            {
                if (shockwaveRoutine != null) StopCoroutine(shockwaveRoutine);
                EndShockwave();
            }
            if (Bable.TowerLoading.Busy || (session != null && session.IsPaused))
            {
                return;
            }
            if(controller.IsRecoiling)return;

            if (Bable.GameInput.Down(Bable.GameAction.Attack))
            {
                PerformMeleeAttack();
            }

            if (Bable.GameInput.Down(Bable.GameAction.Shockwave) || Bable.GameInput.Down(Bable.GameAction.Shockwave))
            {
                PerformShockwave();
            }

            bool shift=Bable.GameInput.Held(Bable.GameAction.Dash)||Bable.GameInput.Held(Bable.GameAction.Dash);
            if(shift && chargeStarted<0 && !controller.IsDashing && Time.time>=dashReadyTime && session!=null && session.HasAbility(AbilityId.CrystalDash) ) {chargeStarted=Time.time;controller.SetCharging(true);}
            if(chargeStarted>=0 && !shift){chargeStarted=-1;controller.SetCharging(false);}
            if(chargeStarted>=0 && Time.time-chargeStarted>=.7f){controller.SetCharging(false);chargeStarted=-1;PerformCrystalDash();}
        }

        public bool PerformMeleeAttack()
        {
            if (IsShockwaveActive || controller.IsRecoiling || !Alive || GameSession.Instance != null && GameSession.Instance.IsPaused) return false;
            if (definition == null || (GameSession.Instance!=null&&!GameSession.Instance.HasWeapon) || Time.time < meleeReadyTime)
            {
                return false;
            }

            var direction = ResolveMeleeDirection();
            Vector2 axis = direction == AttackDirection.Up ? Vector2.up : direction == AttackDirection.Down ? Vector2.down : Vector2.right * controller.FacingSign;
            float duration = Mathf.Max(.28f, definition.MeleeCooldown * GetCooldownMultiplier());
            meleeReadyTime = Time.time + duration;
            GetComponent<Bable.CharacterPresentation>()?.Act(direction == AttackDirection.Up ? "upattack" : direction == AttackDirection.Down ? "downattack" : "attack", duration);
            StartCoroutine(MeleeSequence(axis, direction == AttackDirection.Down, duration));
            return true;
        }

        private bool Alive => isActiveAndEnabled && GetComponent<HealthComponent>().CurrentHealth > 0;

        private IEnumerator MeleeSequence(Vector2 axis, bool pogo, float duration)
        {
            // Frame 0 anticipates; frame 1 releases; frame 2 strikes; frame 3 recovers.
            yield return new WaitForSeconds(duration * .25f);
            if (!Alive) yield break;
            float range = definition.MeleeRange * GetRangeMultiplier();
            Bable.CombatAudio.Play("sword",transform.position,.75f);
            var touched = new HashSet<Component>();
            float started = Time.time, active = duration * .35f;
            while (Alive && Time.time - started < active)
            {
                var center = GetDirectionalCenter(axis, range * .5f);
                if (Time.time - started >= duration * .07f)
                {
                    int hits = DamageOnce(center, GetDirectionalSize(axis, range, 1.1f), BuildDamageInfo(ApplyDamageModifiers(definition.MeleeDamage), axis), touched);
                    if (pogo && hits > 0) controller.PogoBounce();
                }
                yield return new WaitForFixedUpdate();
            }
        }

        public bool IsShockwaveActive { get; private set; }
        public float LastShockwaveDistance { get; private set; }
        public Vector2 LastShockwaveAxis { get; private set; }
        public int ShockwaveFacing { get; private set; } = 1;
        public Vector2 ShockwaveOrigin => Bable.PlayerShockwaveMuzzle.Resolve(this);
        private Bable.DirectionalShockwaveVisual shockwaveVisual;
        private Bable.CombatSoundLoop shockwaveSound;
        private Coroutine shockwaveRoutine;
        public bool PerformShockwave()
        {
            ResolveSession();
            if (definition == null || session == null || session.IsPaused || Bable.TowerLoading.Busy || !Alive || !controller.enabled || controller.IsRecoiling || IsShockwaveActive || !session.HasAbility(AbilityId.Shockwave) || Time.time < shockwaveReadyTime) return false;
            if (!session.TrySpendMana(definition.ShockwaveManaCost)) return false;
            IsShockwaveActive = true;
            shockwaveReadyTime = Time.time + Mathf.Max(definition.ShockwaveCooldown * GetCooldownMultiplier(), .16f + definition.ShockwaveDuration + .12f);
            LastShockwaveAxis = ResolveCardinalDirection();
            ShockwaveFacing = Mathf.Abs(LastShockwaveAxis.x)>.5f?(int)Mathf.Sign(LastShockwaveAxis.x):controller.FacingSign;
            GetComponent<Bable.CharacterPresentation>()?.Act("shockwave", .35f);
            shockwaveRoutine = StartCoroutine(ShockwaveSequence(LastShockwaveAxis));
            return true;
        }
        private IEnumerator ShockwaveSequence(Vector2 axis)
        {
            LastShockwaveAxis = axis;
            yield return new WaitForSeconds(.16f);
            float range = definition.ShockwaveRange * GetRangeMultiplier();
            const float thickness = ShockwaveThickness;
            float elapsed = 0, nextTick = 0;
            var touched = new HashSet<Component>();
            if (Alive) {
                shockwaveVisual = Bable.DirectionalShockwaveVisual.Create(transform, axis, range, thickness);
                Bable.CombatAudio.Play("beam_start", transform.position, .8f);
                shockwaveSound=Bable.CombatSoundLoop.Begin(transform,"beam_loop",.4f);
            }
            while (Alive && controller.enabled && !Bable.TowerLoading.Busy && elapsed < definition.ShockwaveDuration)
            {
                if (session.IsPaused) { yield return null; continue; }
                Vector2 origin = ShockwaveOrigin;
                float front = range * Mathf.Clamp01((elapsed + Time.fixedDeltaTime) / .16f);
                bool blocked;
                float distance = MuzzlePathClear(origin) ? TraceShockwave(origin, axis, front, thickness, out blocked) : 0;
                blocked = distance < front;
                LastShockwaveDistance = distance;
                if (shockwaveVisual != null) shockwaveVisual.SetReach(distance, blocked);
                if(blocked)Bable.CombatAudio.Play("beam_contact",origin+axis*distance,.34f);
                if (elapsed + .0001f >= nextTick) {
                    touched.Clear();
                    if (distance > .01f) DamageOnce(origin + axis * distance * .5f, GetDirectionalSize(axis, distance, thickness), BuildDamageInfo(ApplyDamageModifiers(definition.ShockwaveDamage), axis * 2), touched);
                    nextTick += Mathf.Max(.1f, definition.ShockwaveTickInterval);
                }
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
            EndShockwave();
        }
        private float TraceShockwave(Vector2 origin, Vector2 axis, float range, float thickness, out bool blocked)
        {
            float distance = range;
            var hits = Physics2D.BoxCastAll(origin, GetDirectionalSize(axis, .02f, thickness), 0, axis, range);
            foreach (var h in hits) {
                var c = h.collider;
                if (c == null || !c.enabled || c.isTrigger || c.transform.IsChildOf(transform)) continue;
                bool wall = c.GetComponentInParent<Bable.BreakableWall>() != null;
                bool gate = c.GetComponentInParent<Bable.AbilityGate>() != null;
                if (wall || gate || Bable.TerrainMotion.Solid(c)) distance = Mathf.Min(distance, h.distance);
            }
            blocked = distance < range - .001f;
            if (blocked) {
                // Excavate the contact face each physics step; never destroy blocks beyond a nearer solid wall.
                Vector2 contact = origin + axis * (distance + .03f);
                var face = GetDirectionalSize(axis, .1f, thickness);
                foreach (var c in Physics2D.OverlapBoxAll(contact, face, 0)) {
                    var wall = c.GetComponentInParent<Bable.BreakableWall>();
                    if (wall != null && c.enabled) wall.HitByShockwave();
                    var gate = c.GetComponentInParent<Bable.AbilityGate>();
                    if (gate != null) gate.Strike(contact, face);
                }
                Physics2D.SyncTransforms();
            }
            return Mathf.Max(0, distance);
        }
        private bool MuzzlePathClear(Vector2 muzzle)
        {
            Vector2 centre=GetComponent<Collider2D>().bounds.center;
            var delta=muzzle-centre;RaycastHit2D nearest=default;float distance=delta.magnitude;
            foreach(var hit in Physics2D.RaycastAll(centre,delta.normalized,distance))
                if(Bable.TerrainMotion.Solid(hit.collider)&&hit.distance<=distance){nearest=hit;distance=hit.distance;}
            if(nearest.collider==null)return true;
            // An extended hand must not start the attack on the far side of a wall.
            foreach(var c in Physics2D.OverlapCircleAll(nearest.point+delta.normalized*.02f,.06f)){
                c.GetComponentInParent<Bable.BreakableWall>()?.HitByShockwave();
                c.GetComponentInParent<Bable.AbilityGate>()?.Strike(nearest.point,Vector2.one*.12f);
            }
            return false;
        }
        private void EndShockwave()
        {
            if(shockwaveSound!=null){shockwaveSound.Stop();shockwaveSound=null;Bable.CombatAudio.Play("beam_end",transform.position,.45f);}
            IsShockwaveActive = false;
            if (shockwaveVisual != null) Destroy(shockwaveVisual.gameObject);
            shockwaveVisual = null; shockwaveRoutine = null;
        }
        private void OnDisable()
        {
            if (shockwaveRoutine != null) StopCoroutine(shockwaveRoutine);
            EndShockwave();
        }

        public bool PerformCrystalDash()
        {
            ResolveSession();
            if (definition == null || session == null || session.IsPaused || IsShockwaveActive || controller.IsRecoiling || !session.HasAbility(AbilityId.CrystalDash) || Time.time < dashReadyTime) return false;
            var direction = ResolveCardinalDirection();
            controller.StartDash(direction, definition.CrystalDashSpeed, definition.CrystalDashDuration);
            GetComponent<Bable.CharacterPresentation>()?.Act("crystaldash", definition.CrystalDashDuration);
            Bable.RelicFX.Dash(gameObject, definition.CrystalDashDuration);
            Bable.CombatAudio.Play("dash",transform.position,.8f);
            StartCoroutine(DashSequence(direction));
            dashReadyTime = Time.time + definition.CrystalDashCooldown * GetCooldownMultiplier();
            return true;
        }

        private IEnumerator DashSequence(Vector2 direction)
        {
            var touched = new HashSet<Component>();
            Vector2 previous = transform.position;
            while (Alive && controller.IsDashing)
            {
                yield return new WaitForFixedUpdate();
                Vector2 position = transform.position;
                Vector2 delta = position - previous;
                // Sweep only the actual motion: a wall stopping the body also stops damage.
                Vector2 size = new Vector2(Mathf.Abs(delta.x) + 1.2f, Mathf.Abs(delta.y) + 1.25f);
                DamageOnce((position + previous) * .5f, size, BuildDamageInfo(ApplyDamageModifiers(definition.CrystalDashDamage), direction * definition.CrystalDashSpeed), touched);
                previous = position;
            }
        }

        private int DamageOnce(Vector2 center, Vector2 size, DamageInfo damage, HashSet<Component> touched)
        {
            int count = 0;
            foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0))
            {
                var target = hit.GetComponentInParent<IDamageable>() as Component;
                if (target != null && !touched.Contains(target) && TryDamage(hit, damage)) { touched.Add(target); count++;if(IsShockwaveActive)Bable.CombatAudio.Play("beam_hit",hit.transform.position,.45f); }
            }
            return count;
        }

        private void ResolveSession()
        {
            if (session == null)
            {
                session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            }
        }

        private int ApplyDamageModifiers(int baseDamage)
        {
            ResolveSession();
            var damage = baseDamage;
            if (session != null)
            {
                var multiplier = session.DamageMultiplier;
                damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
            }

            return damage;
        }

        private float GetCooldownMultiplier()
        {
            ResolveSession();
            return session != null ? session.AttackCooldownMultiplier : 1f;
        }

        private float GetRangeMultiplier()
        {
            ResolveSession();
            return session != null ? session.AttackRangeMultiplier : 1f;
        }

        private AttackDirection ResolveMeleeDirection()
        {
            if (controller.VerticalInput > 0.5f)
            {
                return AttackDirection.Up;
            }

            if (controller.VerticalInput < -0.5f && !controller.IsGrounded)
            {
                return AttackDirection.Down;
            }

            return controller.FacingSign < 0 ? AttackDirection.Left : AttackDirection.Right;
        }

        private DamageInfo BuildDamageInfo(int amount, Vector2 force)
        {
            return new DamageInfo(amount, transform.position, force, gameObject, team);
        }

        private Vector2 GetHorizontalCenter(float forwardOffset)
        {
            var origin = attackOrigin != null ? attackOrigin.position : transform.position;
            return new Vector2(origin.x + controller.FacingSign * forwardOffset, origin.y);
        }

        private Vector2 GetVerticalCenter(float direction, float range)
        {
            var origin = attackOrigin != null ? attackOrigin.position : transform.position;
            return new Vector2(origin.x, origin.y + direction * range * 0.5f);
        }

        private Vector2 ResolveCardinalDirection()
        {
            if (controller.VerticalInput > 0.5f)
            {
                return Vector2.up;
            }

            if (controller.VerticalInput < -0.5f)
            {
                return Vector2.down;
            }

            return controller.FacingSign < 0 ? Vector2.left : Vector2.right;
        }

        private Vector2 GetDirectionalCenter(Vector2 direction, float offset)
        {
            var origin = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
            return origin + direction * offset;
        }

        private Vector2 GetDirectionalSize(Vector2 direction, float longAxis, float shortAxis)
        {
            if (Mathf.Abs(direction.y) > 0.5f)
            {
                return new Vector2(shortAxis, longAxis);
            }

            return new Vector2(longAxis, shortAxis);
        }

        private int ApplyDirectionalBoxDamage(Vector2 center, Vector2 size, DamageInfo damage)
        {
            var hitCount = 0;
            var hits = Physics2D.OverlapBoxAll(center, size, 0f);
            foreach (var hit in hits)
            {
                if (TryDamage(hit, damage))
                {
                    hitCount++;
                }
            }

            return hitCount;
        }

        private bool TryDamage(Component hit, DamageInfo damage)
        {
            var enemy = hit.GetComponentInParent<Babel.Runtime.Characters.Enemies.EnemyControllerBase>();
            if (enemy != null)
            {
                return enemy.TryReceiveDamage(damage);
            }

            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.CanReceiveDamage(team))
            {
                return false;
            }

            damageable.ReceiveDamage(damage);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null)
            {
                return;
            }

            var facing = controller != null ? controller.FacingSign : 1;
            var origin = attackOrigin != null ? attackOrigin.position : transform.position;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector2(origin.x + facing * definition.MeleeRange * 0.5f, origin.y), new Vector3(definition.MeleeRange, 1.1f, 0f));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector2(origin.x + facing * definition.ShockwaveRange * 0.5f, origin.y), new Vector3(definition.ShockwaveRange, ShockwaveThickness, 0f));
        }

        private enum AttackDirection
        {
            Left,
            Right,
            Up,
            Down,
        }
    }
}


