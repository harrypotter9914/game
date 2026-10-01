using System.Collections.Generic;
using System.Text;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;
using Babel.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Babel.Runtime.Shop
{
    public sealed class ShopMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text listText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text footerText;

        private readonly List<ShopItemDefinition> currentItems = new List<ShopItemDefinition>();
        private readonly HashSet<ShopItemDefinition> purchasedItems = new HashSet<ShopItemDefinition>();
        private GameFlowController flow;
        private GameSession session;
        private bool isOpen;
        private int selectedIndex;
        private int lastToggleFrame = -1000;
        private ShopWindowView window;

        public bool IsOpen => isOpen;
        public bool ConsumedToggleThisFrame => Time.frameCount == lastToggleFrame;

        private void Awake()
        {
            flow = FindObjectOfType<GameFlowController>();
            session = GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>();
            window=gameObject.AddComponent<ShopWindowView>();
            window.Build(panelRoot, SelectItem, TryBuySelected, Close);
            SetVisible(false);
        }

        private void Update()
        {
            HandleInput(Bable.GameInput.Down(Bable.GameAction.Up),Bable.GameInput.Down(Bable.GameAction.Down),Bable.GameInput.Down(Bable.GameAction.Submit),Bable.GameInput.Down(Bable.GameAction.Pause)||Bable.GameInput.Down(Bable.GameAction.Back)||Bable.GameInput.Down(Bable.GameAction.Interact));
        }

        public void HandleInput(bool up,bool down,bool purchase,bool close)
        {
            if (!isOpen || ConsumedToggleThisFrame)
            {
                return;
            }

            if (up)
            {
                SelectItem(Mathf.Max(0, selectedIndex - 1));
            }
            else if (down)
            {
                SelectItem(Mathf.Min(currentItems.Count - 1, selectedIndex + 1));
            }
            else if (purchase)
            {
                TryBuySelected();
            }
            else if (close)
            {
                Close();
            }
        }

        public void Open(IReadOnlyList<ShopItemDefinition> items)
        {
            currentItems.Clear();
            if (items != null)
            {
                currentItems.AddRange(items);
            }

            selectedIndex = 0;
            isOpen = true;
            Bable.CombatAudio.UI("ui_confirm");
            lastToggleFrame = Time.frameCount;
            if (flow != null)
            {
                flow.SetPaused(true);
            }

            SetVisible(true);
            Refresh();
        }

        public void Close()
        {
            if(isOpen)Bable.CombatAudio.UI("ui_cancel");
            isOpen = false;
            lastToggleFrame = Time.frameCount;
            SetVisible(false);
            if (flow != null)
            {
                flow.SetPaused(false);
            }
        }

        private void TryBuySelected()
        {
            if (session == null || currentItems.Count == 0)
            {
                return;
            }

            var item = currentItems[selectedIndex];
            if((item.EffectType==ShopItemEffectType.RestoreHealth&&session.CurrentHealth>=session.MaxHealth)||(item.EffectType==ShopItemEffectType.RestoreMana&&session.CurrentMana>=session.MaxMana)){
                Bable.CombatAudio.UI("ui_deny");window.Message("Already full — keep your gold.");return;
            }
            if (item.OneTimePurchase && (purchasedItems.Contains(item)||Bable.CampaignStore.Purchased(item.name)))
            {
                ScreenMessagePresenter.ShowCenter("Sold out");
                window.Message("This relic has already been purchased.");
                Bable.CombatAudio.UI("ui_deny");
                return;
            }

            if (!session.TrySpendGold(GetPrice(item)))
            {
                ScreenMessagePresenter.ShowCenter("Not enough gold");
                window.Message("You need more gold for this item.");
                Bable.CombatAudio.UI("ui_deny");
                return;
            }

            ApplyItem(item);
            Bable.CombatAudio.UI("ui_purchase");
            if (item.OneTimePurchase)
            {
                purchasedItems.Add(item);Bable.CampaignStore.Purchase(item.name);
            }

            ScreenMessagePresenter.ShowCenter($"Bought {item.DisplayName}");
            Refresh();
            window.Message("Purchased: "+item.DisplayName);
        }

        private void SelectItem(int index){if(index<0||index>=currentItems.Count)return;if(selectedIndex!=index)Bable.CombatAudio.UI("ui_select",.5f);selectedIndex=index;Refresh();}

        private void ApplyItem(ShopItemDefinition item)
        {
            switch (item.EffectType)
            {
                case ShopItemEffectType.RestoreHealth:
                    session.SetHealth(session.CurrentHealth+Mathf.RoundToInt(item.Magnitude),session.MaxHealth);break;
                case ShopItemEffectType.RestoreMana:
                    session.RestoreMana(Mathf.RoundToInt(item.Magnitude));break;
                case ShopItemEffectType.MaxHealth:
                    session.AddPermanentMaxHealth(Mathf.RoundToInt(item.Magnitude));
                    break;
                case ShopItemEffectType.MaxMana:
                    session.AddPermanentMaxMana(Mathf.RoundToInt(item.Magnitude));
                    break;
                case ShopItemEffectType.DamageBoost:
                    session.AddPermanentDamageMultiplier(item.Magnitude);
                    break;
                case ShopItemEffectType.AttackSpeed:
                    session.MultiplyPermanentAttackCooldown(item.Magnitude);
                    break;
                case ShopItemEffectType.AttackRange:
                    session.MultiplyPermanentAttackRange(item.Magnitude);
                    break;
            }
        }

        private void Refresh()
        {
            if (!isOpen)
            {
                return;
            }
            foreach(var item in currentItems)if(Bable.CampaignStore.Purchased(item.name))purchasedItems.Add(item);window.Render(currentItems,selectedIndex,session.CurrentGold,purchasedItems,GetPrice);
        }

        private int GetPrice(ShopItemDefinition item)
        {
            var multiplier = session != null ? session.ShopPriceMultiplier : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(item.Price * multiplier));
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }
    }
}
