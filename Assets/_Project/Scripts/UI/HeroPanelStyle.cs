using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The pieces the two hero panels share, so the quick bar on the left and the panel over the hero look like one
    /// family: tinted icon buttons as in the tower menu, and a health bar that goes from green to yellow to red.
    /// </summary>
    public static class HeroPanelStyle
    {
        private const float NormalShare = 0.55f;
        private static readonly Color DisabledColor = new Color(0.12f, 0.12f, 0.14f);
        private static readonly Color PlainGreen = new Color(0.4f, 0.9f, 0.45f);
        private static readonly Color PlainYellow = new Color(0.95f, 0.85f, 0.35f);
        private static readonly Color PlainRed = new Color(1f, 0.4f, 0.35f);

        public static void Tint(Button button, Color tint)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = tint * NormalShare;
            colors.highlightedColor = tint;
            colors.selectedColor = tint;
            colors.pressedColor = tint;
            colors.disabledColor = DisabledColor;
            button.colors = colors;
        }

        /// <summary>A health bar anchored to the top-left of its parent; returns the fill to update.</summary>
        public static Image CreateHealthBar(RectTransform parent, Vector2 offset, Vector2 size)
        {
            RectTransform bar = UiKit.CreateBar(parent, UiKit.Skin != null ? UiKit.Skin.BarGreen : null, 1f);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(0f, 1f);
            bar.pivot = new Vector2(0f, 1f);
            bar.anchoredPosition = offset;
            bar.sizeDelta = size;
            return bar.GetChild(0).GetComponent<Image>();
        }

        /// <summary>Sets the fill's width and colour band. Without a skin the fill is a plain tinted block.</summary>
        public static void FillHealth(Image fill, float fraction, float lowFraction, float midFraction)
        {
            fraction = Mathf.Clamp01(fraction);
            RectTransform rect = fill.rectTransform;
            rect.anchorMax = new Vector2(fraction, 1f);
            UiSkin skin = UiKit.Skin;
            if (skin != null)
            {
                fill.sprite = fraction <= lowFraction ? skin.BarRed : fraction <= midFraction ? skin.BarYellow : skin.BarGreen;
                fill.color = Color.white;
            }
            else
            {
                fill.color = fraction <= lowFraction ? PlainRed : fraction <= midFraction ? PlainYellow : PlainGreen;
            }
        }

        public static int Cooldown(float ready, float total) => Mathf.CeilToInt((1f - ready) * total);
    }
}
