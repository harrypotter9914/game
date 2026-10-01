using System;
using System.Collections.Generic;
using UnityEngine;

namespace Babel.Runtime.Runes
{
    public sealed class RuneInventory : MonoBehaviour
    {
        [SerializeField] private int maxActiveSlots = 4;

        private readonly HashSet<RuneId> collectedRuneIds = new HashSet<RuneId>();
        private readonly List<RuneDefinition> collectedRunes = new List<RuneDefinition>();
        private readonly List<RuneDefinition> activeSlots = new List<RuneDefinition>();

        public IReadOnlyList<RuneDefinition> CollectedRunes => collectedRunes;
        public IReadOnlyList<RuneDefinition> ActiveSlots => activeSlots;
        public event Action InventoryChanged;
        public void Restore(RuneId[] collected,int[] equipped){
            ResetInventory();var definitions=Resources.LoadAll<RuneDefinition>("Bable/Runes");
            foreach(var id in collected??Array.Empty<RuneId>()){var r=Array.Find(definitions,d=>d.RuneId==id);if(r!=null)Collect(r);}
            for(int i=0;i<Mathf.Min(4,equipped?.Length??0);i++){var r=Array.Find(definitions,d=>(int)d.RuneId==equipped[i]);if(r!=null)EquipAt(r,i);}
            InventoryChanged?.Invoke();
        }

        private void Awake()
        {
            ResetInventory();
        }

        public void ResetInventory()
        {
            collectedRuneIds.Clear();
            collectedRunes.Clear();
            activeSlots.Clear();
            for (var i = 0; i < maxActiveSlots; i++)
            {
                activeSlots.Add(null);
            }

            InventoryChanged?.Invoke();
        }

        public void SetMaxActiveSlots(int slotCount)
        {
            maxActiveSlots = Mathf.Max(1, slotCount);
            while (activeSlots.Count < maxActiveSlots)
            {
                activeSlots.Add(null);
            }

            while (activeSlots.Count > maxActiveSlots)
            {
                activeSlots.RemoveAt(activeSlots.Count - 1);
            }

            InventoryChanged?.Invoke();
        }

        public bool Collect(RuneDefinition rune)
        {
            if (rune == null || !collectedRuneIds.Add(rune.RuneId))
            {
                return false;
            }

            collectedRunes.Add(rune);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool IsCollected(RuneId runeId)
        {
            return collectedRuneIds.Contains(runeId);
        }

        public bool IsActive(RuneId runeId)
        {
            return activeSlots.Exists(slot => slot != null && slot.RuneId == runeId);
        }

        public bool TryActivateToEmptySlot(RuneDefinition rune)
        {
            if (rune == null || IsSpent(rune) || !IsCollected(rune.RuneId) || IsActive(rune.RuneId))
            {
                return false;
            }

            var emptySlot = activeSlots.FindIndex(slot => slot == null);
            if (emptySlot < 0)
            {
                return false;
            }

            activeSlots[emptySlot] = rune;
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TryDeactivate(RuneId runeId)
        {
            var slotIndex = activeSlots.FindIndex(slot => slot != null && slot.RuneId == runeId);
            return TryDeactivateSlot(slotIndex);
        }

        public bool TryDeactivateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= activeSlots.Count || activeSlots[slotIndex] == null)
            {
                return false;
            }

            activeSlots[slotIndex] = null;
            InventoryChanged?.Invoke();
            return true;
        }

        public bool ToggleActivation(RuneDefinition rune)
        {
            if (rune == null)
            {
                return false;
            }

            return IsActive(rune.RuneId) ? TryDeactivate(rune.RuneId) : TryActivateToEmptySlot(rune);
        }

        // Moving an equipped rune exchanges slots atomically; no duplicate effects or
        // temporary stat loss is emitted between removing and inserting the rune.
        public bool EquipAt(RuneDefinition rune, int slotIndex)
        {
            if (rune == null || IsSpent(rune) || !IsCollected(rune.RuneId) || slotIndex < 0 || slotIndex >= activeSlots.Count) return false;
            int old = activeSlots.FindIndex(s => s != null && s.RuneId == rune.RuneId);
            if (old == slotIndex) return true;
            if (old >= 0) activeSlots[old] = activeSlots[slotIndex];
            activeSlots[slotIndex] = rune;
            InventoryChanged?.Invoke();
            return true;
        }
        public bool IsSpent(RuneDefinition rune) => rune != null && rune.EffectType == RuneEffectType.OneTimeRevive && Core.GameSession.Instance != null && Core.GameSession.Instance.RevivalConsumed;
    }
}
