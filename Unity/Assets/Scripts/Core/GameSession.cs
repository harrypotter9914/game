using System;
using System.Collections.Generic;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Runes;
using UnityEngine;

namespace Babel.Runtime.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RuneInventory))]
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession instance;

        [SerializeField] private Vector2 defaultRespawnPoint = new Vector2(0f, 0f);

        private readonly HashSet<AbilityId> unlockedAbilities = new HashSet<AbilityId>();
        private readonly HashSet<string> rewardedBosses = new HashSet<string>();
        public int BossManaUpgrades => rewardedBosses.Count;
        public bool GrantBossMana(string bossId)
        {
            if (string.IsNullOrEmpty(bossId) || !rewardedBosses.Add(bossId)) return false;
            AddPermanentMaxMana(1);
            return true;
        }
        private RuneInventory runeInventory;
        private PlayerDefinition activePlayerDefinition;
        private int currentHealth;
        private int currentMana;
        private int maxHealth;
        private int maxMana;
        private int currentGold;
        private int bonusMaxHealth;
        private int bonusMaxMana;
        private float bonusDamageMultiplier;
        private float bonusAttackCooldownMultiplier = 1f;
        private float bonusAttackRangeMultiplier = 1f;
        private float attackCooldownMultiplier = 1f;
        private float attackRangeMultiplier = 1f;
        private float damageReflectAmount;
        private float damageReflectRadius;
        private float coinMagnetRadius;
        private float shopPriceMultiplier = 1f;
        private bool revivalConsumed;

        public static GameSession Instance => instance;

        public event Action StateChanged;

        [Serializable] public class CampaignState {
            public int health,mana,gold,bonusHealth,bonusMana;
            public float bonusDamage,bonusCooldown=1,bonusRange=1;
            public bool weapon,reviveSpent;
            public Vector2 checkpoint;
            public AbilityId[] abilities;
            public string[] bosses;
            public RuneId[] collected;
            public int[] equipped;
        }
        public CampaignState ExportCampaign(){
            var collected=new List<RuneId>();foreach(var r in runeInventory.CollectedRunes)collected.Add(r.RuneId);
            var slots=new List<int>();foreach(var r in runeInventory.ActiveSlots)slots.Add(r==null?-1:(int)r.RuneId);
            return new CampaignState{health=currentHealth,mana=currentMana,gold=currentGold,bonusHealth=bonusMaxHealth,bonusMana=bonusMaxMana,bonusDamage=bonusDamageMultiplier,bonusCooldown=bonusAttackCooldownMultiplier,bonusRange=bonusAttackRangeMultiplier,weapon=HasWeapon,reviveSpent=revivalConsumed,checkpoint=RespawnPoint,abilities=new List<AbilityId>(unlockedAbilities).ToArray(),bosses=new List<string>(rewardedBosses).ToArray(),collected=collected.ToArray(),equipped=slots.ToArray()};
        }
        public void ImportCampaign(CampaignState data){
            runeInventory.InventoryChanged-=HandleRuneInventoryChanged;
            unlockedAbilities.Clear();foreach(var a in data.abilities??Array.Empty<AbilityId>())if(Enum.IsDefined(typeof(AbilityId),a))unlockedAbilities.Add(a);
            rewardedBosses.Clear();foreach(var b in data.bosses??Array.Empty<string>())rewardedBosses.Add(b);
            HasWeapon=data.weapon;revivalConsumed=data.reviveSpent;RespawnPoint=data.checkpoint;
            currentGold=Mathf.Max(0,data.gold);bonusMaxHealth=Mathf.Max(0,data.bonusHealth);bonusMaxMana=Mathf.Max(0,data.bonusMana);
            bonusDamageMultiplier=Mathf.Max(0,data.bonusDamage);bonusAttackCooldownMultiplier=Mathf.Clamp(data.bonusCooldown,.1f,1);bonusAttackRangeMultiplier=Mathf.Max(1,data.bonusRange);
            runeInventory.Restore(data.collected,data.equipped);
            runeInventory.InventoryChanged+=HandleRuneInventoryChanged;
            RecalculateDerivedStats();currentHealth=Mathf.Clamp(data.health,1,maxHealth);currentMana=Mathf.Clamp(data.mana,0,maxMana);StateChanged?.Invoke();
        }

        public bool IsPaused { get; private set; }
        public bool HasWeapon {get;private set;}
        public void UnlockWeapon(){HasWeapon=true;StateChanged?.Invoke();}
        public int CurrentHealth => currentHealth;
        public int CurrentMana => currentMana;
        public int MaxHealth => maxHealth;
        public int MaxMana => maxMana;
        public int CurrentGold => currentGold;
        public Vector2 RespawnPoint { get; private set; }
        public RuneInventory RuneInventory => runeInventory;
        public float DamageMultiplier => CalculateDamageMultiplier();
        public float AttackCooldownMultiplier => attackCooldownMultiplier;
        public float AttackRangeMultiplier => attackRangeMultiplier;
        public float DamageReflectAmount => damageReflectAmount;
        public float DamageReflectRadius => damageReflectRadius;
        public bool RevivalConsumed => revivalConsumed;
        public float CoinMagnetRadius => coinMagnetRadius;
        public float ShopPriceMultiplier => shopPriceMultiplier;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            runeInventory = GetComponent<RuneInventory>();
            if (runeInventory == null)
            {
                runeInventory = gameObject.AddComponent<RuneInventory>();
            }

            runeInventory.InventoryChanged += HandleRuneInventoryChanged;
            RespawnPoint = defaultRespawnPoint;
            ResetForNewGame(activePlayerDefinition);
        }

        private void OnDestroy()
        {
            if (runeInventory != null)
            {
                runeInventory.InventoryChanged -= HandleRuneInventoryChanged;
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        public void ResetForNewGame(PlayerDefinition playerDefinition = null)
        {
            activePlayerDefinition = playerDefinition != null ? playerDefinition : activePlayerDefinition;
            if (runeInventory != null)
            {
                runeInventory.ResetInventory();
            }

            unlockedAbilities.Clear();
            rewardedBosses.Clear();
            HasWeapon=false;
            revivalConsumed = false;
            currentGold = 0;
            bonusMaxHealth = 0;
            bonusMaxMana = 0;
            bonusDamageMultiplier = 0f;
            bonusAttackCooldownMultiplier = 1f;
            bonusAttackRangeMultiplier = 1f;

            maxHealth = activePlayerDefinition != null ? activePlayerDefinition.BaseMaxHealth : 6;
            maxMana = activePlayerDefinition != null ? activePlayerDefinition.BaseMaxMana : 3;
            currentHealth = maxHealth;
            currentMana = maxMana;
            RespawnPoint = defaultRespawnPoint;
            RecalculateDerivedStats();
        }

        public void ConfigurePlayer(PlayerDefinition playerDefinition)
        {
            activePlayerDefinition = playerDefinition;
            RecalculateDerivedStats();
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            StateChanged?.Invoke();
        }

        public void SetRespawnPoint(Vector2 point)
        {
            RespawnPoint = point;
            StateChanged?.Invoke();
        }

        public bool HasAbility(AbilityId ability)
        {
            return unlockedAbilities.Contains(ability);
        }

        public void UnlockAbility(AbilityId ability)
        {
            if (unlockedAbilities.Add(ability))
            {
                StateChanged?.Invoke();
            }
        }

        public bool CollectRune(RuneDefinition rune)
        {
            return runeInventory != null && runeInventory.Collect(rune);
        }

        public bool TrySpendMana(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (currentMana < amount)
            {
                return false;
            }

            currentMana -= amount;
            StateChanged?.Invoke();
            return true;
        }

        public void RestoreMana(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentMana = Mathf.Min(maxMana, currentMana + amount);
            StateChanged?.Invoke();
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentGold += amount;
            StateChanged?.Invoke();
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (currentGold < amount)
            {
                return false;
            }

            currentGold -= amount;
            StateChanged?.Invoke();
            return true;
        }

        public void AddPermanentMaxHealth(int amount)
        {
            bonusMaxHealth += Mathf.Max(0, amount);
            RecalculateDerivedStats();
            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0, amount));
            StateChanged?.Invoke();
        }

        public void AddPermanentMaxMana(int amount)
        {
            bonusMaxMana += Mathf.Max(0, amount);
            RecalculateDerivedStats();
            currentMana = Mathf.Min(maxMana, currentMana + Mathf.Max(0, amount));
            StateChanged?.Invoke();
        }

        public void AddPermanentDamageMultiplier(float amount)
        {
            bonusDamageMultiplier += Mathf.Max(0f, amount);
            RecalculateDerivedStats();
        }

        public void MultiplyPermanentAttackCooldown(float multiplier)
        {
            bonusAttackCooldownMultiplier *= Mathf.Clamp(multiplier, 0.25f, 1f);
            RecalculateDerivedStats();
        }

        public void MultiplyPermanentAttackRange(float multiplier)
        {
            bonusAttackRangeMultiplier *= Mathf.Max(1f, multiplier);
            RecalculateDerivedStats();
        }

        public void SetMana(int current, int max)
        {
            maxMana = Mathf.Max(0, max);
            currentMana = Mathf.Clamp(current, 0, maxMana);
            StateChanged?.Invoke();
        }

        public void SetHealth(int current, int max)
        {
            maxHealth = Mathf.Max(1, max);
            currentHealth = Mathf.Clamp(current, 0, maxHealth);
            StateChanged?.Invoke();
        }

        public bool ConsumeReviveIfAvailable()
        {
            if (revivalConsumed) return false;
            if (runeInventory == null)
            {
                return false;
            }

            foreach (var rune in runeInventory.ActiveSlots)
            {
                if (rune != null && rune.EffectType == RuneEffectType.OneTimeRevive)
                {
                    revivalConsumed = true;
                    runeInventory.TryDeactivate(rune.RuneId);
                    currentHealth = Mathf.Max(1, Mathf.CeilToInt(maxHealth * 0.5f));
                    StateChanged?.Invoke();
                    return true;
                }
            }

            return false;
        }

        private void HandleRuneInventoryChanged()
        {
            RecalculateDerivedStats();
        }

        private void RecalculateDerivedStats()
        {
            var baseHealth = activePlayerDefinition != null ? activePlayerDefinition.BaseMaxHealth : 6;
            var baseMana = activePlayerDefinition != null ? activePlayerDefinition.BaseMaxMana : 3;
            var derivedHealth = baseHealth + bonusMaxHealth;
            var derivedMana = baseMana + bonusMaxMana;
            attackCooldownMultiplier = bonusAttackCooldownMultiplier;
            attackRangeMultiplier = bonusAttackRangeMultiplier;
            damageReflectAmount = 0f;
            damageReflectRadius = 0f;
            coinMagnetRadius = 0f;
            shopPriceMultiplier = 1f;

            if (runeInventory != null)
            {
                foreach (var rune in runeInventory.ActiveSlots)
                {
                    if (rune == null)
                    {
                        continue;
                    }

                    switch (rune.EffectType)
                    {
                        case RuneEffectType.BonusMaxHealth:
                            derivedHealth += Mathf.RoundToInt(rune.Magnitude);
                            break;
                        case RuneEffectType.AttackSpeedBoost:
                            attackCooldownMultiplier *= 1f / Mathf.Max(1f, rune.Magnitude);
                            break;
                        case RuneEffectType.AttackRangeBoost:
                            attackRangeMultiplier *= Mathf.Max(1f, rune.Magnitude);
                            break;
                        case RuneEffectType.DamageReflect:
                            damageReflectAmount = Mathf.Max(damageReflectAmount, Mathf.Clamp01(rune.Magnitude));
                            damageReflectRadius = Mathf.Max(damageReflectRadius, rune.EffectRadius);
                            break;
                        case RuneEffectType.CoinMagnet:
                            coinMagnetRadius = Mathf.Max(coinMagnetRadius, Mathf.Max(1.5f, rune.Magnitude));
                            break;
                        case RuneEffectType.ShopDiscount:
                            shopPriceMultiplier = Mathf.Min(shopPriceMultiplier, Mathf.Clamp(rune.Magnitude, 0.1f, 1f));
                            break;
                    }
                }
            }

            maxHealth = Mathf.Max(1, derivedHealth);
            maxMana = Mathf.Max(0, derivedMana);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            StateChanged?.Invoke();
        }

        private float CalculateDamageMultiplier()
        {
            var result = 1f + bonusDamageMultiplier;
            if (runeInventory == null)
            {
                return result;
            }

            foreach (var rune in runeInventory.ActiveSlots)
            {
                if (rune == null)
                {
                    continue;
                }

                switch (rune.EffectType)
                {
                    case RuneEffectType.LowHealthDamageBoost:
                        if (currentHealth > 0 && currentHealth <= maxHealth * RuneDefinition.RingsCriticalThreshold)
                        {
                            result *= rune.CriticalDamageMultiplier;
                        }
                        else if (currentHealth > 0 && currentHealth <= maxHealth * RuneDefinition.RingsLowThreshold)
                        {
                            result *= Mathf.Max(1f, rune.Magnitude);
                        }
                        break;
                    case RuneEffectType.BonusMaxHealth:
                        result *= RuneDefinition.RomanceDamageMultiplier;
                        break;
                }
            }

            return result;
        }
    }
}

