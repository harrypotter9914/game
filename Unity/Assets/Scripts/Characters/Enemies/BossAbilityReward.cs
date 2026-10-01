using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.Characters.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class BossAbilityReward : MonoBehaviour
    {
        [SerializeField] private AbilityId abilityId;
        [SerializeField] private string rewardTitle;

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

        public void Configure(AbilityId ability, string title)
        {
            abilityId = ability;
            rewardTitle = title;
        }

        private void HandleDied()
        {
            if (session == null)
            {
                session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            }

            if (session == null || session.HasAbility(abilityId))
            {
                return;
            }

            session.UnlockAbility(abilityId);
            ScreenMessagePresenter.ShowCenter($"Unlocked {GetAbilityDisplayName(abilityId)}\n{rewardTitle}", 4.2f);
        }

        private static string GetAbilityDisplayName(AbilityId ability)
        {
            switch (ability)
            {
                case AbilityId.Shockwave:
                    return "Shockwave";
                case AbilityId.CrystalDash:
                    return "Crystal Dash";
                case AbilityId.WallJump:
                    return "Wall Jump";
                case AbilityId.DoubleJump:
                    return "Double Jump";
                default:
                    return ability.ToString();
            }
        }
    }
}
