using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Builds the placeholder uGUI used by the menu screens, in code. Everything is operable with a gamepad.</summary>
    public static class UiKit
    {
        public const int BodySize = 36;
        public const int TitleSize = 72;
        public const float ButtonHeight = 80f;
        public const float ButtonWidth = 820f;

        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private static readonly Color Background = new Color(0.04f, 0.05f, 0.08f);
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color ButtonNormal = new Color(0.32f, 0.48f, 0.78f);
        private static readonly Color ButtonSelected = new Color(0.4f, 0.75f, 1f);
        private static readonly Color ButtonDisabled = new Color(0.28f, 0.3f, 0.36f);
        private static readonly Color Accent = new Color(0.3f, 0.75f, 1f, 0.9f);
        private const int StarTextureWidth = 1024;
        private const int StarTextureHeight = 576;
        private const float StarDensity = 0.004f;
        private const int MinTextSize = 16;
        private const float TextPadding = 18f;
        private const float SliderTrackHeight = 22f;
        private const float SliderTrackGap = 8f;
        private const float SliderHandleSize = 40f;
        private static readonly Vector2 OutlineDistance = new Vector2(2f, -2f);

        private static Texture2D _starTexture;

        private static Font _fallbackFont;

        /// <summary>Display text (titles, buttons, big numbers) keeps the sci-fi font; anything smaller than this reads better in a plain face.</summary>
        private const int DisplayFontMinSize = 28;
        private const int FitMinFontSize = 14;

        /// <summary>The sprites and fonts everything is drawn with. Set by the bootstrapper.</summary>
        public static UiSkin Skin { get; set; }

        /// <summary>A plain, open face for small text: hints, captions, descriptions and stats.</summary>
        public static Font BodyFont
        {
            get
            {
                if (_fallbackFont == null)
                {
                    _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _fallbackFont;
            }
        }

        public static Font DefaultFont
        {
            get
            {
                if (Skin != null && Skin.Font != null)
                {
                    return Skin.Font;
                }

                if (_fallbackFont == null)
                {
                    _fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _fallbackFont;
            }
        }

        public static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var go = new GameObject("UiCamera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            go.AddComponent<AudioListener>();
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        /// <summary>Creates a scaled canvas and returns a root inside the safe area.</summary>
        public static RectTransform CreateRoot(Transform parent, bool withStarfield = true)
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (withStarfield)
            {
                AddStarfield(canvasGo.transform);
            }

            var safe = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvasGo.transform, false);
            Rect area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;
            return safe;
        }

        public static Text CreateText(RectTransform parent, string text, int size, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = size >= DisplayFontMinSize ? DefaultFont : BodyFont;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = TextColor;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        public static Text CreateFixedText(RectTransform parent, string text, int size, TextAnchor alignment, Vector2 anchor, Vector2 offset, Vector2 dimensions)
        {
            Text label = CreateText(parent, text, size, alignment);
            if (size >= DisplayFontMinSize)
            {
                var glow = label.gameObject.AddComponent<Outline>();
                glow.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
                glow.effectDistance = OutlineDistance;
            }

            // A fixed box is a promise: the text wraps inside it and shrinks when it still would not fit, so no
            // line ever runs past a panel edge, whatever the copy says.
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = size;
            label.resizeTextMinSize = Mathf.Max(FitMinFontSize, size / 2);

            RectTransform rect = label.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = dimensions;
            return label;
        }

        /// <summary>A vertical stack centred on screen. Navigation between children follows layout order.</summary>
        public static RectTransform CreateColumn(RectTransform parent, float spacing)
        {
            var go = new GameObject("Column", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ButtonWidth, 0f);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        public static Button CreateButton(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            StyleSliced(image, Skin != null ? Skin.Button : null);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = ButtonHeight;
            element.preferredWidth = ButtonWidth;

            Text text = CreateText(go.GetComponent<RectTransform>(), label, BodySize, TextAnchor.MiddleCenter);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = MinTextSize;
            text.resizeTextMaxSize = BodySize;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(TextPadding, 4f);
            text.rectTransform.offsetMax = new Vector2(-TextPadding, -4f);
            return button;
        }

        /// <summary>A square button with a white icon, used on the field so it never covers the screen.</summary>
        public static Button CreateIconButton(RectTransform parent, Sprite icon, float size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("IconButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            StyleSliced(image, Skin != null ? Skin.ButtonSquare : null);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = size;
            element.preferredHeight = size;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.raycastTarget = false;
            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(12f, 12f);
            iconRect.offsetMax = new Vector2(-12f, -12f);
            return button;
        }

        public static void SetLabel(Button button, string label)
        {
            button.GetComponentInChildren<Text>().text = label;
        }

        public static void Select(Selectable selectable)
        {
            if (EventSystem.current != null && selectable != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }

        private static void AddStarfield(Transform canvas)
        {
            if (_starTexture == null)
            {
                _starTexture = new Texture2D(StarTextureWidth, StarTextureHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var pixels = new Color32[StarTextureWidth * StarTextureHeight];
                var random = new System.Random(7);
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (random.NextDouble() < StarDensity)
                    {
                        byte brightness = (byte)(90 + random.Next(166));
                        pixels[i] = new Color32(brightness, brightness, 255, brightness);
                    }
                }

                _starTexture.SetPixels32(pixels);
                _starTexture.Apply();
            }

            var go = new GameObject("Starfield", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var image = go.AddComponent<RawImage>();
            image.texture = _starTexture;
            image.raycastTarget = false;
            Stretch(image.rectTransform);
        }

        /// <summary>
        /// A labelled slider from 0 to 1 that the gamepad steers with left and right while it is selected: the label
        /// above a dark track with a coloured fill and a square handle that lights up like a button.
        /// </summary>
        public static Slider CreateSlider(RectTransform parent, string label, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = ButtonHeight;
            element.preferredWidth = ButtonWidth;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            Text text = CreateText(rect, label, BodySize, TextAnchor.UpperLeft);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(TextPadding, SliderTrackHeight + SliderTrackGap);
            text.rectTransform.offsetMax = new Vector2(-TextPadding, 0f);

            var track = new GameObject("Track", typeof(RectTransform)).AddComponent<Image>();
            track.transform.SetParent(rect, false);
            StyleSliced(track, Skin != null ? Skin.BarBack : null);
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.offsetMin = new Vector2(TextPadding, 0f);
            trackRect.offsetMax = new Vector2(-TextPadding, SliderTrackHeight);

            var fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            fill.transform.SetParent(trackRect, false);
            StyleSliced(fill, Skin != null ? Skin.BarBlue : null);
            fill.raycastTarget = false;
            Stretch(fill.rectTransform);

            // The handle slides inside a strip inset by half its width, so it never hangs over the track's ends.
            var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(trackRect, false);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(SliderHandleSize * 0.5f, 0f);
            handleArea.offsetMax = new Vector2(-SliderHandleSize * 0.5f, 0f);
            var handle = new GameObject("Handle", typeof(RectTransform)).AddComponent<Image>();
            handle.transform.SetParent(handleArea, false);
            StyleSliced(handle, Skin != null ? Skin.ButtonSquare : null);
            handle.rectTransform.sizeDelta = new Vector2(SliderHandleSize, SliderHandleSize);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.colors = ButtonColors();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        private static ColorBlock ButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonSelected;
            colors.selectedColor = ButtonSelected;
            colors.pressedColor = ButtonSelected;
            colors.disabledColor = ButtonDisabled;
            colors.colorMultiplier = 1f;
            return colors;
        }

        /// <summary>Gives an image a sliced skin sprite, or a plain colour when no skin is loaded.</summary>
        public static void StyleSliced(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
        }

        /// <summary>A tinted panel from the skin, placed with explicit anchors.</summary>
        public static Image CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            StyleSliced(image, Skin != null ? Skin.Panel : null);
            image.color = tint;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return image;
        }

        /// <summary>A horizontal stat bar: dark back plate and a coloured fill whose width follows fraction.</summary>
        public static RectTransform CreateBar(RectTransform parent, Sprite fillSprite, float fraction)
        {
            var back = new GameObject("Bar", typeof(RectTransform));
            back.transform.SetParent(parent, false);
            var backImage = back.AddComponent<Image>();
            StyleSliced(backImage, Skin != null ? Skin.BarBack : null);
            backImage.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(back.transform, false);
            var fillImage = fill.AddComponent<Image>();
            StyleSliced(fillImage, fillSprite);
            fillImage.raycastTarget = false;
            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            return back.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}





