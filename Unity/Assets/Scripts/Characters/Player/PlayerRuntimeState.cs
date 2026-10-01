using System;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using UnityEngine;

namespace Babel.Runtime.Characters.Player
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(ManaComponent))]
    public sealed class PlayerRuntimeState : MonoBehaviour
    {
        [SerializeField] private bool snapToCheckpointOnStart = true;

        private GameSession session;
        private HealthComponent health;
        private ManaComponent mana;
        private bool isSynchronizing;

        public event Action<PlayerRuntimeState> StateSynchronized;

        public GameSession Session => session;
        public HealthComponent Health => health;
        public ManaComponent Mana => mana;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            mana = GetComponent<ManaComponent>();
            RebindSessionIfNeeded();
        }

        private void OnEnable()
        {
            health.Changed += HandleHealthChanged;
            mana.Changed += HandleManaChanged;
            RebindSessionIfNeeded();
            if (session != null)
            {
                session.StateChanged -= HandleSessionChanged;
                session.StateChanged += HandleSessionChanged;
            }
        }

        private void Start()
        {
            RebindSessionIfNeeded();
            SynchronizeFromSession();
            if (snapToCheckpointOnStart && session != null)
            {
                transform.position = session.RespawnPoint;
            }
        }

        private void Update()
        {
            RebindSessionIfNeeded();
        }

        private void OnDisable()
        {
            health.Changed -= HandleHealthChanged;
            mana.Changed -= HandleManaChanged;

            if (session != null)
            {
                session.StateChanged -= HandleSessionChanged;
            }
        }

        public void SynchronizeFromSession()
        {
            RebindSessionIfNeeded();
            if (session == null)
            {
                return;
            }

            isSynchronizing = true;
            health.Configure(session.MaxHealth, session.CurrentHealth);
            mana.Configure(session.MaxMana, session.CurrentMana);
            isSynchronizing = false;
            StateSynchronized?.Invoke(this);
        }

        private void RebindSessionIfNeeded()
        {
            var resolved = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            if (resolved == session)
            {
                return;
            }

            if (session != null)
            {
                session.StateChanged -= HandleSessionChanged;
            }

            session = resolved;
            if (session != null && isActiveAndEnabled)
            {
                session.StateChanged += HandleSessionChanged;
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (session == null || isSynchronizing)
            {
                return;
            }

            session.SetHealth(current, max);
        }

        private void HandleManaChanged(int current, int max)
        {
            if (session == null || isSynchronizing)
            {
                return;
            }

            session.SetMana(current, max);
        }

        private void HandleSessionChanged()
        {
            if (!isSynchronizing)
            {
                SynchronizeFromSession();
            }
        }
    }
}
