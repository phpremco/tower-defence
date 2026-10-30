using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Hit points with events. Plain class so enemies, the base and towers can share it. An optional layer of
    /// armour sits in front of the hit points: while any armour is left it soaks up a share of every hit, and the
    /// rest goes through to the body. Once the armour is chipped away every hit lands in full.
    /// </summary>
    public sealed class Health
    {
        public Health(float max) : this(max, 0f, 0f)
        {
        }

        /// <param name="armor">Hit points of the armour layer; zero for creatures without one.</param>
        /// <param name="armorReduction">Share of each hit the armour absorbs while it lasts, from 0 to 1.</param>
        public Health(float max, float armor, float armorReduction)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            ArmorMax = Mathf.Max(0f, armor);
            Armor = ArmorMax;
            ArmorReduction = Mathf.Clamp01(armorReduction);
        }

        public float Max { get; private set; }

        public float Current { get; private set; }

        public float ArmorMax { get; }

        public float Armor { get; private set; }

        public float ArmorReduction { get; }

        public bool IsDead => Current <= 0f;

        public float Fraction => Current / Max;

        /// <summary>Share of the armour still on, from 0 to 1; zero for creatures that never had any.</summary>
        public float ArmorFraction => ArmorMax > 0f ? Armor / ArmorMax : 0f;

        public bool HasArmor => Armor > 0f;

        public event Action<float> Changed;

        /// <summary>Raised with the armour left whenever a hit chips it or something restores it.</summary>
        public event Action<float> ArmorChanged;

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

        /// <summary>Grows armour back, never above what the creature started with.</summary>
        public void RestoreArmor(float amount)
        {
            if (amount <= 0f || ArmorMax <= 0f || IsDead)
            {
                return;
            }

            Armor = Mathf.Min(ArmorMax, Armor + amount);
            ArmorChanged?.Invoke(Armor);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            if (Armor > 0f)
            {
                float absorbed = Mathf.Min(Armor, amount * ArmorReduction);
                Armor -= absorbed;
                amount -= absorbed;
                ArmorChanged?.Invoke(Armor);
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
