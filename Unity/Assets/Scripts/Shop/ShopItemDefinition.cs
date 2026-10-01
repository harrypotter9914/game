using UnityEngine;

namespace Babel.Runtime.Shop
{
    [CreateAssetMenu(menuName = "Babel/Shop/Shop Item", fileName = "ShopItem")]
    public sealed class ShopItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private ShopItemEffectType effectType;
        [SerializeField] private int price = 5;
        [SerializeField] private float magnitude = 1f;
        [SerializeField] private bool oneTimePurchase = true;
        [SerializeField] private Sprite icon;

        public string DisplayName => displayName;
        public string Description => description;
        public ShopItemEffectType EffectType => effectType;
        public int Price => price;
        public float Magnitude => magnitude;
        public bool OneTimePurchase => oneTimePurchase;
        public Sprite Icon => icon;
    }
}
