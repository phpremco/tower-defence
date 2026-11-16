using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The screen a level loads behind (round 64, Remco: "when I click Easy it's frozen for a second"). It fades in over
    /// the map, stays up across the scene change while the level is built a step a frame, and fades out over the level
    /// once it stands and the first frames run smoothly. In the game's own look: the dark sky, the planet's limb in the
    /// level's palette with scan rings turning round it, the level's name, a line of advice from the story's hint data and
    /// a thin progress bar that eases after the load. Nothing on it can be pressed, so it needs no focus; it swallows
    /// clicks, and everything on it runs on unscaled time.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour, ILevelLoadProgress
    {
        private const int SortingOrder = 32000;
        private const float FadeInSeconds = 0.3f;
        private const float FadeOutSeconds = 0.45f;

        /// <summary>The most a fade moves in one frame: a long frame must not turn a fade into a cut.</summary>
        private const float MaxFadeStepSeconds = 1f / 30f;

        /// <summary>The screen stays at least this long, so a level that loads in a blink does not flash a screen past the player.</summary>
        private const float MinVisibleSeconds = 1.1f;

        /// <summary>After the load the screen waits for this many smooth frames in a row (the level's first frames compile shaders and fill pools), at most <see cref="SettleTimeoutSeconds"/>.</summary>
        private const int SettleFrames = 4;
        private const float SmoothFrameSeconds = 0.05f;
        private const float SettleTimeoutSeconds = 2f;

        /// <summary>A load that reports nothing for this long is taken for broken: the screen lifts so the error underneath shows.</summary>
        private const float StallSeconds = 30f;

        private const float CatchUpPerSecond = 5f;
        private const float CrawlPerSecond = 0.02f;
        private const float HoldBack = 0.97f;
        private const float FinishPerSecond = 1.5f;

        private const float TipSeconds = 5.5f;
        private const float TipFadeSeconds = 0.35f;

        private const int PlanetPixels = 384;
        private const int RingPixels = 512;
        private const int GlowPixels = 128;
        private const float PlanetSize = 980f;
        private const float InnerRingSize = 1180f;
        private const float OuterRingSize = 1420f;
        private const float InnerRingSpin = 14f;
        private const float OuterRingSpin = -8f;
        private const float GlowSize = 1900f;
        private static readonly Vector2 PlanetOffset = new Vector2(-150f, -40f);
        private static readonly Vector3 SunDirection = new Vector3(-0.75f, 0.45f, 0.5f);

        private const int LabelSize = 28;
        private const int TitleSize = 88;
        private const int SubtitleSize = 36;
        private const int TipSize = 32;
        private const float TextWidth = 1040f;
        private const float BarHeight = 8f;
        private const float HeadSize = 44f;
        private const float StartSize = 26f;

        /// <summary>The words keep this far from the frame's edges; the safe frame only adds what a television needs, which is nothing on a monitor.</summary>
        private const float SideMargin = 96f;
        private const float TopMargin = 120f;
        private const float BottomMargin = 72f;

        private static readonly Color Night = new Color(0.02f, 0.03f, 0.055f);
        private static readonly Color Accent = new Color(0.3f, 0.75f, 1f);
        private static readonly Color Dim = new Color(0.62f, 0.72f, 0.82f);

        private readonly LoadingProgress _progress = new LoadingProgress(CatchUpPerSecond, CrawlPerSecond, HoldBack, FinishPerSecond);
        private readonly List<Object> _owned = new List<Object>();
        private CanvasGroup _group;
        private RectTransform _fill;
        private RectTransform _head;
        private Text _percent;
        private Text _tipSpeaker;
        private Text _tipText;
        private CanvasGroup _tipGroup;
        private RectTransform _innerRing;
        private RectTransform _outerRing;
        private IReadOnlyList<LoadingTip> _tips;
        private int _tip;
        private float _tipClock;
        private float _fade;
        private float _visible;
        private float _settleClock;
        private float _quiet;
        private int _smooth;
        private bool _leaving;

        /// <summary>True once the screen hides everything under it: the scene may change without the player seeing it.</summary>
        public bool Covered => _fade >= 1f;

        /// <summary>The bar's value as drawn, for tests and probes.</summary>
        public float Shown => _progress.Shown;

        /// <summary>What the load last reported, for probes that time its steps.</summary>
        public float Reported => _progress.Reported;

        /// <summary>
        /// Puts the screen up over whatever is on screen and registers it as the load's progress. It lives across the scene
        /// change and removes itself when the load is complete.
        /// </summary>
        /// <param name="firstTips">How many of the tips, counted from the first, are about this level: the screen opens on one of those.</param>
        /// <param name="place">Where the level lies (its continent), for the line under its name; may be empty.</param>
        public static LoadingScreen Show(LevelDefinition level, DifficultyProfile difficulty, IReadOnlyList<LoadingTip> tips, int firstTips, string place)
        {
            var go = new GameObject("LoadingScreen");
            DontDestroyOnLoad(go);
            var screen = go.AddComponent<LoadingScreen>();
            screen._tips = tips ?? new LoadingTip[0];
            screen._tip = screen._tips.Count > 0 ? Random.Range(0, firstTips > 0 ? Mathf.Min(firstTips, screen._tips.Count) : screen._tips.Count) : 0;
            screen.Build(level, difficulty, place);
            Services.Register<ILevelLoadProgress>(screen);
            // Whatever button started the load keeps the focus under the screen; a second Confirm must not press it again.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return screen;
        }

        public void Report(float fraction)
        {
            _progress.Report(fraction);
            _quiet = 0f;
        }

        public void Complete() => _progress.Complete();

        private void Build(LevelDefinition level, DifficultyProfile difficulty, string place)
        {
            RectTransform safe = UiKit.CreateRoot(transform, false);
            var canvas = safe.GetComponentInParent<Canvas>();
            canvas.sortingOrder = SortingOrder;
            _group = canvas.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = true;
            _group.interactable = false;

            VisualTheme theme = level.Theme;
            Color accent = theme != null ? Color.Lerp(theme.AccentColor, Accent, 0.35f) : Accent;
            accent.a = 1f;
            BuildBackdrop(canvas.transform, theme, accent);
            safe.SetAsLastSibling();
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(safe, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(SideMargin, BottomMargin);
            content.offsetMax = new Vector2(-SideMargin, -TopMargin);
            BuildHeader(content, level, difficulty, place, accent);
            BuildTip(content);
            BuildBar(content, accent);
            ShowTip();
        }

        // The sky, the planet's limb and its rings fill the whole screen, past the safe frame the words keep to.
        private void BuildBackdrop(Transform canvas, VisualTheme theme, Color accent)
        {
            Color sky = theme != null ? Color.Lerp(Night, theme.BackgroundColor, 0.45f) : Night;
            sky.a = 1f;
            Image back = NewImage(canvas, "Backdrop", sky);
            back.raycastTarget = true;
            Stretch(back.rectTransform);

            Texture2D stars = UiArtSet.Active != null ? UiArtSet.Active.Starfield : null;
            if (stars != null)
            {
                RawImage starfield = NewRaw(canvas, "Stars", stars, Color.white);
                Stretch(starfield.rectTransform);
            }

            Color low = theme != null ? Color.Lerp(theme.GroundColor, Night, 0.25f) : new Color(0.1f, 0.14f, 0.2f);
            Color high = theme != null ? Color.Lerp(theme.AccentColor, theme.GroundColor, 0.35f) : Accent;
            Texture2D glow = Own(LoadingArt.Glow(GlowPixels));
            Place(NewRaw(canvas, "Glow", glow, new Color(accent.r, accent.g, accent.b, 0.22f)).rectTransform, GlowSize);
            RawImage planet = NewRaw(canvas, "Planet", null, Color.white);
            Place(planet.rectTransform, PlanetSize);
            RawImage inner = NewRaw(canvas, "RingInner", null, new Color(accent.r, accent.g, accent.b, 0.7f));
            _innerRing = inner.rectTransform;
            Place(_innerRing, InnerRingSize);
            RawImage outer = NewRaw(canvas, "RingOuter", null, new Color(accent.r, accent.g, accent.b, 0.5f));
            _outerRing = outer.rectTransform;
            Place(_outerRing, OuterRingSize);
            StartCoroutine(Paint(planet, inner, outer, low, high, accent));

            // The words stand on the left: the sky darkens there so they read over the stars and the planet's edge.
            RawImage shade = NewRaw(canvas, "Shade", Own(HorizontalFade()), new Color(sky.r * 0.6f, sky.g * 0.6f, sky.b * 0.6f, 0.9f));
            Stretch(shade.rectTransform);
        }

        // The planet and its rings are painted on the two frames after the screen appears, while it is still almost clear:
        // painting them with the rest made the click that starts a level a frame of a fifth of a second.
        private IEnumerator Paint(RawImage planet, RawImage inner, RawImage outer, Color low, Color high, Color air)
        {
            planet.enabled = inner.enabled = outer.enabled = false;
            yield return null;
            planet.texture = Own(LoadingArt.Planet(PlanetPixels, low, high, air, SunDirection));
            planet.enabled = true;
            yield return null;
            inner.texture = Own(LoadingArt.Ring(RingPixels, 1.6f, 0.3f, 0.22f));
            outer.texture = Own(LoadingArt.Ring(RingPixels, 1.2f, 0.18f, 0.12f));
            inner.enabled = outer.enabled = true;
        }

        // Top left, one under the other with air between: the small label, the level's name, where it lies and how hard, a rule.
        private void BuildHeader(RectTransform frame, LevelDefinition level, DifficultyProfile difficulty, string place, Color accent)
        {
            Text label = Plain(frame, Loc.Get("loading.deploying_to"), LabelSize, new Vector2(0f, 1f), Vector2.zero, new Vector2(TextWidth, 40f));
            label.color = accent;
            UiKit.CreateFixedText(frame, level.DisplayName.ToUpperInvariant(), TitleSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, -48f), new Vector2(TextWidth, 110f));
            string where = string.IsNullOrEmpty(place) ? difficulty.DisplayName : $"{place}  ·  {difficulty.DisplayName}";
            Text subtitle = Plain(frame, where, SubtitleSize, new Vector2(0f, 1f), new Vector2(0f, -168f), new Vector2(TextWidth, 50f));
            subtitle.color = Dim;
            Image rule = NewImage(frame, "Rule", new Color(accent.r, accent.g, accent.b, 0.8f));
            RectTransform rect = rule.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -236f);
            rect.sizeDelta = new Vector2(360f, 3f);
        }

        // Text without the display outline: a sentence reads better plain, and a small label next to a big title needs no glow.
        private static Text Plain(RectTransform parent, string words, int size, Vector2 anchor, Vector2 offset, Vector2 dimensions)
        {
            Text text = UiKit.CreateText(parent, words, size, anchor.x > 0.5f ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            UiKit.WrapInBox(text);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = offset;
            rect.sizeDelta = dimensions;
            return text;
        }

        private void BuildTip(RectTransform safe)
        {
            var holder = new GameObject("Tip", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(safe, false);
            holder.anchorMin = holder.anchorMax = holder.pivot = Vector2.zero;
            holder.anchoredPosition = new Vector2(0f, 96f);
            holder.sizeDelta = new Vector2(TextWidth, 190f);
            _tipGroup = holder.gameObject.AddComponent<CanvasGroup>();
            _tipSpeaker = Plain(holder, string.Empty, LabelSize, new Vector2(0f, 1f), Vector2.zero, new Vector2(TextWidth, 38f));
            _tipText = Plain(holder, string.Empty, TipSize, new Vector2(0f, 1f), new Vector2(0f, -48f), new Vector2(TextWidth, 140f));
        }

        private void BuildBar(RectTransform safe, Color accent)
        {
            Text loading = Plain(safe, Loc.Get("loading.loading"), LabelSize, Vector2.zero, new Vector2(0f, 22f), new Vector2(400f, 40f));
            loading.color = Dim;
            _percent = Plain(safe, "0%", LabelSize, new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(200f, 40f));
            _percent.color = accent;

            Image track = NewImage(safe, "Track", new Color(1f, 1f, 1f, 0.1f));
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(0f, BarHeight);

            Image fill = NewImage(trackRect, "Fill", accent);
            _fill = fill.rectTransform;
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(0f, 1f);
            _fill.offsetMin = Vector2.zero;
            _fill.offsetMax = Vector2.zero;

            // The bar grows out of a small light at its start instead of a square end.
            RectTransform start = NewRaw(trackRect, "Start", Own(LoadingArt.Glow(GlowPixels)), new Color(accent.r, accent.g, accent.b, 0.9f)).rectTransform;
            start.anchorMin = start.anchorMax = new Vector2(0f, 0.5f);
            start.pivot = new Vector2(0.5f, 0.5f);
            start.sizeDelta = new Vector2(StartSize, StartSize);

            // A soft light rides the end of the fill, so the bar reads as energy and its end is never a hard cut.
            _head = NewRaw(trackRect, "Head", Own(LoadingArt.Glow(GlowPixels)), new Color(accent.r, accent.g, accent.b, 0.9f)).rectTransform;
            _head.anchorMin = _head.anchorMax = new Vector2(0f, 0.5f);
            _head.pivot = new Vector2(0.5f, 0.5f);
            _head.sizeDelta = new Vector2(HeadSize, HeadSize);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _visible += dt;
            _quiet += dt;
            _progress.Tick(dt);
            _fill.anchorMax = new Vector2(_progress.Shown, 1f);
            _head.anchorMin = _head.anchorMax = new Vector2(_progress.Shown, 0.5f);
            _percent.text = Mathf.FloorToInt(_progress.Shown * 100f) + "%";
            _innerRing.Rotate(0f, 0f, InnerRingSpin * dt);
            _outerRing.Rotate(0f, 0f, OuterRingSpin * dt);
            TickTip(dt);

            float fadeStep = Mathf.Min(dt, MaxFadeStepSeconds);
            if (!_leaving)
            {
                _fade = Mathf.MoveTowards(_fade, 1f, fadeStep / FadeInSeconds);
                _leaving = ReadyToLeave(dt);
            }
            else
            {
                _fade = Mathf.MoveTowards(_fade, 0f, fadeStep / FadeOutSeconds);
                if (_fade <= 0f)
                {
                    Destroy(gameObject);
                }
            }

            _group.alpha = Mathf.SmoothStep(0f, 1f, _fade);
        }

        // The load is done, the bar is full, the screen has been up long enough and the level under it runs smoothly.
        private bool ReadyToLeave(float dt)
        {
            if (_quiet > StallSeconds)
            {
                return true;
            }

            if (!_progress.IsFull || _visible < MinVisibleSeconds)
            {
                return false;
            }

            _settleClock += dt;
            _smooth = dt <= SmoothFrameSeconds ? _smooth + 1 : 0;
            return _smooth >= SettleFrames || _settleClock >= SettleTimeoutSeconds;
        }

        private void TickTip(float dt)
        {
            if (_tips.Count == 0)
            {
                return;
            }

            _tipClock += dt;
            float alpha = Mathf.Clamp01(_tipClock / TipFadeSeconds);
            if (_tips.Count > 1)
            {
                alpha = Mathf.Min(alpha, Mathf.Clamp01((TipSeconds - _tipClock) / TipFadeSeconds));
                if (_tipClock >= TipSeconds)
                {
                    _tip = (_tip + 1) % _tips.Count;
                    ShowTip();
                }
            }

            _tipGroup.alpha = Mathf.SmoothStep(0f, 1f, alpha);
        }

        private void ShowTip()
        {
            _tipClock = 0f;
            _tipGroup.alpha = 0f;
            if (_tips.Count == 0)
            {
                return;
            }

            LoadingTip tip = _tips[_tip];
            _tipSpeaker.text = Loc.Format("loading.tip_speaker", tip.Speaker.ToUpperInvariant());
            _tipSpeaker.color = tip.Colour;
            _tipText.text = PromptText.ForText(tip.Text);
        }

        private void OnDestroy()
        {
            if (Services.TryGet(out ILevelLoadProgress current) && ReferenceEquals(current, this))
            {
                Services.Unregister<ILevelLoadProgress>();
            }

            foreach (Object owned in _owned)
            {
                Destroy(owned);
            }
        }

        private Texture2D Own(Texture2D texture)
        {
            _owned.Add(texture);
            return texture;
        }

        // Dark on the left, clear from the middle on: a two-pixel-high strip the canvas stretches.
        private static Texture2D HorizontalFade()
        {
            const int width = 64;
            var texture = new Texture2D(width, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[width * 2];
            for (int x = 0; x < width; x++)
            {
                float alpha = 1f - Mathf.SmoothStep(0.2f, 0.7f, x / (float)(width - 1));
                pixels[x] = pixels[width + x] = new Color(1f, 1f, 1f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // The planet stands right of the middle and runs off the right edge at any aspect.
        private static void Place(RectTransform rect, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = PlanetOffset;
            rect.sizeDelta = new Vector2(size, size);
        }

        private static Image NewImage(Transform parent, string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage NewRaw(Transform parent, string name, Texture texture, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.color = colour;
            image.raycastTarget = false;
            return image;
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
