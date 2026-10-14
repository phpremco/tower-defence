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
        public const float ButtonWidth = 640f;

        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private static readonly Color Background = new Color(0.04f, 0.05f, 0.08f);
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color ButtonNormal = new Color(0.12f, 0.2f, 0.32f);
        private static readonly Color ButtonSelected = new Color(0.25f, 0.5f, 0.85f);
        private static readonly Color ButtonDisabled = new Color(0.1f, 0.1f, 0.12f);

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
        public static RectTransform CreateRoot(Transform parent)
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
            Stretch(text.rectTransform);
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
