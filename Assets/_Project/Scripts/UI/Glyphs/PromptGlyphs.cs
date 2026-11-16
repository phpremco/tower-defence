using System.Collections.Generic;
using Game.Core;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The button glyph of an action on the device in use: it asks the input layer which control the action sits on
    /// (rebinding moves it) and which pad family is in the player's hands, and draws that control once per session.
    /// Screens put the sprite in an <see cref="Image"/>; <see cref="CreateImage"/> makes one that keeps its proportions.
    /// </summary>
    public static class PromptGlyphs
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private const float PixelsPerUnit = 100f;

        /// <summary>The glyph of an action on the active device and pad family; null without an input layer or binding.</summary>
        public static Sprite For(GameAction action)
        {
            if (!Services.TryGet(out IInputProvider input))
            {
                return null;
            }

            return For(action, input.ActiveDevice);
        }

        /// <summary>The glyph of an action on a device kind, in the pad family in use.</summary>
        public static Sprite For(GameAction action, InputDeviceKind device)
        {
            if (!Services.TryGet(out IInputBindings bindings))
            {
                return null;
            }

            return For(action, device, bindings.ActivePadLayout);
        }

        /// <summary>The glyph of an action in a given pad family (the controls page shows any family on request).</summary>
        public static Sprite For(GameAction action, InputDeviceKind device, PadLayout layout)
        {
            if (!Services.TryGet(out IInputBindings bindings) || !Services.TryGet(out IInputProvider input))
            {
                return null;
            }

            string path = bindings.ControlPath(action, device);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return ForControl(GlyphFamilies.For(device, layout), path, input.PromptFor(action, device));
        }

        /// <summary>The glyph of a control path in a family; <paramref name="label"/> names a key or an unknown pad control.</summary>
        public static Sprite ForControl(GlyphFamily family, string path, string label)
        {
            GlyphSpec spec = GlyphCatalog.Describe(family, path, label);
            string key = spec.Key;
            if (Sprites.TryGetValue(key, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            Texture2D texture = GlyphPainter.PaintTexture(spec);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            Sprites[key] = sprite;
            return sprite;
        }

        /// <summary>An image for a glyph, <paramref name="height"/> canvas units tall, as wide as the glyph needs.</summary>
        public static Image CreateImage(RectTransform parent, float height, string name = "Glyph")
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(height, height);
            return image;
        }

        /// <summary>Shows a glyph in an image and sizes it to the glyph's proportions; hides the image without one.</summary>
        public static void Show(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
            if (sprite == null)
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            float height = rect.sizeDelta.y;
            float width = height * sprite.rect.width / sprite.rect.height;
            rect.sizeDelta = new Vector2(width, height);
            var element = image.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.preferredWidth = width;
                element.preferredHeight = height;
            }
        }
    }
}
