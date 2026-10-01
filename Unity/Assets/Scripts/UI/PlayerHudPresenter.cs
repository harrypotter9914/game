using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Babel.Runtime.UI
{
    public sealed class PlayerHudPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerRuntimeState playerState;
        [SerializeField] private HealthComponent playerHealth;
        [SerializeField] private ManaComponent playerMana;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image manaFill;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private TMP_Text manaLabel;

        private HealthComponent subscribedHealth;
        private ManaComponent subscribedMana;
        private GameSession session;
        private float healthFullWidth;
        private float manaFullWidth;

        private void Awake()
        {
            if (healthFill != null)
            {
                healthFullWidth = healthFill.rectTransform.sizeDelta.x;
            }

            if (manaFill != null)
            {
                manaFullWidth = manaFill.rectTransform.sizeDelta.x;
            }

            ResolveReferences();
            RefreshSubscriptions();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshSubscriptions();
            Refresh();
        }

        private void Update()
        {
            ResolveReferences();
            RefreshSubscriptions();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (playerState == null)
            {
                playerState = FindObjectOfType<PlayerRuntimeState>();
            }

            if (session == null)
            {
                session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            }

            if (playerState != null)
            {
                playerHealth = playerState.Health;
                playerMana = playerState.Mana;
            }
        }

        private void RefreshSubscriptions()
        {
            if (subscribedHealth != playerHealth || subscribedMana != playerMana)
            {
                Unsubscribe();
                subscribedHealth = playerHealth;
                subscribedMana = playerMana;

                if (subscribedHealth != null)
                {
                    subscribedHealth.Changed += HandleHealthChanged;
                }

                if (subscribedMana != null)
                {
                    subscribedMana.Changed += HandleManaChanged;
                }
            }
        }

        private void Unsubscribe()
        {
            if (subscribedHealth != null)
            {
                subscribedHealth.Changed -= HandleHealthChanged;
            }

            if (subscribedMana != null)
            {
                subscribedMana.Changed -= HandleManaChanged;
            }

            subscribedHealth = null;
            subscribedMana = null;
        }

        private void Refresh()
        {
            if (playerHealth != null)
            {
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (playerMana != null)
            {
                HandleManaChanged(playerMana.CurrentMana, playerMana.MaxMana);
            }

            if (goldLabel != null && session != null)
            {
                goldLabel.text = $"Gold {session.CurrentGold}";
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            SetBarWidth(healthFill, healthFullWidth, current, max);
            if (healthLabel != null)
            {
                healthLabel.text = $"HP {current}/{max}";
            }
        }

        private void HandleManaChanged(int current, int max)
        {
            SetBarWidth(manaFill, manaFullWidth, current, max);
            if (manaLabel != null)
            {
                manaLabel.text = $"MP {current}/{max}";
            }
        }

        private static void SetBarWidth(Image image, float fullWidth, int current, int max)
        {
            if (image == null)
            {
                return;
            }

            var ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(fullWidth * ratio, rect.sizeDelta.y);
        }
    }
}
