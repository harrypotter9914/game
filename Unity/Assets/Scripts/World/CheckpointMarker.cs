using UnityEngine;

namespace Babel.Runtime.World
{
    public sealed class CheckpointMarker : MonoBehaviour
    {
        [SerializeField] private bool setAsSpawnOnAwake;

        public Vector2 Position => transform.position;

        private void Awake()
        {
            if (setAsSpawnOnAwake)
            {
                var existing = FindObjectOfType<CheckpointService>();
                if (existing != null)
                {
                    existing.RegisterCheckpoint(this);
                }
            }
        }
    }
}
