using System.Text;
using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The compact box in which ORA and Dr. Voss answer during a boss intro (Docs/BOSS_INTROS.md, round 13b): a small
    /// comic-framed panel at the right edge, where the visor cut-in was, with the speaker's portrait, name and line. It
    /// never covers the lower middle of the field, keeps its text at a size that reads on a 720p handheld, and the HUD
    /// keep-out sees it, so world labels stay off it. A dialogue presenter: the intro's <see cref="DialoguePlayer"/>
    /// types the lines into it. Look only.
    /// </summary>
    public sealed class BossAnswerBox : MonoBehaviour, IDialoguePresenter
    {
        private const string FrameSprite = "ComicFrame";
        private const float Width = 600f;
        private const float Height = 196f;
        private const float EdgeGap = 36f;
        private const float RaiseFromMiddle = 40f;
        private const float TiltDegrees = -2f;
        private const float Inset = 26f;
        private const float PortraitSize = 140f;
        private const float Gap = 18f;
        private const float NameHeight = 32f;
        // Canvas units on the 1080p reference: 26 is about 17 screen pixels at 720p, the smallest the HUD uses.
        private const int NameSize = 24;
        private const int BodySize = 26;
        private const float FrameSlice = 2f;
        private const float FadeSeconds = 0.18f;
        private const float SlideDistance = 60f;

        private static readonly Color Backing = new Color(0.03f, 0.05f, 0.09f, 0.94f);
        private static readonly Color BodyColour = new Color(0.9f, 0.95f, 1f);

        private readonly StringBuilder _full = new StringBuilder(160);
        private RectTransform _box;
        private CanvasGroup _group;
        private Image _portrait;
        private Text _name;
        private Text _body;
        private PortraitProvider _portraits;
        private SpeakerDefinition _speaker;
        private string _line = string.Empty;
        private float _fade;
        private bool _open;

        /// <summary>The panel, for the HUD keep-out.</summary>
        public RectTransform Box => _box;

        /// <summary>True while the box is up (fading included).</summary>
        public bool IsShown => _box != null && _box.gameObject.activeSelf;

        /// <summary>The words typed so far, for tests and probes.</summary>
        public string ShownText { get; private set; } = string.Empty;

        public void Initialize(RectTransform safe, PortraitProvider portraits)
        {
            _portraits = portraits;
            Image backing = UiKit.CreatePanel(safe, "BossAnswerBox", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, Backing);
            _box = backing.rectTransform;
            _box.pivot = new Vector2(1f, 0.5f);
            _box.sizeDelta = new Vector2(Width, Height);
            _box.localRotation = Quaternion.Euler(0f, 0f, TiltDegrees);
            _group = _box.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _portrait = new GameObject("Portrait", typeof(RectTransform)).AddComponent<Image>();
            _portrait.transform.SetParent(_box, false);
            _portrait.raycastTarget = false;
            _portrait.preserveAspect = true;
            RectTransform portrait = _portrait.rectTransform;
            portrait.anchorMin = portrait.anchorMax = new Vector2(0f, 0.5f);
            portrait.pivot = new Vector2(0f, 0.5f);
            portrait.anchoredPosition = new Vector2(Inset, 0f);
            portrait.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            float textLeft = Inset + PortraitSize + Gap;
            float textWidth = Width - textLeft - Inset;
            _name = UiKit.CreateFixedText(_box, string.Empty, NameSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(textLeft, -Inset), new Vector2(textWidth, NameHeight));
            _body = UiKit.CreateFixedText(_box, string.Empty, BodySize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(textLeft, -Inset - NameHeight), new Vector2(textWidth, Height - Inset * 2f - NameHeight));
            _body.color = BodyColour;
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Truncate;
            AddFrame();
            _box.gameObject.SetActive(false);
        }

        // The Blender comic frame over the panel's edge, as on the cut-in; without the art the plain panel stands alone.
        private void AddFrame()
        {
            UiArtSet art = UiArtSet.Active;
            Sprite sprite = art != null ? art.Sprite(FrameSprite) : null;
            if (sprite == null)
            {
                return;
            }

            var frame = new GameObject("ComicFrame", typeof(RectTransform)).AddComponent<Image>();
            frame.transform.SetParent(_box, false);
            frame.raycastTarget = false;
            frame.sprite = sprite;
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = FrameSlice;
            RectTransform rect = frame.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public void Open(DialogueDefinition dialogue)
        {
            if (!_box.gameObject.activeSelf)
            {
                _fade = 0f;
            }

            _box.gameObject.SetActive(true);
            _open = true;
        }

        public void ShowLine(DialogueLine line, int lineIndex)
        {
            _full.Clear();
            DialogueSegment[] segments = line.Segments ?? new DialogueSegment[0];
            foreach (DialogueSegment segment in segments)
            {
                _full.Append(segment.Text);
            }

            _line = _full.ToString();
            ShownText = string.Empty;
            _body.text = string.Empty;
            _speaker = line.Speaker;
            _name.text = _speaker != null ? _speaker.DisplayName.ToUpperInvariant() : string.Empty;
            _name.color = _speaker != null ? _speaker.RoleColour : BodyColour;
            _portrait.enabled = false;
            if (_speaker != null && _portraits != null)
            {
                SpeakerDefinition asked = _speaker;
                _portraits.Get(asked, sprite => ShowPortrait(asked, sprite));
            }
        }

        public void Reveal(int visibleCharacters, bool complete)
        {
            int count = complete ? _line.Length : Mathf.Clamp(visibleCharacters, 0, _line.Length);
            ShownText = _line.Substring(0, count);
            _body.text = ShownText;
        }

        public void Close() => _open = false;

        private void ShowPortrait(SpeakerDefinition asked, Sprite sprite)
        {
            if (asked != _speaker || _portrait == null)
            {
                return;
            }

            _portrait.sprite = sprite;
            _portrait.enabled = sprite != null;
        }

        private void LateUpdate()
        {
            if (!_box.gameObject.activeSelf)
            {
                return;
            }

            _fade = Mathf.MoveTowards(_fade, _open ? 1f : 0f, Time.unscaledDeltaTime / FadeSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, _fade);
            _group.alpha = eased;
            _box.anchoredPosition = new Vector2(-EdgeGap + (1f - eased) * SlideDistance, RaiseFromMiddle);
            if (!_open && _fade <= 0f)
            {
                _box.gameObject.SetActive(false);
            }
        }
    }
}
