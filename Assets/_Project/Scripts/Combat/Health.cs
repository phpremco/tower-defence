using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Hit points with events. Plain class so enemies, the base and towers can share it. An optional pool of armour sits
    /// in front of the hit points (Remco's design, round 24): while any armour is left, a hit works on the armour at the
    /// weapon's armour factor (a heavy shell at one or more strips it point for point, a light pulse at a fraction chips
    /// it slowly), and nothing reaches the body; the part of a hit the armour did not need carries on to the body in
    /// full. Once the armour is gone every weapon lands in full. The balance model uses the same rule
    /// (<c>Game.Balance.ArmourMix</c>).
    /// </summary>
    public sealed class Health
    {
        public Health(float max) : this(max, 0f)
        {
        }

        /// <param name="armor">Hit points of the armour pool; zero for creatures without one.</param>
        public Health(float max, float armor)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
            ArmorMax = Mathf.Max(0f, armor);
            Armor = ArmorMax;
        }

        public float Max { get; private set; }

        public float Current { get; private set; }

        public float ArmorMax { get; }

        public float Armor { get; private set; }

        public bool IsDead => Current <= 0f;

        public float Fraction => Current / Max;

        /// <summary>Share of the armour still on, from 0 to 1; zero for creatures that never had any.</summary>
        public float ArmorFraction => ArmorMax > 0f ? Armor / ArmorMax : 0f;

        public bool HasArmor => Armor > 0f;

        public event Action<float> Changed;

        /// <summary>Raised with the armour left whenever a hit chips it or something restores it.</summary>
        public event Action<float> ArmorChanged;

        /// <summary>Raised once when a hit takes the last of the armour, for views that show it breaking.</summary>
        public event Action ArmorBroken;

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

        /// <summary>Gives the owner a new maximum and fills it to that (a boss's shard that takes its share of the whole).</summary>
        public void Refill(float max)
        {
            if (max <= 0f || IsDead)
            {
                return;
            }

            Max = max;
            Current = max;
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

        /// <summary>
        /// The share of a hit the armour takes before the body is reached: all of it while the armour can absorb the whole
        /// hit at the weapon's factor, less once the hit is bigger than what is left. Zero without armour.
        /// </summary>
        public static float ArmourShareOfHit(float hit, float armor, float armourFactor)
        {
            if (armor <= 0f || hit <= 0f)
            {
                return 0f;
            }

            // A weapon that cannot work on armour at all is swallowed whole while any is left.
            return armourFactor <= 0f ? 1f : Mathf.Min(1f, armor / (hit * armourFactor));
        }

        /// <summary>A hit from a weapon that works on armour as well as it does on the body.</summary>
        public float TakeDamage(float amount) => TakeDamage(amount, 1f);

        /// <summary>
        /// One hit. Returns the points it took off the creature in all: armour chipped plus body hurt, which is what the
        /// damage of the hit was worth (a light pulse on armour is worth a fraction of itself).
        /// </summary>
        /// <param name="armourFactor">Share of the hit that works on armour; one for a heavy shell, under one for light fire.</param>
        public float TakeDamage(float amount, float armourFactor)
        {
            if (IsDead || amount <= 0f)
            {
                return 0f;
            }

            float dealt = 0f;
            if (Armor > 0f)
            {
                float share = ArmourShareOfHit(amount, Armor, armourFactor);
                float chipped = Mathf.Min(Armor, share * amount * armourFactor);
                Armor -= chipped;
                dealt += chipped;
                amount *= 1f - share;
                ArmorChanged?.Invoke(Armor);
                if (Armor <= 0f)
                {
                    Armor = 0f;
                    ArmorBroken?.Invoke();
                }
            }

            if (amount <= 0f)
            {
                return dealt;
            }

            float hurt = Mathf.Min(Current, amount);
            Current -= hurt;
            dealt += hurt;
            Changed?.Invoke(Current);

            if (IsDead)
            {
                Died?.Invoke();
            }

            return dealt;
        }
    }
}
