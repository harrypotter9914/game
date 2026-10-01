using Babel.Runtime.Combat;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private TeamAlignment sourceTeam = TeamAlignment.Enemy;
        [SerializeField] private float speed = 7f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private int damage = 1;

        private Rigidbody2D body;
        private void Awake(){body=GetComponent<Rigidbody2D>();if(body==null)body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;body.freezeRotation=true;}

        private Vector2 direction = Vector2.right;
        private GameObject source;
        private float launchedAt, dissolvesAt = -1;
        private Color tint;
        private SpriteRenderer visual;
        private float radius = .12f;
        public string effectArt;
        public float VisualAlpha => dissolvesAt>=0?1-Mathf.SmoothStep(0,1,(Time.time-dissolvesAt)/.18f):Mathf.SmoothStep(0,1,(Time.time-launchedAt)/.1f);
        public float maxDistance=40;
        float travelled;

        public void Launch(Vector2 moveDirection, int damageAmount, float moveSpeed, GameObject sourceObject, TeamAlignment team)
        {
            direction = moveDirection.sqrMagnitude > 0.01f ? moveDirection.normalized : Vector2.right;
            damage = damageAmount;
            speed = moveSpeed;
            source = sourceObject;
            sourceTeam = team;
            launchedAt = Time.time;
            visual = GetComponent<SpriteRenderer>();
            if (visual != null) { tint = visual.color; visual.color = new Color(tint.r,tint.g,tint.b,0); }
            var circle = GetComponent<CircleCollider2D>();
            if(circle != null) radius = circle.radius * Mathf.Abs(transform.lossyScale.x);
        }

        private void Update()
        {
            if (visual == null) return;
            if(!string.IsNullOrEmpty(effectArt))visual.sprite=Bable.CombatVfxFrames.Get(effectArt,dissolvesAt>=0?3:Time.time-launchedAt<.05f?0:Time.time-launchedAt<.1f?1:2);
            float alpha = dissolvesAt >= 0 ? 1 - Mathf.SmoothStep(0,1,(Time.time-dissolvesAt)/.18f) : Mathf.SmoothStep(0,1,(Time.time-launchedAt)/.1f);
            visual.color = new Color(tint.r,tint.g,tint.b,tint.a*alpha);
            if(dissolvesAt >= 0 && Time.time-dissolvesAt >= .18f) Destroy(gameObject);
        }

        private void OnEnable()
        {
            CancelInvoke(nameof(DestroySelf));
            Invoke(nameof(DestroySelf), lifetime);
        }

        private void FixedUpdate()
        {
            if(dissolvesAt >= 0 || Time.time-launchedAt < .1f) return;
            if(travelled>=maxDistance){DestroySelf();return;}
            float step=Mathf.Min(speed*Time.fixedDeltaTime,maxDistance-travelled);travelled+=step;
            foreach(var hit in Physics2D.CircleCastAll(transform.position,radius,direction,step))
            {
                var other=hit.collider;if(other==null||other.gameObject==gameObject||other.isTrigger)continue;
                if(source!=null&&(other.transform==source.transform||other.transform.IsChildOf(source.transform)))continue;
                var hp=other.GetComponentInParent<IDamageable>();
                if(hp!=null&&!hp.CanReceiveDamage(sourceTeam)&&!Bable.TerrainMotion.Solid(other))continue;
                transform.position = hit.centroid;
                OnTriggerEnter2D(other);return;
            }
            body.MovePosition((Vector2)transform.position+direction*step);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (dissolvesAt >= 0 || Time.time-launchedAt < .1f) return;
            if (source != null)
            {
                var sourceTransform = source.transform;
                if (other.transform == sourceTransform || other.transform.IsChildOf(sourceTransform) || sourceTransform.IsChildOf(other.transform))
                {
                    return;
                }
            }

            if(Bable.TerrainMotion.Solid(other)){Bable.CombatImpact.Spawn(transform.position,-direction,false);Bable.CombatAudio.Play("projectile_wall",transform.position,.45f);DestroySelf();return;}
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CanReceiveDamage(sourceTeam))
            {
                damageable.ReceiveDamage(new DamageInfo(damage, transform.position, direction * speed, source, sourceTeam));
                DestroySelf();
                return;
            }

            if (!other.isTrigger)
            {
                var friendly = other.GetComponentInParent<IDamageable>();
                if (friendly == null || friendly.CanReceiveDamage(sourceTeam)) DestroySelf();
            }
        }

        private void DestroySelf()
        {
            if (gameObject != null)
            {
                if(dissolvesAt >= 0) return;
                dissolvesAt = Time.time;
                var c=GetComponent<Collider2D>();if(c!=null)c.enabled=false;
                Destroy(gameObject,.2f);
            }
        }
    }
}
