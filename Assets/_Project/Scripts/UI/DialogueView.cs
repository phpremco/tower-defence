using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Levels;
using Game.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The dialogue box: a dark glass panel low on the screen with a thin edge glowing in the speaker's colour, a
    /// rim-lit bust on the left, a name tag on top and typewriter text. Frames follow the speaker: a portrait for
    /// people, a flickering hologram for the AI, a header bar without portrait for transmissions, a recorder look
    /// for logs. Redacted words are black bars with crawling noise, struck words are crossed out, and a glitching
    /// line shakes and splits its colours. It only draws what the <see cref="DialoguePlayer"/> tells it; keyboard
    /// and gamepad belong to <see cref="DialogueInput"/>, the mouse to the box and its two buttons. Everything
    /// animates on unscaled time, because the game clock stands still while someone speaks.
    /// </summary>
    public sealed class DialogueView : MonoBehaviour, IDialoguePresenter
    {
        private const float Overhang = 18f;
        private const float BoxWidth = 1400f;
        private const float BoxHeight = 292f;
        private const float BoxBottom = 186f;
        private const float HeaderHeight = 50f;
        private const float Inset = 26f;
        private const float PortraitSize = 232f;
        private const float TextGap = 34f;
        private const float TextRight = 44f;
        private const float TextTop = 34f;
        private const float FooterHeight = 80f;
        private const float NameTagHeight = 48f;
        private const float NameTagPadding = 26f;
        private const float NameTagOverlap = 10f;
        private const float ButtonWidth = 230f;
        private const float ButtonHeight = 54f;
        private const float ButtonGap = 12f;
        private const float ButtonBottom = 18f;
        private const float ArrowSize = 22f;
        private const float HeaderIconSize = 38f;
        private const int BodySize = 34;
        private const int BodyMinSize = 25;
        private const int NameSize = 26;
        private const int HeaderSize = 26;
        private const int ButtonTextSize = 26;
        private const int ButtonTextMinSize = 22;
        private const int NextSlot = 0;
        private const int SkipSlot = 1;
        private const int CodexSlot = 2;
        private const string KeyColour = "#FFD45C";
        private const float NameSpacing = 8f;
        private const float HeaderSpacing = 3f;
        private const float SmallSpacing = 4f;
        private const float SmallSliceMultiplier = 2.2f;
        private const float MachineTint = 0.3f;
        private const string MachineSpacing = "<cspace=0.05em>";

        private const float FadeInSeconds = 0.24f;
        private const float FadeOutSeconds = 0.18f;
        private const float SlideDistance = 44f;
        private const float ColourSpeed = 9f;
        private const float PortraitPopSeconds = 0.2f;
        private const float PortraitPopScale = 0.92f;
        private const float BreathSpeed = 1.7f;
        private const float BreathScale = 0.012f;
        private const float SwaySpeed = 0.9f;
        private const float SwayPixels = 2.5f;
        private const float SwayDegrees = 0.5f;
        private const float ArrowBlinkSpeed = 6f;
        private const float ArrowBob = 3f;
        private const float ScanlinePixels = 3f;
        private const float ScanlineSpeed = 0.35f;
        private const float FlickerRate = 15f;
        private const float NoiseRate = 12f;
        private const float GlitchRate = 14f;
        private const float GlitchBoxPixels = 7f;
        private const float GlitchCharPixels = 5f;
        private const float GlitchSplitPixels = 6f;
        private const float RecBlinkSpeed = 3f;

        private static readonly Color PanelTint = new Color(0.02f, 0.032f, 0.06f, 0.94f);
        private static readonly Color BodyColour = new Color(0.9f, 0.95f, 1f);
        private static readonly Color DimColour = new Color(0.005f, 0.01f, 0.03f, 0.78f);
        private static readonly Color RedactedBar = new Color(1f, 1f, 1f, 0.97f);
        private static readonly Color SplitWarm = new Color(1f, 0.25f, 0.45f, 0.55f);
        private static readonly Color32 SplitCool = new Color32(90, 230, 255, 255);
        private static readonly Color32 SplitHot = new Color32(255, 80, 140, 255);

        private readonly StringBuilder _builder = new StringBuilder(256);
        private readonly List<Vector2Int> _redacted = new List<Vector2Int>();
        private readonly List<RawImage> _bars = new List<RawImage>();

        private DialoguePlayer _player;
        private PortraitProvider _portraits;

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _box;

        /// <summary>The speech box, for the HUD keep-out (inactive while no one speaks).</summary>
        public RectTransform Box => _box;
        private Image _edge;
        private RectTransform _portraitFrame;
        private RawImage _portraitGlow;
        private Image _portraitBorder;
        private Image _portrait;
        private RawImage _scanlines;
        private RectTransform _nameTag;
        private Image _nameFill;
        private Image _nameEdge;
        private TextMeshProUGUI _name;
        private RectTransform _header;
        private Image _headerFill;
        private Image _headerIcon;
        private TextMeshProUGUI _headerLeft;
        private TextMeshProUGUI _headerRight;
        private TextMeshProUGUI _body;
        private TextMeshProUGUI _ghost;
        private RectTransform _barLayer;
        private Image _arrow;
        private Image[] _buttonEdges;
        private TextMeshProUGUI[] _buttonLabels;
        private Button[] _buttons;
        private IInputProvider _input;
        private int _visibleButtons;

        // What each footer button says and which action it names, so its prompt follows the device in use.
        private static readonly string[] ButtonWords = { "NEXT", "SKIP", "CODEX" };
        private static readonly GameAction[] ButtonActions = { GameAction.Confirm, GameAction.Cancel, GameAction.Codex };

        private SpeakerDefinition _speaker;
        private SpeakerFrame _frame;
        private Color _shownRole = Color.white;
        private Color _targetRole = Color.white;
        private float _glitch;
        private int _seed;
        private float _fade;
        private bool _open;
        private float _clock;
        private float _portraitAge;
        private bool _complete;
        private bool _meshDirty;
        private int _lastGlitchTick = -1;
        private int _lastNoiseTick = -1;
        private float _bottom = BoxBottom - Overhang;
        private GameObject _restoreSelection;
        private bool _selectionTaken;

        /// <summary>True from <see cref="Open"/> until the box has faded out after <see cref="Close"/>.</summary>
        public bool IsShown => _root != null && _root.activeSelf;

        /// <summary>The current line as plain text, for tests and probes.</summary>
        public string ShownText { get; private set; } = string.Empty;

        /// <summary>
        /// Moves the box from its HUD height: <paramref name="bottom"/> is the height of its lower edge (glow
        /// included) in canvas units and <paramref name="scale"/> shrinks it from that edge. Cutscenes put it low,
        /// just above the letterbox bar.
        /// </summary>
        public void SetPlacement(float bottom, float scale)
        {
            _bottom = bottom;
            if (_box != null)
            {
                _box.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void Initialize(RectTransform safeRoot, DialoguePlayer player, PortraitProvider portraits)
        {
            _player = player;
            _portraits = portraits;
            Build(safeRoot);
            _root.SetActive(false);
            if (Services.TryGet(out _input))
            {
                _input.ActiveDeviceChanged += RefreshButtons;
            }

        }

        /// <summary>Renders the busts of everyone who speaks in these triggers now, so the first line never waits for a picture.</summary>
        public void Prewarm(StoryTrigger[] triggers)
        {
            var speakers = new HashSet<SpeakerDefinition>();
            foreach (StoryTrigger trigger in triggers)
            {
                if (trigger.Dialogue == null)
                {
                    continue;
                }

                foreach (DialogueLine line in trigger.Dialogue.Lines)
                {
                    if (line.Speaker != null && speakers.Add(line.Speaker))
                    {
                        _portraits.Get(line.Speaker, IgnorePicture);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.ActiveDeviceChanged -= RefreshButtons;
            }
        }

        /// <summary>Renders the busts of everyone who speaks in these conversations now (the creature scans, for one).</summary>
        public void Prewarm(IReadOnlyList<ScanHint> hints)
        {
            if (hints == null)
            {
                return;
            }

            var speakers = new HashSet<SpeakerDefinition>();
            foreach (ScanHint hint in hints)
            {
                if (hint.Dialogue == null)
                {
                    continue;
                }

                foreach (DialogueLine line in hint.Dialogue.Lines)
                {
                    if (line.Speaker != null && speakers.Add(line.Speaker))
                    {
                        _portraits.Get(line.Speaker, IgnorePicture);
                    }
                }
            }
        }

        public void Open(DialogueDefinition dialogue)
        {
            if (!_root.activeSelf)
            {
                _fade = 0f;
            }

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _open = true;
            TakeSelection();
            RefreshButtons();
        }

        public void ShowLine(DialogueLine line, int lineIndex)
        {
            if (line.Speaker != _speaker || _speaker == null)
            {
                ApplySpeaker(line.Speaker);
            }

            _glitch = Mathf.Clamp01(line.GlitchIntensity);
            _seed = StableHash(line.Id);
            _ghost.gameObject.SetActive(_glitch > 0f);
            SetText(line);
            _complete = false;
            _lastGlitchTick = -1;
        }

        public void Reveal(int visibleCharacters, bool complete)
        {
            _body.maxVisibleCharacters = visibleCharacters;
            _ghost.maxVisibleCharacters = visibleCharacters;
            _complete = complete;
            _meshDirty = true;
            LayoutBars(visibleCharacters);
        }

        public void Close()
        {
            _open = false;
        }

        /// <summary>Closes the box at once, without the fade (a cutscene seek wants the frame of that moment).</summary>
        public void HideNow()
        {
            _open = false;
            if (_root == null || !_root.activeSelf)
            {
                return;
            }

            _fade = 0f;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _root.SetActive(false);
            _speaker = null;
            ShownText = string.Empty;
            GiveSelectionBack();
        }

        private static void IgnorePicture(Sprite _)
        {
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            _clock += delta;
            _portraitAge += delta;
            AnimateFade(delta);
            if (!_root.activeSelf)
            {
                return;
            }

            AnimateColour(delta);
            AnimatePortrait();
            AnimateArrow();
            AnimateHeader();
            AnimateNoise();
        }

        // After the player's tick, so a reveal this frame is laid out before the glitch moves its characters.
        private void LateUpdate()
        {
            if (_root == null || !_root.activeSelf)
            {
                return;
            }

            AnimateGlitch();
        }

        private void AnimateFade(float delta)
        {
            float step = delta / (_open ? FadeInSeconds : FadeOutSeconds);
            _fade = Mathf.Clamp01(_fade + (_open ? step : -step));
            float eased = 1f - (1f - _fade) * (1f - _fade);
            _group.alpha = eased;
            _group.blocksRaycasts = _open;
            _box.anchoredPosition = new Vector2(0f, _bottom - (1f - eased) * SlideDistance) + GlitchShake();
            if (!_open && _fade <= 0f)
            {
                _root.SetActive(false);
                _speaker = null;
                ShownText = string.Empty;
                GiveSelectionBack();
            }
        }

        // Gamepad A advances the box, and the UI module would also submit whatever button has focus (a tower or drone
        // card): the focus is parked while the box is up and handed back once it has faded, a frame after the press.
        private void TakeSelection()
        {
            EventSystem events = EventSystem.current;
            if (_selectionTaken || events == null)
            {
                return;
            }

            _selectionTaken = true;
            _restoreSelection = events.currentSelectedGameObject;
            events.SetSelectedGameObject(null);
        }

        private void GiveSelectionBack()
        {
            if (!_selectionTaken)
            {
                return;
            }

            _selectionTaken = false;
            GameObject target = _restoreSelection;
            _restoreSelection = null;
            EventSystem events = EventSystem.current;
            if (events == null || target == null || !target.activeInHierarchy || events.currentSelectedGameObject != null)
            {
                return;
            }

            Selectable selectable = target.GetComponent<Selectable>();
            if (selectable == null || selectable.IsInteractable())
            {
                events.SetSelectedGameObject(target);
            }
        }

        private void AnimateColour(float delta)
        {
            _shownRole = Color.Lerp(_shownRole, _targetRole, 1f - Mathf.Exp(-ColourSpeed * delta));
            Color role = _shownRole;
            _edge.color = new Color(role.r, role.g, role.b, 0.95f);
            _portraitBorder.color = new Color(role.r, role.g, role.b, 0.9f);
            _portraitGlow.color = new Color(role.r, role.g, role.b, _frame == SpeakerFrame.Hologram ? 0.55f : 0.3f);
            _nameFill.color = new Color(role.r * 0.22f, role.g * 0.22f, role.b * 0.22f, 0.95f);
            _nameEdge.color = new Color(role.r, role.g, role.b, 0.9f);
            _name.color = Color.Lerp(role, Color.white, 0.25f);
            _headerFill.color = new Color(role.r * 0.3f, role.g * 0.3f, role.b * 0.3f, 0.9f);
            _headerLeft.color = Color.Lerp(role, Color.white, 0.3f);
            _headerRight.color = new Color(role.r, role.g, role.b, 0.85f);
            _arrow.color = new Color(role.r, role.g, role.b, _arrow.color.a);
            for (int i = 0; i < _buttonEdges.Length; i++)
            {
                _buttonEdges[i].color = new Color(role.r, role.g, role.b, 0.75f);
                _buttonLabels[i].color = Color.Lerp(role, Color.white, 0.55f);
            }
        }

        // A slow breath and sway keep the bust alive; a new speaker pops in; the hologram flickers on its scanlines.
        private void AnimatePortrait()
        {
            float pop = Mathf.Clamp01(_portraitAge / PortraitPopSeconds);
            float popEased = 1f - (1f - pop) * (1f - pop);
            float breath = 1f + Mathf.Sin(_clock * BreathSpeed) * BreathScale;
            float scale = Mathf.Lerp(PortraitPopScale, 1f, popEased) * breath;
            RectTransform rect = _portrait.rectTransform;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = new Vector2(Mathf.Sin(_clock * SwaySpeed * 0.7f) * SwayPixels, Mathf.Sin(_clock * SwaySpeed) * SwayPixels);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_clock * SwaySpeed * 0.8f) * SwayDegrees);

            float alpha = popEased;
            bool projected = _frame == SpeakerFrame.Hologram || _frame == SpeakerFrame.Log;
            if (projected)
            {
                int tick = Mathf.FloorToInt(_clock * FlickerRate);
                float flicker = Unit(tick, 17);
                alpha *= flicker < 0.08f ? 0.55f : 0.86f + flicker * 0.14f;
                Rect uv = _scanlines.uvRect;
                uv.y = -_clock * ScanlineSpeed;
                _scanlines.uvRect = uv;
            }

            if (_glitch > 0f && DialogueGlitch.IsScrambled(_glitch, _seed, -7, Mathf.FloorToInt(_clock * GlitchRate)))
            {
                alpha *= 0.4f;
            }

            Color tint = _frame == SpeakerFrame.Hologram ? Color.Lerp(_shownRole, Color.white, 0.55f) : Color.white;
            _portrait.color = new Color(tint.r, tint.g, tint.b, _portrait.sprite != null ? alpha : 0f);
        }

        private void AnimateArrow()
        {
            if (!_complete)
            {
                _arrow.color = new Color(_arrow.color.r, _arrow.color.g, _arrow.color.b, 0f);
                return;
            }

            float wave = 0.5f + 0.5f * Mathf.Sin(_clock * ArrowBlinkSpeed);
            _arrow.color = new Color(_arrow.color.r, _arrow.color.g, _arrow.color.b, 0.35f + 0.65f * wave);
            _arrow.rectTransform.anchoredPosition = new Vector2(_arrow.rectTransform.anchoredPosition.x, FooterCentre() - wave * ArrowBob);
        }

        // The log's recording dot blinks; a transmission's header text pulses faintly.
        private void AnimateHeader()
        {
            if (!_header.gameObject.activeSelf)
            {
                return;
            }

            float wave = 0.5f + 0.5f * Mathf.Sin(_clock * RecBlinkSpeed);
            Color right = _headerRight.color;
            _headerRight.color = new Color(right.r, right.g, right.b, _frame == SpeakerFrame.Log ? (wave > 0.5f ? 1f : 0.25f) : 0.6f + 0.4f * wave);
        }

        private void AnimateNoise()
        {
            if (_redacted.Count == 0)
            {
                return;
            }

            int tick = Mathf.FloorToInt(_clock * NoiseRate);
            if (tick == _lastNoiseTick)
            {
                return;
            }

            _lastNoiseTick = tick;
            for (int i = 0; i < _bars.Count; i++)
            {
                Rect uv = _bars[i].uvRect;
                uv.x = Unit(tick, i);
                uv.y = Unit(tick, i + 31);
                _bars[i].uvRect = uv;
            }
        }

        private Vector2 GlitchShake()
        {
            if (_glitch <= 0f)
            {
                return Vector2.zero;
            }

            int tick = Mathf.FloorToInt(_clock * GlitchRate);
            return DialogueGlitch.IsScrambled(_glitch, _seed, -3, tick) ? DialogueGlitch.Jitter(_glitch, _seed, tick) * GlitchBoxPixels : Vector2.zero;
        }

        // Scrambled characters jump and take a split colour; the ghost copy behind the text slides out of register.
        private void AnimateGlitch()
        {
            if (_glitch <= 0f)
            {
                return;
            }

            int tick = Mathf.FloorToInt(_clock * GlitchRate);
            if (tick == _lastGlitchTick && !_meshDirty)
            {
                return;
            }

            _lastGlitchTick = tick;
            _meshDirty = false;
            Vector2 split = DialogueGlitch.Jitter(_glitch, _seed + 1, tick) * GlitchSplitPixels;
            _ghost.rectTransform.anchoredPosition = _body.rectTransform.anchoredPosition + new Vector2(split.x + GlitchSplitPixels * _glitch * 0.5f, split.y * 0.3f);

            _body.ForceMeshUpdate();
            TMP_TextInfo info = _body.textInfo;
            int visible = Mathf.Min(_body.maxVisibleCharacters, info.characterCount);
            for (int i = 0; i < visible; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible || !DialogueGlitch.IsScrambled(_glitch, _seed, i, tick))
                {
                    continue;
                }

                Vector2 jump = DialogueGlitch.Jitter(1f, _seed ^ i, tick) * GlitchCharPixels;
                Color32 tint = (i + tick) % 2 == 0 ? SplitCool : SplitHot;
                TMP_MeshInfo mesh = info.meshInfo[character.materialReferenceIndex];
                for (int v = 0; v < 4; v++)
                {
                    int index = character.vertexIndex + v;
                    mesh.vertices[index] += new Vector3(jump.x, jump.y, 0f);
                    // Keep the alpha, so a redacted letter stays hidden under its bar.
                    mesh.colors32[index] = new Color32(tint.r, tint.g, tint.b, mesh.colors32[index].a);
                }
            }

            _body.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private void ApplySpeaker(SpeakerDefinition speaker)
        {
            _speaker = speaker;
            _portraitAge = 0f;
            _frame = speaker != null ? speaker.Frame : SpeakerFrame.Portrait;
            _targetRole = speaker != null ? speaker.RoleColour : Color.white;
            if (!IsFullyShown)
            {
                _shownRole = _targetRole;
            }

            bool hasPortrait = _frame != SpeakerFrame.Transmission;
            bool hasHeader = _frame == SpeakerFrame.Transmission || _frame == SpeakerFrame.Log;
            bool projected = _frame == SpeakerFrame.Hologram || _frame == SpeakerFrame.Log;
            float height = BoxHeight + (hasHeader ? HeaderHeight : 0f);
            _box.sizeDelta = new Vector2(BoxWidth + Overhang * 2f, height + Overhang * 2f);

            _portraitFrame.gameObject.SetActive(hasPortrait);
            _scanlines.gameObject.SetActive(projected);
            _header.gameObject.SetActive(hasHeader);
            _nameTag.gameObject.SetActive(!hasHeader);

            string displayName = speaker != null ? speaker.DisplayName.ToUpperInvariant() : string.Empty;
            _name.text = displayName;
            _nameTag.sizeDelta = new Vector2(_name.GetPreferredValues(displayName).x + NameTagPadding * 2f + Overhang * 2f, NameTagHeight + Overhang * 2f);
            if (_frame == SpeakerFrame.Transmission)
            {
                _headerLeft.text = "INCOMING TRANSMISSION   //   " + displayName;
                _headerRight.text = "ENCRYPTED CHANNEL";
            }
            else
            {
                _headerLeft.text = displayName + "   //   LOG ENTRY";
                _headerRight.text = "● PLAYBACK";
            }

            float top = TextTop + (hasHeader ? HeaderHeight : 0f);
            float left = hasPortrait ? Inset + PortraitSize + TextGap : Inset + TextGap;
            PlaceBody(_body.rectTransform, left, top);
            PlaceBody(_ghost.rectTransform, left, top);
            _portraitFrame.anchoredPosition = new Vector2(Overhang + Inset, -(Overhang + (hasHeader ? HeaderHeight : 0f) + (BoxHeight - PortraitSize) * 0.5f));

            _portrait.sprite = null;
            _headerIcon.sprite = null;
            _headerIcon.enabled = false;
            if (speaker != null)
            {
                SpeakerDefinition asked = speaker;
                _portraits.Get(speaker, sprite => ShowPicture(asked, sprite));
            }
        }

        private bool IsFullyShown => _root.activeSelf && _fade >= 1f;

        private void ShowPicture(SpeakerDefinition speaker, Sprite sprite)
        {
            if (speaker != _speaker)
            {
                return;
            }

            _portrait.sprite = sprite;
            _headerIcon.sprite = sprite;
            _headerIcon.enabled = _frame == SpeakerFrame.Transmission && sprite != null;
        }

        private static void PlaceBody(RectTransform rect, float left, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(Overhang + left, Overhang + FooterHeight);
            rect.offsetMax = new Vector2(-(Overhang + TextRight), -(Overhang + top));
        }

        // The whole line is laid out once; the typewriter only moves maxVisibleCharacters, so nothing ever jumps.
        private void SetText(DialogueLine line)
        {
            _builder.Clear();
            _redacted.Clear();
            // Machines speak in a slightly wider, role-tinted type, so a message reads as received rather than spoken.
            bool machine = _frame == SpeakerFrame.Transmission || _frame == SpeakerFrame.Log;
            _body.color = machine ? Color.Lerp(BodyColour, _targetRole, MachineTint) : BodyColour;
            if (machine)
            {
                _builder.Append(MachineSpacing);
            }

            var plain = new StringBuilder();
            int index = 0;
            DialogueSegment[] segments = line.Segments ?? new DialogueSegment[0];
            foreach (DialogueSegment segment in segments)
            {
                string text = segment.Text ?? string.Empty;
                switch (segment.Style)
                {
                    case SegmentStyle.Struck:
                        _builder.Append("<color=#93A3BA><s>");
                        AppendLiteral(text);
                        _builder.Append("</s></color>");
                        break;
                    case SegmentStyle.Redacted:
                        // The letters keep their space but stay invisible; the noise bars cover the gap.
                        _builder.Append("<alpha=#00>");
                        AppendLiteral(text);
                        _builder.Append("<alpha=#FF>");
                        _redacted.Add(new Vector2Int(index, index + text.Length));
                        break;
                    default:
                        AppendLiteral(text);
                        break;
                }

                plain.Append(text);
                index += text.Length;
            }

            ShownText = plain.ToString();
            string rich = _builder.ToString();
            _body.text = rich;
            _body.maxVisibleCharacters = 0;
            _ghost.text = rich;
            _ghost.maxVisibleCharacters = 0;
            _body.ForceMeshUpdate();
            _ghost.fontSize = _body.fontSize;
            _meshDirty = true;
            LayoutBars(0);
        }

        private void AppendLiteral(string text)
        {
            bool needsGuard = text.IndexOf('<') >= 0;
            if (needsGuard)
            {
                _builder.Append("<noparse>");
            }

            _builder.Append(text);
            if (needsGuard)
            {
                _builder.Append("</noparse>");
            }
        }

        // One bar per redacted run per text line, as wide as the characters revealed so far.
        private void LayoutBars(int visible)
        {
            int used = 0;
            if (_redacted.Count > 0)
            {
                TMP_TextInfo info = _body.textInfo;
                foreach (Vector2Int range in _redacted)
                {
                    int end = Mathf.Min(range.y, Mathf.Min(visible, info.characterCount));
                    int start = range.x;
                    while (start < end)
                    {
                        int line = info.characterInfo[start].lineNumber;
                        int stop = start;
                        while (stop + 1 < end && info.characterInfo[stop + 1].lineNumber == line)
                        {
                            stop++;
                        }

                        PlaceBar(used++, info, start, stop, line);
                        start = stop + 1;
                    }
                }
            }

            for (int i = used; i < _bars.Count; i++)
            {
                _bars[i].gameObject.SetActive(false);
            }
        }

        private void PlaceBar(int slot, TMP_TextInfo info, int first, int last, int line)
        {
            while (_bars.Count <= slot)
            {
                _bars.Add(CreateBar());
            }

            TMP_CharacterInfo a = info.characterInfo[first];
            TMP_CharacterInfo b = info.characterInfo[last];
            TMP_LineInfo lineInfo = info.lineInfo[line];
            float xMin = a.origin;
            float xMax = Mathf.Max(b.topRight.x, b.xAdvance);
            float yMin = lineInfo.descender;
            float yMax = lineInfo.ascender;

            RawImage bar = _bars[slot];
            bar.gameObject.SetActive(true);
            RectTransform rect = bar.rectTransform;
            rect.anchoredPosition = new Vector2(xMin, yMin);
            rect.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), yMax - yMin);
            Rect uv = bar.uvRect;
            uv.width = rect.sizeDelta.x / (DialogueArt.Noise.width * 1.5f);
            uv.height = 1f;
            bar.uvRect = uv;
        }

        private RawImage CreateBar()
        {
            var go = new GameObject("Redacted", typeof(RectTransform));
            go.transform.SetParent(_barLayer, false);
            var bar = go.AddComponent<RawImage>();
            bar.texture = DialogueArt.Noise;
            bar.color = RedactedBar;
            bar.raycastTarget = false;
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = rect.anchorMax = _body.rectTransform.pivot;
            rect.pivot = Vector2.zero;
            return bar;
        }

        private float FooterCentre() => Overhang + ButtonBottom + ButtonHeight * 0.5f;

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                if (text != null)
                {
                    foreach (char c in text)
                    {
                        hash = (hash ^ c) * 16777619;
                    }
                }

                return hash;
            }
        }

        private static float Unit(int tick, int salt)
        {
            unchecked
            {
                uint h = (uint)(tick * 73856093) ^ (uint)(salt * 19349663);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0xFFFF) / 65535f;
            }
        }

        // ---- Building -------------------------------------------------------------------------------------------

        private void Build(RectTransform safeRoot)
        {
            _root = new GameObject("Dialogue", typeof(RectTransform));
            var rootRect = (RectTransform)_root.transform;
            rootRect.SetParent(safeRoot, false);
            Stretch(rootRect);
            _group = _root.AddComponent<CanvasGroup>();

            // A shade that darkens the field from the bottom up and takes the clicks meant for the field.
            var dim = NewChild<RawImage>(rootRect, "DialogueDim");
            dim.texture = DialogueArt.Shade;
            dim.color = DimColour;
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);
            CoverScreen.Add(dim.rectTransform);

            BuildBox(rootRect);
            BuildNameTag();
            BuildHeader();
            BuildPortrait();
            BuildText();
            BuildFooter();
        }

        private void BuildBox(RectTransform root)
        {
            var fill = NewChild<Image>(root, "DialogueBox");
            fill.sprite = DialogueArt.Panel;
            fill.type = Image.Type.Sliced;
            fill.color = PanelTint;
            fill.raycastTarget = true;
            _box = fill.rectTransform;
            _box.anchorMin = _box.anchorMax = new Vector2(0.5f, 0f);
            _box.pivot = new Vector2(0.5f, 0f);
            _box.sizeDelta = new Vector2(BoxWidth + Overhang * 2f, BoxHeight + Overhang * 2f);

            // The whole box is a button that advances, but it never takes the keyboard focus: confirm goes through
            // DialogueInput, so one press is one step.
            var button = fill.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(Advance);

            var sheen = NewChild<RawImage>(_box, "Sheen");
            sheen.texture = DialogueArt.Shade;
            sheen.uvRect = new Rect(0f, 1f, 1f, -1f);
            sheen.color = new Color(0.45f, 0.65f, 1f, 0.07f);
            sheen.raycastTarget = false;
            Stretch(sheen.rectTransform, Overhang);

            _edge = NewChild<Image>(_box, "Edge");
            _edge.sprite = DialogueArt.Edge;
            _edge.type = Image.Type.Sliced;
            _edge.raycastTarget = false;
            Stretch(_edge.rectTransform);
        }

        private void BuildNameTag()
        {
            _nameFill = NewChild<Image>(_box, "NameTag");
            _nameFill.sprite = DialogueArt.Panel;
            _nameFill.type = Image.Type.Sliced;
            _nameFill.raycastTarget = false;
            _nameTag = _nameFill.rectTransform;
            _nameTag.anchorMin = _nameTag.anchorMax = new Vector2(0f, 1f);
            _nameTag.pivot = new Vector2(0f, 0f);
            // Overlaps the box's top edge a little, so the tag reads as part of the box.
            _nameTag.anchoredPosition = new Vector2(Inset, -Overhang * 2f - NameTagOverlap);

            _nameEdge = NewChild<Image>(_nameTag, "Edge");
            _nameEdge.sprite = DialogueArt.Edge;
            _nameEdge.type = Image.Type.Sliced;
            _nameEdge.raycastTarget = false;
            Stretch(_nameEdge.rectTransform);

            _name = NewText(_nameTag, "Name", NameSize, TextAlignmentOptions.Center);
            _name.characterSpacing = NameSpacing;
            Stretch(_name.rectTransform, Overhang);
        }

        private void BuildHeader()
        {
            _headerFill = NewChild<Image>(_box, "Header");
            _headerFill.sprite = DialogueArt.Panel;
            _headerFill.type = Image.Type.Sliced;
            _headerFill.pixelsPerUnitMultiplier = SmallSliceMultiplier;
            _headerFill.raycastTarget = false;
            _header = _headerFill.rectTransform;
            _header.anchorMin = new Vector2(0f, 1f);
            _header.anchorMax = new Vector2(1f, 1f);
            _header.pivot = new Vector2(0.5f, 1f);
            _header.offsetMin = new Vector2(Overhang + 8f, -(Overhang + 8f + HeaderHeight));
            _header.offsetMax = new Vector2(-(Overhang + 8f), -(Overhang + 8f));

            _headerIcon = NewChild<Image>(_header, "Emblem");
            _headerIcon.preserveAspect = true;
            _headerIcon.raycastTarget = false;
            RectTransform icon = _headerIcon.rectTransform;
            icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(HeaderIconSize, HeaderIconSize);
            icon.anchoredPosition = new Vector2(14f, 0f);

            _headerLeft = NewText(_header, "HeaderLeft", HeaderSize, TextAlignmentOptions.MidlineLeft);
            _headerLeft.characterSpacing = HeaderSpacing;
            Stretch(_headerLeft.rectTransform);
            _headerLeft.rectTransform.offsetMin = new Vector2(14f + HeaderIconSize + 14f, 0f);
            _headerRight = NewText(_header, "HeaderRight", HeaderSize, TextAlignmentOptions.MidlineRight);
            _headerRight.characterSpacing = SmallSpacing;
            Stretch(_headerRight.rectTransform);
            _headerRight.rectTransform.offsetMax = new Vector2(-18f, 0f);
        }

        private void BuildPortrait()
        {
            var frame = new GameObject("PortraitFrame", typeof(RectTransform));
            _portraitFrame = (RectTransform)frame.transform;
            _portraitFrame.SetParent(_box, false);
            _portraitFrame.anchorMin = _portraitFrame.anchorMax = new Vector2(0f, 1f);
            _portraitFrame.pivot = new Vector2(0f, 1f);
            _portraitFrame.sizeDelta = new Vector2(PortraitSize, PortraitSize);

            var back = NewChild<Image>(_portraitFrame, "Back");
            back.sprite = DialogueArt.Panel;
            back.type = Image.Type.Sliced;
            back.pixelsPerUnitMultiplier = SmallSliceMultiplier * 0.6f;
            back.color = new Color(0.01f, 0.02f, 0.04f, 0.95f);
            back.raycastTarget = false;
            Stretch(back.rectTransform, -Overhang * 0.5f);

            // A mask keeps the bust and its scanlines inside the frame while it breathes and sways.
            var window = NewChild<RectMask2D>(_portraitFrame, "Window");
            RectTransform windowRect = (RectTransform)window.transform;
            Stretch(windowRect, 4f);

            _portraitGlow = NewChild<RawImage>(windowRect, "Glow");
            _portraitGlow.texture = DialogueArt.Radial;
            _portraitGlow.raycastTarget = false;
            Stretch(_portraitGlow.rectTransform, -PortraitSize * 0.15f);
            _portraitGlow.rectTransform.anchoredPosition = new Vector2(0f, PortraitSize * 0.08f);

            _portrait = NewChild<Image>(windowRect, "Bust");
            _portrait.preserveAspect = true;
            _portrait.raycastTarget = false;
            Stretch(_portrait.rectTransform);

            _scanlines = NewChild<RawImage>(windowRect, "Scanlines");
            _scanlines.texture = DialogueArt.Scanlines;
            _scanlines.color = new Color(0f, 0.02f, 0.05f, 0.75f);
            _scanlines.raycastTarget = false;
            _scanlines.uvRect = new Rect(0f, 0f, 1f, PortraitSize / (DialogueArt.Scanlines.height * ScanlinePixels));
            Stretch(_scanlines.rectTransform);

            _portraitBorder = NewChild<Image>(_portraitFrame, "Border");
            _portraitBorder.sprite = DialogueArt.Edge;
            _portraitBorder.type = Image.Type.Sliced;
            _portraitBorder.pixelsPerUnitMultiplier = SmallSliceMultiplier * 0.6f;
            _portraitBorder.raycastTarget = false;
            Stretch(_portraitBorder.rectTransform, -Overhang * 0.5f);
        }

        private void BuildText()
        {
            _ghost = NewText(_box, "Ghost", BodySize, TextAlignmentOptions.TopLeft);
            _ghost.color = SplitWarm;
            _ghost.fontStyle = FontStyles.Normal;
            _ghost.textWrappingMode = TextWrappingModes.Normal;
            _ghost.gameObject.SetActive(false);

            _body = NewText(_box, "Body", BodySize, TextAlignmentOptions.TopLeft);
            _body.color = BodyColour;
            _body.fontStyle = FontStyles.Normal;
            _body.textWrappingMode = TextWrappingModes.Normal;
            _body.enableAutoSizing = true;
            _body.fontSizeMin = BodyMinSize;
            _body.fontSizeMax = BodySize;
            _body.lineSpacing = 4f;
            _body.overflowMode = TextOverflowModes.Truncate;
            _ghost.lineSpacing = _body.lineSpacing;

            var layer = new GameObject("RedactedBars", typeof(RectTransform));
            _barLayer = (RectTransform)layer.transform;
            _barLayer.SetParent(_body.rectTransform, false);
            Stretch(_barLayer);
        }

        private void BuildFooter()
        {
            _buttonEdges = new Image[ButtonWords.Length];
            _buttonLabels = new TextMeshProUGUI[ButtonWords.Length];
            _buttons = new Button[ButtonWords.Length];
            FooterButton(NextSlot, Advance);
            FooterButton(SkipSlot, Skip);
            FooterButton(CodexSlot, OpenCodex);

            _arrow = NewChild<Image>(_box, "NextArrow");
            _arrow.sprite = DialogueArt.Arrow;
            _arrow.raycastTarget = false;
            RectTransform arrow = _arrow.rectTransform;
            arrow.anchorMin = arrow.anchorMax = new Vector2(1f, 0f);
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
            RefreshButtons();
        }

        // Each button names its key or pad button for the device in use ("A  NEXT", "Esc  SKIP"); the Codex button only shows
        // on a scan. Called on every opening and whenever the player switches between keyboard and gamepad.
        private void RefreshButtons()
        {
            if (_buttons == null)
            {
                return;
            }

            bool codex = _player != null && _player.CanOpenCodex;
            _buttons[CodexSlot].gameObject.SetActive(codex);
            _visibleButtons = codex ? ButtonWords.Length : CodexSlot;
            for (int slot = 0; slot < _buttons.Length; slot++)
            {
                string key = _input != null ? _input.PromptFor(ButtonActions[slot]).ToUpperInvariant() : string.Empty;
                _buttonLabels[slot].text = key.Length > 0 ? $"<color={KeyColour}>{key}</color>  {ButtonWords[slot]}" : ButtonWords[slot];
            }

            RectTransform arrow = _arrow.rectTransform;
            arrow.anchoredPosition = new Vector2(-(Overhang + Inset + (ButtonWidth + ButtonGap) * _visibleButtons + ArrowSize), FooterCentre());
        }

        private void FooterButton(int slot, UnityEngine.Events.UnityAction action)
        {
            int fromRight = slot;
            string label = ButtonWords[slot];
            var fill = NewChild<Image>(_box, label + "Button");
            fill.sprite = DialogueArt.Panel;
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = SmallSliceMultiplier;
            fill.color = Color.white;
            RectTransform rect = fill.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            float overhang = Overhang / SmallSliceMultiplier;
            rect.sizeDelta = new Vector2(ButtonWidth + overhang * 2f, ButtonHeight + overhang * 2f);
            rect.anchoredPosition = new Vector2(-(Overhang + Inset - overhang + fromRight * (ButtonWidth + ButtonGap)), Overhang + ButtonBottom - overhang);

            var button = fill.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colours = ColorBlock.defaultColorBlock;
            colours.normalColor = new Color(0.07f, 0.11f, 0.19f, 0.95f);
            colours.highlightedColor = new Color(0.16f, 0.27f, 0.45f, 1f);
            colours.pressedColor = new Color(0.25f, 0.4f, 0.65f, 1f);
            colours.selectedColor = colours.normalColor;
            colours.fadeDuration = 0.08f;
            button.colors = colours;
            button.onClick.AddListener(action);

            Image edge = NewChild<Image>(rect, "Edge");
            edge.sprite = DialogueArt.Edge;
            edge.type = Image.Type.Sliced;
            edge.pixelsPerUnitMultiplier = SmallSliceMultiplier;
            edge.raycastTarget = false;
            Stretch(edge.rectTransform);
            _buttonEdges[slot] = edge;

            TextMeshProUGUI text = NewText(rect, "Label", ButtonTextSize, TextAlignmentOptions.Center);
            // A long key name ("ENTER") shrinks a little rather than run past the button.
            text.enableAutoSizing = true;
            text.fontSizeMax = ButtonTextSize;
            text.fontSizeMin = ButtonTextMinSize;
            text.characterSpacing = SmallSpacing;
            Stretch(text.rectTransform, overhang);
            text.text = label;
            _buttonLabels[slot] = text;
            _buttons[slot] = button;
        }

        private void Advance() => _player?.Advance();

        private void Skip() => _player?.SkipConversation();

        private void OpenCodex() => _player?.RequestCodex();

        private static T NewChild<T>(RectTransform parent, string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        // Every label uses the skin's TextMeshPro face (round 35: Chakra Petch Medium, checked at 720p; Liberation Sans
        // without a skin). Small labels are bold capitals with wide spacing.
        private static TextMeshProUGUI NewText(RectTransform parent, string name, int size, TextAlignmentOptions alignment)
        {
            var text = NewChild<TextMeshProUGUI>(parent, name);
            UiKit.UseLabelFont(text);

            text.fontSize = size;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.fontStyle = FontStyles.Bold;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
