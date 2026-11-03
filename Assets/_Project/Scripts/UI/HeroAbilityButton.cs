using Game.Hero;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>One ability button on a hero panel: its slot, the state it shows, the button and its label.</summary>
    public sealed class HeroAbilityButton
    {
        public HeroAbilityButton(int slot, HeroAbilityState state, Button button, Text label)
        {
            Slot = slot;
            State = state;
            Button = button;
            Label = label;
        }

        public int Slot { get; }
        public HeroAbilityState State { get; }
        public Button Button { get; }
        public Text Label { get; }
    }
}
