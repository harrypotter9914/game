using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.World;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyDeathReward : MonoBehaviour
    {
        [SerializeField] private int manaReward = 1;
        [SerializeField] private int goldReward = 2;
        [SerializeField] private GameObject coinPickupPrefab;

        private HealthComponent health;
        private GameSession session;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void HandleDied()
        {
            if (session != null)
            {
                session.RestoreMana(manaReward);
            }

            if (coinPickupPrefab != null)
            {
                Vector2 dropPosition;
                var shape = GetComponent<Collider2D>();
                Vector2 origin = shape != null ? (Vector2)shape.bounds.center : (Vector2)transform.position;
                if (!Bable.TreasureClearance.FindDrop(origin, out dropPosition))
                {
                    // A defeated enemy in a tight space must not leave inaccessible gold.
                    if (session != null) session.AddGold(goldReward);
                    return;
                }
                var spawned = Instantiate(coinPickupPrefab, dropPosition, Quaternion.identity);
                var pickup = spawned.GetComponent<CollectiblePickup>();
                if (pickup != null)
                {
                    pickup.ConfigureGoldAmount(goldReward);
                }
            }
            else if (session != null)
            {
                session.AddGold(goldReward);
            }
        }
    }
}
