using Babel.Runtime.Core;
using UnityEngine;

namespace Babel.Runtime.World
{
    public sealed class CheckpointService : MonoBehaviour
    {
        [SerializeField] private CheckpointMarker currentCheckpoint;

        private GameSession session;

        public CheckpointMarker CurrentCheckpoint => currentCheckpoint;

        private void Awake()
        {
            session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            if (currentCheckpoint != null && session != null)
            {
                session.SetRespawnPoint(currentCheckpoint.Position);
            }
        }

        public void ClearCheckpoint(){currentCheckpoint=null;}

        public void RegisterCheckpoint(CheckpointMarker marker)
        {
            if (marker == null)
            {
                return;
            }

            currentCheckpoint = marker;
            if (session != null)
            {
                session.SetRespawnPoint(marker.Position);
                // Recording a respawn location is not healing. In particular the
                // bridge landing must preserve the resources the player had above.
            }
        }
    }
}
