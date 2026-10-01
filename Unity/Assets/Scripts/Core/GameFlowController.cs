using System;
using Babel.Runtime.Characters.Player;
using UnityEngine;

namespace Babel.Runtime.Core
{
    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameSession session;

        public bool IsPaused { get; private set; }
        public event Action<bool> PauseChanged;

        private void Awake()
        {
            session = session != null ? session : GetComponent<GameSession>();
            if (session == null)
            {
                session = FindObjectOfType<GameSession>();
            }
        }

        public void StartNewGame(PlayerDefinition playerDefinition = null)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (session != null)
            {
                session.ResetForNewGame(playerDefinition);
            }

            PauseChanged?.Invoke(false);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (session != null)
            {
                session.SetPaused(paused);
            }
            PauseChanged?.Invoke(paused);
        }
    }
}
