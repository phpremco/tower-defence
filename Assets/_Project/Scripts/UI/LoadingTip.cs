using UnityEngine;

namespace Game.UI
{
    /// <summary>One line of advice on the loading screen: who says it, in which colour, and the words.</summary>
    public readonly struct LoadingTip
    {
        public LoadingTip(string speaker, Color colour, string text)
        {
            Speaker = speaker;
            Colour = colour;
            Text = text;
        }

        public string Speaker { get; }

        public Color Colour { get; }

        public string Text { get; }
    }
}
