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
        private static readonly Color ButtonNormal = new Color(0.12f, 0.2f, 0.32f);
        private static readonly Color ButtonSelected = new Color(0.25f, 0.5f, 0.85f);
        private static readonly Color ButtonDisabled = new Color(0.1f, 0.1f, 0.12f);
        private static readonly Color Accent = new Color(0.3f, 0.75f, 1f, 0.9f);
        private const int StarTextureWidth = 1024;
        private const int StarTextureHeight = 576;
        private const float StarDensity = 0.004f;
        private const int MinTextSize = 16;
        private const float TextPadding = 18f;
        private static readonly Vector2 OutlineDistance = new Vector2(2f, -2f);

        private static Texture2D _starTexture;

        private static Font _font;

        private static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
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
            if (Object.FindFirstObjectByType<EventSystem>() != null)
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
            label.font = DefaultFont;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = TextColor;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static Text CreateFixedText(RectTransform parent, string text, int size, TextAnchor alignment, Vector2 anchor, Vector2 offset, Vector2 dimensions)
        {
            Text label = CreateText(parent, text, size, alignment);
            var glow = label.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            glow.effectDistance = OutlineDistance;
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
            image.color = Color.white;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Accent;
            outline.effectDistance = OutlineDistance;

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
            image.color = Color.white;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Accent;
            outline.effectDistance = OutlineDistance;

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

        public static void Select(Button button)
        {
            if (EventSystem.current != null && button != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}



