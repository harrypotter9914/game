using Babel.Runtime.Characters.Player;
using Babel.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Babel.Runtime.Shop
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ShopkeeperController : MonoBehaviour
    {
        [SerializeField] private ShopItemDefinition[] stock;
        [SerializeField] private ShopMenuController shopMenu;
        [SerializeField] private TMP_Text promptLabel;

        private bool playerInRange;
        private Transform customer;
        private SpriteRenderer visual;
        private bool greeted;
        readonly System.Collections.Generic.HashSet<Collider2D> contacts=new();

        private void Awake()
        {
            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
            visual=transform.Find("Merchant visual")?.GetComponent<SpriteRenderer>();
            if(shopMenu==null)shopMenu=FindFirstObjectByType<ShopMenuController>();
            SetPrompt(false);
        }

        private void Update()
        {
            if(promptLabel!=null)promptLabel.text=Bable.GameInput.Hint(Bable.GameAction.Interact)+"  Trade";
            if(Bable.TowerLoading.Busy)return;
            if(customer!=null&&visual!=null&&Mathf.Abs(customer.position.x-transform.position.x)>.7f)
                visual.flipX=customer.position.x<transform.position.x;
            if (Babel.Runtime.Core.GameSession.Instance != null && Babel.Runtime.Core.GameSession.Instance.IsPaused) return;
            if (!playerInRange || shopMenu == null || shopMenu.IsOpen || shopMenu.ConsumedToggleThisFrame)
            {
                return;
            }

            if (Bable.GameInput.Down(Bable.GameAction.Interact)&&!Bable.PickupInteractor.HasTarget&&!Bable.PickupInteractor.ConsumedThisFrame)
            {
                TryTrade();
            }
        }

        public bool TryTrade(){
            if(Bable.TowerLoading.Busy||!playerInRange||shopMenu==null||shopMenu.IsOpen||shopMenu.ConsumedToggleThisFrame||Bable.TowerDialogue.StoryActive)return false;
            if(Babel.Runtime.Core.GameSession.Instance!=null&&Babel.Runtime.Core.GameSession.Instance.IsPaused)return false;
            shopMenu.Open(stock);return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerRuntimeState>() == null)
            {
                return;
            }

            contacts.Add(other);
            bool arriving=!playerInRange;
            playerInRange = true;
            customer=other.GetComponentInParent<PlayerRuntimeState>().transform;
            SetPrompt(true);
            if(arriving&&!greeted&&!Bable.TowerLoading.Busy){Bable.TowerDialogue.Speak(transform.position.y>40?"merchant_high":transform.position.y<0?"merchant_low":"merchant_mid",transform);greeted=true;}
            Bable.NarrativeGuidance.Instance?.Teach("trade","The wayfarer's exchange","Approach a merchant to inspect provisions and relics. Choose an item, then confirm your purchase.","E  Trade / Leave     ARROWS  Select     ENTER  Buy");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerRuntimeState>() == null)
            {
                return;
            }

            contacts.Remove(other);
            if(contacts.Count>0)return;
            playerInRange = false;
            customer=null;
            SetPrompt(false);
            if (shopMenu != null && shopMenu.IsOpen)
            {
                shopMenu.Close();
            }
        }

        private void SetPrompt(bool visible)
        {
            if (promptLabel != null)
            {
                promptLabel.gameObject.SetActive(visible);
            }
        }
    }
}
