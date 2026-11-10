using Game.Economy;
using Game.Hero;
using Game.Platform;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// What an ability button shows, shared by the quick bar and the hero panel so both read the same: its picture, its
    /// key, its label (name, cooldown or price) and whether it can be pressed now.
    /// </summary>
    public static class HeroAbilityHud
    {
        // The ability slots in order: the first is F or the left trigger, the second G or the right trigger.
        private static readonly GameAction[] SlotActions = { GameAction.HeroAbility1, GameAction.HeroAbility2 };

        public static Sprite IconFor(HeroAbilityDefinition ability)
        {
            if (ability.Icon != null)
            {
                return ability.Icon;
            }

            return IconFactory.Get(ability.Kind == HeroAbilityKind.GateRepair ? IconType.Repair : IconType.Shockwave);
        }

        /// <summary>The key or pad button of a slot on the device in use ("F" or "LT"); empty without input or slot.</summary>
        public static string KeyFor(int slot, IInputProvider input)
        {
            return input != null && slot >= 0 && slot < SlotActions.Length ? input.PromptFor(SlotActions[slot]) : string.Empty;
        }

        /// <summary>The ability's name and key when ready, its cooldown while cooling, its price when it costs money.</summary>
        public static string Label(HeroAbilityState state, string key)
        {
            HeroAbilityDefinition ability = state.Definition;
            if (!state.Ready)
            {
                return $"{ability.DisplayName} {state.SecondsLeft}s";
            }

            string price = ability.MoneyCost > 0 ? $" {ability.MoneyCost}" : string.Empty;
            return UiKit.Keyed(key, $"{ability.DisplayName}{price}");
        }

        public static bool Affordable(HeroAbilityState state, Wallet wallet) => wallet.CanAfford(state.Definition.MoneyCost);
    }
}
