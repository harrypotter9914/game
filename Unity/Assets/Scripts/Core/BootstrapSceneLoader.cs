using UnityEngine;
using UnityEngine.SceneManagement;

namespace Babel.Runtime.Core
{
    public sealed class BootstrapSceneLoader : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "Gameplay_Main";
        [SerializeField] private bool loadOnStart = true;

        private bool hasLoaded;

        private void Start()
        {
            if (loadOnStart)
            {
                LoadGameplayScene();
            }
        }

        public void LoadGameplayScene()
        {
            if (hasLoaded || string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                return;
            }

            hasLoaded = true;
            SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
    }
}
