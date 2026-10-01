using UnityEngine;

namespace Babel.Runtime.Runes
{
    [CreateAssetMenu(menuName = "Babel/Runes/Rune Definition", fileName = "RuneDefinition")]
    public sealed class RuneDefinition : ScriptableObject
    {
        [SerializeField] private RuneId runeId;
        [SerializeField] private RuneEffectType effectType;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private float magnitude = 1f;
        [TextArea] [SerializeField] private string lore;
        [SerializeField] private float effectRadius = 2.5f;
        public const float RomanceDamageMultiplier = .85f;
        public const float RingsLowThreshold = .5f;
        public const float RingsCriticalThreshold = .25f;

        public RuneId RuneId => runeId;
        public RuneEffectType EffectType => effectType;
        public string DisplayName => displayName;
        public Sprite Icon => Resources.Load<Sprite>("Bable/NewArt/Release51/Rune"+runeId);
        public string Description => EffectDescription + "\n\n" + Lore;
        public float Magnitude => magnitude;
        public float EffectRadius => effectRadius;
        public string Lore => string.IsNullOrEmpty(lore) ? description : lore;
        public float CriticalDamageMultiplier => magnitude + .5f;
        public string EffectDescription
        {
            get
            {
                switch (effectType)
                {
                    case RuneEffectType.BonusMaxHealth: return "+" + Mathf.RoundToInt(magnitude) + " maximum health; -15% attack damage (minimum 1 damage).";
                    case RuneEffectType.LowHealthDamageBoost: return "+" + Mathf.RoundToInt((magnitude-1)*100) + "% damage at half health or less; +" + Mathf.RoundToInt((CriticalDamageMultiplier-1)*100) + "% at a quarter or less.";
                    case RuneEffectType.AttackSpeedBoost: return "+" + Mathf.RoundToInt((magnitude-1)*100) + "% attack speed. Sword animation and recovery speed up together.";
                    case RuneEffectType.AttackRangeBoost: return "+" + Mathf.RoundToInt((magnitude-1)*100) + "% sword and shockwave reach. Attacks still stop at walls.";
                    case RuneEffectType.CoinMagnet: return "Draws nearby coins toward you within " + magnitude.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture) + " tiles. Cannot pull them through walls.";
                    case RuneEffectType.OneTimeRevive: return "Revives you where you fall with half health. One use per journey; re-equipping does not recharge it.";
                    case RuneEffectType.ShopDiscount: return "Shop prices are " + Mathf.RoundToInt((1-magnitude)*100) + "% lower while equipped.";
                    case RuneEffectType.DamageReflect: return "When hurt, returns " + Mathf.RoundToInt(magnitude*100) + "% of damage (minimum 1) to nearby enemies. Cannot strike through walls.";
                    default: return description;
                }
            }
        }
    }
}
