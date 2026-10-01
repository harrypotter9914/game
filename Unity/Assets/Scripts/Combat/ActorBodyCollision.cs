using UnityEngine;

namespace Babel.Runtime.Combat
{
    // Body layers exclude physical pushing, not the explicit attack/hurtbox queries.
    // A layer rule survives collider toggles (Korah), respawns and newly spawned enemies.
    public static class ActorBodyCollision
    {
        public const int PlayerBodyLayer = 8;
        public const int EnemyBodyLayer = 9;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Physics2D.IgnoreLayerCollision(PlayerBodyLayer, EnemyBodyLayer, true);
            Physics2D.IgnoreLayerCollision(EnemyBodyLayer, EnemyBodyLayer, true);
        }

        public static void Configure(GameObject actor, bool player)
        {
            int layer = player ? PlayerBodyLayer : EnemyBodyLayer;
            actor.layer = layer;
            var body = actor.GetComponent<Rigidbody2D>();
            foreach (var collider in actor.GetComponentsInChildren<Collider2D>(true))
                if (!collider.isTrigger && collider.attachedRigidbody == body)
                    collider.gameObject.layer = layer;
        }
    }
}
