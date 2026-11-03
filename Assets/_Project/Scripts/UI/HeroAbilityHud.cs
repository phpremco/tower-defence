using Game.Economy;
using Game.Hero;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// What an ability button shows, shared by the quick bar and the hero panel so both read the same: its picture, its
    /// key, its label (name, cooldown or price) and whether it can be pressed now.
    /// </summary>
    public static class HeroAbilityHud
    {
        // Keyboard hints by slot; the gamepad uses the left and right trigger for the same two slots.
        private static readonly string[] Keys = { "F", "G" };

        public static Sprite IconFor(HeroAbilityDefinition ability)
        {
            if (ability.Icon != null)
            {
                return ability.Icon;
            }

            return IconFactory.Get(ability.Kind == HeroAbilityKind.GateRepair ? IconType.Repair : IconType.Shockwave);
        }

        public static string KeyFor(int slot) => slot >= 0 && slot < Keys.Length ? Keys[slot] : string.Empty;

        /// <summary>The ability's name and key when ready, its cooldown while cooling, its price when it costs money.</summary>
        public static string Label(HeroAbilityState state, int slot, bool withKey)
        {
            HeroAbilityDefinition ability = state.Definition;
            if (!state.Ready)
            {
                return $"{ability.DisplayName} {state.SecondsLeft}s";
            }

            string price = ability.MoneyCost > 0 ? $" {ability.MoneyCost}" : string.Empty;
            return withKey ? $"{ability.DisplayName}{price}  {KeyFor(slot)}" : $"{ability.DisplayName}{price}";
        }

        public static bool Affordable(HeroAbilityState state, Wallet wallet) => wallet.CanAfford(state.Definition.MoneyCost);
    }
}
