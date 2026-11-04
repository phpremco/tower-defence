using System;

namespace Game.Economy
{
    /// <summary>Money inside one level. Optionally infinite for the test mode.</summary>
    public sealed class Wallet
    {
        private readonly bool _infinite;
        private readonly float _incomeMultiplier;

        // The part of a scaled bounty below one coin, kept for the next one: an ant pays one coin, and at 0.85 it must
        // still pay 0.85 on average instead of rounding back to one.
        private float _incomeRemainder;

        public Wallet(int startingMoney, bool infinite, float incomeMultiplier = 1f)
        {
            Money = startingMoney;
            _infinite = infinite;
            _incomeMultiplier = incomeMultiplier;
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

        /// <summary>Adds money as it is, such as a refund for a sold tower.</summary>
        public void Add(int amount)
        {
            Money += amount;
            Changed?.Invoke(Money);
        }

        /// <summary>Adds income earned in the level (a bounty, a load of ore), scaled by the difficulty's income share.</summary>
        public void Earn(int amount)
        {
            float scaled = amount * _incomeMultiplier + _incomeRemainder;
            int whole = (int)Math.Floor(scaled);
            _incomeRemainder = scaled - whole;
            if (whole != 0)
            {
                Add(whole);
            }
        }
    }
}
