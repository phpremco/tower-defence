using System;

namespace Game.Economy
{
    /// <summary>Money inside one level. Optionally infinite for the test mode.</summary>
    public sealed class Wallet
    {
        private readonly bool _infinite;

        public Wallet(int startingMoney, bool infinite)
        {
            Money = startingMoney;
            _infinite = infinite;
        }

        public int Money { get; private set; }

        public event Action<int> Changed;

        public bool CanAfford(int cost) => _infinite || Money >= cost;

        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            if (!_infinite)
            {
                Money -= cost;
                Changed?.Invoke(Money);
            }

            return true;
        }

        public void Add(int amount)
        {
            Money += amount;
            Changed?.Invoke(Money);
        }
    }
}
