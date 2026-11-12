using System;
using UnityEngine;

namespace WindPet
{
    /// <summary>The player's money, in euros with cents. Earned with bills, spent on the dryer, lights and a new turbine.</summary>
    public class Wallet : MonoBehaviour
    {
        public PetConfig config;

        public float Money { get; private set; }
        public event Action<float> MoneyChanged;

        void Awake()
        {
            Money = config.startMoney;
        }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            Money = Mathf.Round((Money + amount) * 100f) / 100f;
            MoneyChanged?.Invoke(Money);
        }

        /// <summary>Pays the amount if there is enough money. Returns false (and pays nothing) otherwise.</summary>
        public bool TrySpend(float amount)
        {
            if (Money + 0.0001f < amount) return false;
            Money = Mathf.Max(0f, Mathf.Round((Money - amount) * 100f) / 100f);
            MoneyChanged?.Invoke(Money);
            return true;
        }

        // small running costs (lights) arrive in fractions of a cent per frame, so they are collected here first
        float owed;

        /// <summary>Pays a small running cost. Returns false when the money has run out.</summary>
        public bool PayRunningCost(float amount)
        {
            if (Money <= 0f) return false;
            owed += amount;
            if (owed >= 0.01f)
            {
                float cents = Mathf.Floor(owed * 100f) / 100f;
                owed -= cents;
                Money = Mathf.Max(0f, Mathf.Round((Money - cents) * 100f) / 100f);
                MoneyChanged?.Invoke(Money);
            }
            return true;
        }

        public void SetMoney(float amount)
        {
            Money = Mathf.Max(0f, amount);
            MoneyChanged?.Invoke(Money);
        }
    }
}
