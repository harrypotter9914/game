using System;
using UnityEngine;

namespace Babel.Runtime.Combat
{
    public sealed class ManaComponent : MonoBehaviour
    {
        [SerializeField] private int maxMana = 3;

        public int CurrentMana { get; private set; }
        public int MaxMana => maxMana;
        public event Action<int, int> Changed;

        private void Awake()
        {
            CurrentMana = maxMana;
        }

        public void Configure(int newMaxMana, int? currentManaOverride = null)
        {
            maxMana = Mathf.Max(0, newMaxMana);
            CurrentMana = currentManaOverride.HasValue ? Mathf.Clamp(currentManaOverride.Value, 0, maxMana) : Mathf.Min(CurrentMana, maxMana);
            Changed?.Invoke(CurrentMana, maxMana);
        }

        public bool TrySpend(int amount)
        {
            if (CurrentMana < amount)
            {
                return false;
            }

            CurrentMana -= amount;
            Changed?.Invoke(CurrentMana, maxMana);
            return true;
        }

        public void Restore(int amount)
        {
            CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
            Changed?.Invoke(CurrentMana, maxMana);
        }
    }
}
