using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Hit points with events. Plain class so enemies, the base and later towers can share it.</summary>
    public sealed class Health
    {
        public Health(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
        }

        public float Max { get; private set; }

        public float Current { get; private set; }

        public bool IsDead => Current <= 0f;

        public float Fraction => Current / Max;

        public event Action<float> Changed;

        public event Action Died;

        /// <summary>Raises the maximum and gives the extra hit points to the owner, so an upgrade also heals by its size.</summary>
        public void Extend(float extraMax)
        {
            if (extraMax <= 0f || IsDead)
            {
                return;
            }

            Max += extraMax;
            Current += extraMax;
            Changed?.Invoke(Current);
        }

        /// <summary>Restores health, never above the maximum.</summary>
        public void Heal(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            Current = Mathf.Min(Max, Current + amount);
            Changed?.Invoke(Current);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            Current = Mathf.Max(0f, Current - amount);
            Changed?.Invoke(Current);

            if (IsDead)
            {
                Died?.Invoke();
            }
        }
    }
}

