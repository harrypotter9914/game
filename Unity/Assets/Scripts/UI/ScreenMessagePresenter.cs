using TMPro;
using UnityEngine;

namespace Babel.Runtime.UI
{
    public sealed class ScreenMessagePresenter : MonoBehaviour
    {
        private static ScreenMessagePresenter instance;

        [SerializeField] private TMP_Text centerMessage;
        [SerializeField] private float centerDuration = 1.8f;
        [SerializeField] private TMP_Text worldMessagePrefab;

        private float centerHideTime;

        private void Awake()
        {
            instance = this;
            if (GetComponent<Bable.NarrativeGuidance>() == null) gameObject.AddComponent<Bable.NarrativeGuidance>();
            if (centerMessage != null)
            {
                centerMessage.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (centerMessage != null && centerMessage.gameObject.activeSelf && Time.unscaledTime >= centerHideTime)
            {
                centerMessage.gameObject.SetActive(false);
            }
        }

        public static void ShowCenter(string message)
        {
            ShowCenter(message, instance != null ? instance.centerDuration : 1.8f);
        }

        public static void ShowCenter(string message, float duration)
        {
            if (Bable.NarrativeGuidance.Notice(message, duration)) return;
            if (instance == null || instance.centerMessage == null)
            {
                return;
            }

            instance.centerMessage.text = message;
            instance.centerMessage.gameObject.SetActive(true);
            instance.centerHideTime = Time.unscaledTime + Mathf.Max(0.25f, duration);
        }

        public static void ShowWorld(string message, Vector3 worldPosition, Color color)
        {
            ShowWorld(message, worldPosition, color, 0.7f);
        }

        public static void ShowWorld(string message, Vector3 worldPosition, Color color, float duration)
        {
            if (instance == null || instance.worldMessagePrefab == null || Camera.main == null)
            {
                return;
            }

            var spawned = Instantiate(instance.worldMessagePrefab, instance.worldMessagePrefab.transform.parent);
            spawned.gameObject.SetActive(true);
            spawned.text = message;
            spawned.color = color;
            spawned.transform.position = Camera.main.WorldToScreenPoint(worldPosition);
            instance.StartCoroutine(FadeAndDestroy(spawned, duration));
        }

        private static System.Collections.IEnumerator FadeAndDestroy(TMP_Text label, float duration)
        {
            var actualDuration = Mathf.Max(0.25f, duration);
            var start = Time.unscaledTime;
            var initial = label.color;
            while (Time.unscaledTime - start < actualDuration)
            {
                var t = (Time.unscaledTime - start) / actualDuration;
                label.transform.position += Vector3.up * (20f * Time.unscaledDeltaTime);
                label.color = new Color(initial.r, initial.g, initial.b, 1f - t);
                yield return null;
            }

            if (label != null)
            {
                Destroy(label.gameObject);
            }
        }
    }
}

