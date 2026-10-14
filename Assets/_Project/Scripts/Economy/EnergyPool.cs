using System;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Energy inside one level: it gathers over time up to a maximum and pays for drones. Plain logic with no
    /// scene objects, so it is easy to test and the HUD only listens to Changed.
    /// </summary>
    public sealed class EnergyPool
    {
        private readonly float _perSecond;
        private float _current;

        public EnergyPool(float maximum, float perSecond, float start = 0f)
        {
            Maximum = maximum;
            _perSecond = perSecond;
            _current = Mathf.Clamp(start, 0f, maximum);
        }

        public float Maximum { get; }

        public float Current => _current;

        public float PerSecond => _perSecond;

        public event Action Changed;

        public bool CanAfford(float cost) => _current >= cost;

        public void Tick(float deltaTime)
        {
            if (_current >= Maximum)
            {
                return;
            }

            _current = Mathf.Min(Maximum, _current + _perSecond * deltaTime);
            Changed?.Invoke();
        }

        public bool TrySpend(float cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            _current -= cost;
            Changed?.Invoke();
            return true;
        }
    }
}
