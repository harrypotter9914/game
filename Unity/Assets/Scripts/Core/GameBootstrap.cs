using UnityEngine;

namespace Babel.Runtime.Core
{
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (GetComponent<GameSession>() == null)
            {
                gameObject.AddComponent<GameSession>();
            }

            if (GetComponent<GameFlowController>() == null)
            {
                gameObject.AddComponent<GameFlowController>();
            }
        }
    }
}
