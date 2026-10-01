using System.Text;
using Babel.Runtime.Core;
using Babel.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Babel.Runtime.Runes
{
    public sealed class RuneMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text inventoryText;
        [SerializeField] private TMP_Text slotsText;
        [SerializeField] private TMP_Text footerText;

        private GameSession session;
        private GameFlowController flow;
        private RuneInventory inventory;
        private int selectedRow;
        private int selectedColumn;
        private bool isOpen;

        private void Awake()
        {
            ResolveReferences();
            SetVisible(false);
        }

        private void Start()
        {
            ResolveReferences();
            RefreshView();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (inventory != null)
            {
                inventory.InventoryChanged += RefreshView;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= RefreshView;
            }
        }

        private void Update()
        {
            ResolveReferences();
            if (Bable.GameInput.Down(Bable.GameAction.Runes))
            {
                if (!isOpen && session != null && session.IsPaused) return;
                ToggleMenu();
            }

            if (!isOpen || inventory == null)
            {
                return;
            }

            if (Bable.GameInput.Down(Bable.GameAction.Up))
            {
                selectedRow = 0;
                ClampSelection();
                RefreshView();
            }
            else if (Bable.GameInput.Down(Bable.GameAction.Down))
            {
                selectedRow = 1;
                ClampSelection();
                RefreshView();
            }
            else if (Bable.GameInput.Down(Bable.GameAction.Left))
            {
                selectedColumn--;
                ClampSelection();
                RefreshView();
            }
            else if (Bable.GameInput.Down(Bable.GameAction.Right))
            {
                selectedColumn++;
                ClampSelection();
                RefreshView();
            }
            else if (Bable.GameInput.Down(Bable.GameAction.Submit))
            {
                ConfirmSelection();
            }
        }

        private void ResolveReferences()
        {
            if (panelRoot == null)
            {
                var panelTransform = transform.Find("RunePanel");
                if (panelTransform != null)
                {
                    panelRoot = panelTransform.gameObject;
                }
            }

            if (inventoryText == null && panelRoot != null)
            {
                var textTransform = panelRoot.transform.Find("RuneInventoryText");
                if (textTransform != null)
                {
                    inventoryText = textTransform.GetComponent<TMP_Text>();
                }
            }

            if (slotsText == null && panelRoot != null)
            {
                var textTransform = panelRoot.transform.Find("RuneSlotsText");
                if (textTransform != null)
                {
                    slotsText = textTransform.GetComponent<TMP_Text>();
                }
            }

            if (footerText == null && panelRoot != null)
            {
                var textTransform = panelRoot.transform.Find("RuneFooter");
                if (textTransform != null)
                {
                    footerText = textTransform.GetComponent<TMP_Text>();
                }
            }

            if (session == null)
            {
                session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            }

            if (flow == null)
            {
                flow = FindObjectOfType<GameFlowController>();
            }

            var resolvedInventory = session != null ? session.RuneInventory : FindObjectOfType<RuneInventory>();
            if (resolvedInventory != inventory)
            {
                if (inventory != null)
                {
                    inventory.InventoryChanged -= RefreshView;
                }

                inventory = resolvedInventory;
                if (inventory != null && isActiveAndEnabled)
                {
                    inventory.InventoryChanged += RefreshView;
                }
            }
        }

        private void ToggleMenu()
        {
            isOpen = !isOpen;
            SetVisible(isOpen);
            if (flow != null)
            {
                flow.SetPaused(isOpen);
            }

            if (isOpen)
            {
                selectedRow = 0;
                selectedColumn = 0;
                ClampSelection();
                RefreshView();
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
                if (visible)
                {
                    panelRoot.transform.SetAsLastSibling();
                }
            }
        }

        private void ClampSelection()
        {
            if (inventory == null)
            {
                selectedColumn = 0;
                return;
            }

            var maxColumns = selectedRow == 0 ? Mathf.Max(1, inventory.CollectedRunes.Count) : Mathf.Max(1, inventory.ActiveSlots.Count);
            selectedColumn = Mathf.Clamp(selectedColumn, 0, maxColumns - 1);
        }

        private void ConfirmSelection()
        {
            if(!Bable.RuneAttunement.CanChange){ScreenMessagePresenter.ShowCenter("Return to an altar to change runes.");return;}
            if (inventory == null)
            {
                return;
            }

            if (selectedRow == 0)
            {
                if (selectedColumn < inventory.CollectedRunes.Count)
                {
                    var rune = inventory.CollectedRunes[selectedColumn];
                    if (inventory.IsActive(rune.RuneId))
                    {
                        inventory.TryDeactivate(rune.RuneId);
                        ScreenMessagePresenter.ShowCenter($"Deactivated {rune.DisplayName}");
                    }
                    else if (inventory.TryActivateToEmptySlot(rune))
                    {
                        ScreenMessagePresenter.ShowCenter($"Activated {rune.DisplayName}");
                    }
                    else
                    {
                        ScreenMessagePresenter.ShowCenter("No empty rune slot");
                    }
                }
            }
            else if (selectedColumn < inventory.ActiveSlots.Count)
            {
                var rune = inventory.ActiveSlots[selectedColumn];
                if (rune != null && inventory.TryDeactivateSlot(selectedColumn))
                {
                    ScreenMessagePresenter.ShowCenter($"Removed {rune.DisplayName}");
                }
            }

            RefreshView();
        }

        private void RefreshView()
        {
            if (inventory == null)
            {
                return;
            }

            ClampSelection();
            if (inventoryText != null)
            {
                var builder = new StringBuilder();
                builder.AppendLine("Rune Inventory");
                for (var index = 0; index < inventory.CollectedRunes.Count; index++)
                {
                    var rune = inventory.CollectedRunes[index];
                    var marker = selectedRow == 0 && selectedColumn == index ? ">" : " ";
                    var state = inventory.IsActive(rune.RuneId) ? "[ON]" : "[  ]";
                    builder.AppendLine($"{marker} {index + 1}. {state} {rune.DisplayName}");
                }

                if (inventory.CollectedRunes.Count == 0)
                {
                    builder.AppendLine("  No runes collected");
                }

                inventoryText.text = builder.ToString();
            }

            if (slotsText != null)
            {
                var builder = new StringBuilder();
                builder.AppendLine("Active Slots");
                for (var i = 0; i < inventory.ActiveSlots.Count; i++)
                {
                    var marker = selectedRow == 1 && selectedColumn == i ? ">" : " ";
                    var rune = inventory.ActiveSlots[i];
                    builder.AppendLine($"{marker} Slot {i + 1}: {(rune != null ? rune.DisplayName : "Empty")}");
                }

                slotsText.text = builder.ToString();
            }

            if (footerText != null)
            {
                footerText.text = "Arrows: Select   Enter: Activate/Remove   Tab: Close";
            }
        }
    }
}
