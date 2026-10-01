using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Babel.Runtime.Characters.Player
{
    [RequireComponent(typeof(PlayerRuntimeState))]
    public sealed class PlayerHealChannelController : MonoBehaviour
    {
        [SerializeField] private float holdDuration = 3f;
        [SerializeField] private int manaCost = 1;
        [SerializeField] private int healAmount = 1;
        [SerializeField] private Vector3 progressOffset = new Vector3(0f, 1.7f, 0f);

        private PlayerRuntimeState state;
        private GameSession session;
        private float holdTime;
        private bool spentThisPress;
        private PlayerController2D motor;
        public bool IsChanneling { get; private set; }
        public float Progress => Mathf.Clamp01(holdTime / holdDuration);
        public float Duration => holdDuration;
        private Canvas worldCanvas;
        private Image progressFill;
        private float progressFullWidth;
#if UNITY_EDITOR
        public bool TestHold;
#endif

        private void Awake()
        {
            state = GetComponent<PlayerRuntimeState>();
            motor = GetComponent<PlayerController2D>();
            ResolveSession();
            IsChanneling = false;
            BuildProgressBar();
            SetProgressVisible(false);
        }

        private void Update()
        {
            ResolveSession();
            IsChanneling = false;
            if (Bable.TowerLoading.Busy || (session != null && session.IsPaused))
            {
                holdTime = 0f;
                SetProgressVisible(false);
                return;
            }

            bool holding = Bable.GameInput.Held(Bable.GameAction.Heal);
#if UNITY_EDITOR
            holding |= TestHold;
#endif
            if (!holding)
            {
                spentThisPress = false;
                holdTime = 0f;
                SetProgressVisible(false);
                return;
            }

            if (spentThisPress) return;
            if (motor != null && (!motor.enabled || !motor.IsGrounded || motor.IsRecoiling || motor.IsDashing || Mathf.Abs(motor.MoveInput) > .01f))
            {
                holdTime = 0f;
                SetProgressVisible(false);
                return;
            }

            if (state == null || state.Health == null || state.Mana == null)
            {
                return;
            }

            if (state.Health.CurrentHealth >= state.Health.MaxHealth || state.Mana.CurrentMana < manaCost)
            {
                holdTime = 0f;
                SetProgressVisible(false);
                return;
            }

            if(holdTime<=0)Bable.CombatAudio.Play("drink_start",transform.position,.55f);
            holdTime += Time.deltaTime;
            IsChanneling = true;
            SetProgressVisible(true);
            UpdateProgressVisual(Mathf.Clamp01(holdTime / holdDuration));

            if (holdTime >= holdDuration)
            {
                spentThisPress = true;
                IsChanneling = false;
                if (session != null && session.TrySpendMana(manaCost))
                {
                    state.Health.Heal(healAmount);
                    Bable.CombatAudio.Play("drink_finish",transform.position,.7f);
                    ScreenMessagePresenter.ShowCenter($"Votive draught: +{healAmount} HP");
                }

                holdTime = 0f;
                SetProgressVisible(false);
            }
        }

        private void LateUpdate()
        {
            if (worldCanvas != null)
            {
                worldCanvas.transform.position = transform.position + progressOffset;
            }
        }

        private void ResolveSession()
        {
            if (session == null)
            {
                session = state != null && state.Session != null ? state.Session : (GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>());
            }
        }

        private void BuildProgressBar()
        {
            var canvasObject = new GameObject("HealProgressCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = progressOffset;
            worldCanvas = canvasObject.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 20;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1.4f, 0.25f);
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            var background = CreateBarImage(canvasObject.transform, "HealProgressBackground", new Vector2(140f, 18f), new Color(0f, 0f, 0f, 0.7f));
            background.rectTransform.anchoredPosition = Vector2.zero;
            progressFill = CreateBarImage(canvasObject.transform, "HealProgressFill", new Vector2(132f, 10f), new Color(0.35f, 0.95f, 0.55f, 0.95f));
            progressFill.rectTransform.anchoredPosition = Vector2.zero;
            progressFullWidth = progressFill.rectTransform.sizeDelta.x;
            UpdateProgressVisual(0f);
        }

        private static Image CreateBarImage(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private void UpdateProgressVisual(float ratio)
        {
            if (progressFill == null)
            {
                return;
            }

            var rect = progressFill.rectTransform;
            rect.sizeDelta = new Vector2(progressFullWidth * ratio, rect.sizeDelta.y);
        }

        private void SetProgressVisible(bool visible)
        {
            if (worldCanvas != null)
            {
                worldCanvas.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                UpdateProgressVisual(0f);
            }
        }
    }
}
