using Babel.Runtime.Core;
using Babel.Runtime.Shop;
using TMPro;
using UnityEngine;

namespace Babel.Runtime.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text optionsText;
        [SerializeField] private TMP_Text footerText;

        private GameFlowController flow;
        private ShopMenuController shopMenu;
        private readonly string[] options = { "Resume", "Respawn" };
        private int selectedIndex;
        private bool isOpen;

        private void Awake()
        {
            flow = FindObjectOfType<GameFlowController>();
            shopMenu = FindObjectOfType<ShopMenuController>();
            SetVisible(false);
        }

        private void Update()
        {
            if (shopMenu == null)
            {
                shopMenu = FindObjectOfType<ShopMenuController>();
            }

            if (Bable.GameInput.Down(Bable.GameAction.Pause))
            {
                if (shopMenu != null && (shopMenu.IsOpen || shopMenu.ConsumedToggleThisFrame))
                {
                    return;
                }

                ToggleMenu();
            }

            if (!isOpen)
            {
                return;
            }

            if (Bable.GameInput.Down(Bable.GameAction.Up))
            {
                selectedIndex = Mathf.Max(0, selectedIndex - 1);
                Refresh();
            }
            else if (Bable.GameInput.Down(Bable.GameAction.Down))
            {
                selectedIndex = Mathf.Min(options.Length - 1, selectedIndex + 1);
                Refresh();
            }
            else if (Bable.GameInput.Down(Bable.GameAction.Submit))
            {
                Confirm();
            }
        }

        private void ToggleMenu()
        {
            isOpen = !isOpen;
            if (flow == null)
            {
                flow = FindObjectOfType<GameFlowController>();
            }

            if (flow != null)
            {
                flow.SetPaused(isOpen);
            }

            SetVisible(isOpen);
            if (isOpen)
            {
                selectedIndex = 0;
                Refresh();
            }
        }

        private void Confirm()
        {
            switch (selectedIndex)
            {
                case 0:
                    ToggleMenu();
                    break;
                case 1:
                    var respawn = FindObjectOfType<Babel.Runtime.World.PlayerRespawnController>();
                    if (respawn != null)
                    {
                        respawn.Respawn();
                    }
                    ToggleMenu();
                    break;
            }
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void Refresh()
        {
            if (optionsText != null)
            {
                optionsText.text = $"{(selectedIndex == 0 ? ">" : " ")} {options[0]}\n{(selectedIndex == 1 ? ">" : " ")} {options[1]}";
            }

            if (footerText != null)
            {
                footerText.text = "Esc: Close   Arrows: Select   Enter: Confirm";
            }
        }
    }
}
