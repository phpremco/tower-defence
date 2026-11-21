using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The next-wave preview in the HUD's top strip (round 78), to the right of the wave line: one chip per creature type of
    /// the wave that starts next (the one a call would bring), with its picture on the left and, beside it, a wing badge on
    /// flyers, a plate badge on armoured creatures and how many come. A boss shows as the boss marker and the word, on a red
    /// chip, not as its picture, so the creature itself stays a surprise; where the creatures come from is never shown. A wave with many types narrows the
    /// chips (the marks and the number slide over the picture) before anything is left out. It draws
    /// <see cref="WavePreview"/> as it is, reads the level's shared state and takes no input (nothing to hover or select).
    /// </summary>
    public sealed class WavePreviewView : MonoBehaviour
    {
        private const float ChipHeight = 60f;
        // A chip with room shows the picture and, beside it, the marks over the number; squeezed, that column covers the
        // picture's right side, down to a chip the size of the picture alone.
        private const float WideChip = 106f;
        private const float NarrowChip = 62f;
        private const float ChipGap = 6f;
        private const float PictureInset = 3f;
        private const float BadgeSize = 26f;
        private const float BadgeInset = 1f;
        private const float BadgeBackGrow = 4f;
        private const float BadgeEdge = 2f;
        private const float BossIconInset = 7f;
        // The word "BOSS" is wider than a number: its chip gets this much more when the strip has room.
        private const float BossExtraWidth = 24f;
        private const float WashInset = 3f;
        private const float LabelGap = 12f;
        private const float WaveLineGap = 30f;
        private const float RightGap = 18f;
        private const float CountInset = 4f;
        private const float CountHeight = 30f;
        private const float CountWidth = 46f;
        // A wave of the first continent sends six creature types at most; one chip more is the "+N" of a wider wave.
        private const int MaxChips = 7;
        private static readonly Color LabelColour = new Color(0.75f, 0.85f, 1f);
        private static readonly Color CountColour = new Color(0.95f, 0.98f, 1f);
        private static readonly Color CountEdge = new Color(0f, 0.02f, 0.06f, 0.95f);
        private static readonly Vector2 CountEdgeDistance = new Vector2(1.6f, -1.6f);
        private static readonly Color ChipFill = new Color(0.17f, 0.27f, 0.43f, 0.95f);
        private static readonly Color BossWash = new Color(0.5f, 0.07f, 0.06f, 0.78f);
        private static readonly Color BossText = new Color(1f, 0.9f, 0.84f);
        // Narrower than this, the word would sit on top of the marker: the word alone says it better.
        private const float BossMarkerMinWidth = 118f;
        private static readonly Color BadgeBack = new Color(0.02f, 0.05f, 0.1f, 0.9f);
        private static readonly Color FlyerBadge = new Color(0.55f, 0.85f, 1f);
        private static readonly Color PlateBadge = new Color(0.93f, 0.94f, 0.97f);

        private readonly List<WavePreviewEntry> _entries = new List<WavePreviewEntry>();
        private readonly HashSet<EnemyDefinition> _roster = new HashSet<EnemyDefinition>();
        private LevelContext _context;
        private RectTransform _safe;
        private RectTransform _root;
        private Text _waveLine;
        private Text _label;
        private Chip[] _chips;
        private float _rightReserve;
        private float _labelWidth;
        private WaveDefinition _shownWave;
        private bool _shownAny;
        private float _laidOutWidth = -1f;
        private float _laidOutLine = -1f;

        private sealed class Chip
        {
            public RectTransform Root;
            public Image Wash;
            public Image Picture;
            public Image Boss;
            public Image Flyer;
            public Image Plate;
            public Text Count;
            public EnemyDefinition Enemy;
        }

        /// <summary>What the strip lists right now, in its order; empty when there is no next wave.</summary>
        public IReadOnlyList<WavePreviewEntry> Entries => _entries;

        /// <summary>How many of <see cref="Entries"/> have a chip of their own; the rest are counted in the "+N" chip.</summary>
        public int ShownEntries { get; private set; }

        /// <summary>True while the strip is on screen.</summary>
        public bool Visible => _root != null && _root.gameObject.activeSelf;

        /// <param name="waveLine">The wave text the strip stands to the right of.</param>
        /// <param name="barHeight">Height of the top strip the chips are centred in.</param>
        /// <param name="rightReserve">Width kept free at the right edge of the safe area (the icon row).</param>
        public void Initialize(LevelContext context, RectTransform safe, Font font, Text waveLine, float barHeight, float rightReserve)
        {
            _context = context;
            _safe = safe;
            _waveLine = waveLine;
            _rightReserve = rightReserve;

            _root = new GameObject("WavePreview", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(safe, false);
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0f, 1f);
            _root.sizeDelta = new Vector2(0f, ChipHeight);
            _root.anchoredPosition = new Vector2(0f, -(barHeight - ChipHeight) * 0.5f);

            _label = CreateText(_root, "Label", font, TextAnchor.MiddleLeft, LabelColour);
            _label.text = Loc.Get("hud.preview_next");
            _labelWidth = _label.preferredWidth;
            RectTransform label = _label.rectTransform;
            label.anchorMin = label.anchorMax = label.pivot = new Vector2(0f, 0.5f);
            label.sizeDelta = new Vector2(_labelWidth, ChipHeight);

            _chips = new Chip[MaxChips];
            for (int i = 0; i < MaxChips; i++)
            {
                _chips[i] = CreateChip(_root, font);
            }

            Prewarm();
            Refresh();
        }

        // The safe area changes with the resolution and the HUD margin option; the strip then fits itself again.
        private void Update()
        {
            if (_safe != null && !Mathf.Approximately(_safe.rect.width, _laidOutWidth))
            {
                Layout();
            }
        }

        /// <summary>Called by the HUD after it has written the wave line, whenever the level's shared state changed.</summary>
        public void Refresh()
        {
            WaveDefinition wave = _context.Outcome == LevelOutcome.Running ? _context.NextWave : null;
            if (wave != _shownWave || !_shownAny)
            {
                _shownWave = wave;
                _shownAny = true;
                WavePreview.Collect(wave, _context.Difficulty, _entries);
                Layout();
                return;
            }

            // The wave line grows ("BOSS", a two-digit wave): the strip keeps its distance.
            if (!Mathf.Approximately(_waveLine.preferredWidth, _laidOutLine))
            {
                Layout();
            }
        }

        // Every creature of the level gets its picture made up front (one a frame), so a chip never waits for one mid-fight.
        private void Prewarm()
        {
            LevelRoster.Collect(_context.Level, _context.Difficulty, _roster);
            foreach (EnemyDefinition enemy in _roster)
            {
                if (!enemy.IsBoss)
                {
                    RequestPicture(enemy, Ignore);
                }
            }
        }

        private static void RequestPicture(EnemyDefinition enemy, System.Action<Sprite> done)
        {
            ModelIconRenderer.Request(enemy, stage => EnemyDisplayBuilder.Build(stage, enemy).gameObject, done, IconFraming.CreatureChip);
        }

        private static void Ignore(Sprite sprite)
        {
        }

        // Fits the wave's chips between the wave line and the icon row: at full width when there is room, narrower when
        // not, then without the label, and only then with the last types (the plain walkers: the list is ranked) folded
        // into a "+N" chip.
        private void Layout()
        {
            _laidOutWidth = _safe.rect.width;
            _laidOutLine = _waveLine.preferredWidth;
            _root.gameObject.SetActive(_entries.Count > 0);
            if (_entries.Count == 0)
            {
                ShownEntries = 0;
                return;
            }

            // A boss ranks first, so one look at the head of the list tells whether the wide boss chip is needed.
            float bossExtra = _entries[0].Boss ? BossExtraWidth : 0f;
            float left = _laidOutLine * 0.5f + WaveLineGap;
            float room = _laidOutWidth * 0.5f - _rightReserve - RightGap - left - bossExtra;
            float labelled = room - _labelWidth - LabelGap;
            bool withLabel = ChipWidth(labelled, _entries.Count) >= NarrowChip;
            float width = ChipWidth(withLabel ? labelled : room, _entries.Count);
            int places = width >= NarrowChip ? _entries.Count : Mathf.Min(MaxChips, Mathf.FloorToInt((room + ChipGap) / (NarrowChip + ChipGap)));
            width = Mathf.Clamp(width, NarrowChip, WideChip);
            ShownEntries = WavePreview.Shown(_entries, places, out int hidden);

            _root.anchoredPosition = new Vector2(left, _root.anchoredPosition.y);
            _label.enabled = withLabel;
            float x = withLabel ? _labelWidth + LabelGap : 0f;
            int used = 0;
            for (int i = 0; i < ShownEntries && used < MaxChips; i++)
            {
                Show(_chips[used++], _entries[i], width + (_entries[i].Boss ? bossExtra : 0f), ref x);
            }

            if (hidden > 0 && used < Mathf.Min(places, MaxChips))
            {
                ShowMore(_chips[used++], hidden, ref x);
            }

            for (int i = used; i < MaxChips; i++)
            {
                _chips[i].Root.gameObject.SetActive(false);
                _chips[i].Enemy = null;
            }
        }

        // The width each of a number of chips gets in a strip of the given width.
        private static float ChipWidth(float strip, int chips) => (strip - ChipGap * (chips - 1)) / chips;

        private static void Show(Chip chip, WavePreviewEntry entry, float width, ref float x)
        {
            Place(chip, width, ref x);
            // A boss's brood wears the boss chip's red wash: it belongs to the boss, not to the wave's own head count.
            chip.Wash.enabled = entry.Boss || entry.BroodOnly;
            chip.Boss.enabled = entry.Boss && width >= BossMarkerMinWidth;
            GameObject flyer = chip.Flyer.transform.parent.gameObject;
            var plate = (RectTransform)chip.Plate.transform.parent;
            flyer.SetActive(entry.Flies);
            plate.gameObject.SetActive(entry.Armoured);
            // Both marks on one creature sit side by side, the wing outermost.
            plate.anchoredPosition = new Vector2(-BadgeEdge - (entry.Flies ? BadgeSize + BadgeBackGrow : 0f), -BadgeEdge);
            // One boss needs no number: the word says more.
            chip.Count.text = entry.BroodOnly ? Loc.Get("hud.preview_brood")
                : entry.Brood ? Loc.Format("hud.preview_count_brood", entry.Count)
                : entry.Boss && entry.Count == 1 ? Loc.Get("hud.preview_boss") : Loc.Format("hud.preview_count", entry.Count);
            chip.Count.color = entry.Boss || entry.BroodOnly ? BossText : CountColour;
            chip.Count.alignment = TextAnchor.LowerRight;

            if (chip.Enemy == entry.Enemy)
            {
                return;
            }

            chip.Enemy = entry.Enemy;
            chip.Picture.enabled = false;
            if (!entry.Boss)
            {
                EnemyDefinition enemy = entry.Enemy;
                RequestPicture(enemy, sprite => ShowPicture(chip, enemy, sprite));
            }
        }

        // A picture can arrive after the chip moved on to another creature.
        private static void ShowPicture(Chip chip, EnemyDefinition enemy, Sprite sprite)
        {
            if (chip.Root != null && chip.Enemy == enemy)
            {
                chip.Picture.sprite = sprite;
                chip.Picture.enabled = true;
            }
        }

        private static void ShowMore(Chip chip, int hidden, ref float x)
        {
            Place(chip, NarrowChip, ref x);
            chip.Enemy = null;
            chip.Wash.enabled = false;
            chip.Picture.enabled = false;
            chip.Boss.enabled = false;
            chip.Flyer.transform.parent.gameObject.SetActive(false);
            chip.Plate.transform.parent.gameObject.SetActive(false);
            chip.Count.text = Loc.Format("hud.preview_more", hidden);
            chip.Count.color = CountColour;
            chip.Count.alignment = TextAnchor.LowerRight;
        }

        private static void Place(Chip chip, float width, ref float x)
        {
            chip.Root.gameObject.SetActive(true);
            chip.Root.anchoredPosition = new Vector2(x, 0f);
            chip.Root.sizeDelta = new Vector2(width, ChipHeight);
            x += width + ChipGap;
        }

        private static Chip CreateChip(RectTransform parent, Font font)
        {
            var chip = new Chip();
            chip.Root = UiKit.CreatePanel(parent, "Chip", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, ChipFill).rectTransform;
            chip.Root.pivot = new Vector2(0f, 0.5f);
            chip.Root.sizeDelta = new Vector2(WideChip, ChipHeight);

            // The skin's panel is dark in itself and takes no warm tint: a boss chip is washed red inside its frame instead.
            chip.Wash = CreateImage(chip.Root, "BossWash", null, BossWash);
            Stretch(chip.Wash.rectTransform, WashInset);
            chip.Wash.enabled = false;

            chip.Picture = CreateImage(chip.Root, "Picture", null, Color.white);
            PlaceLeft(chip.Picture.rectTransform, PictureInset);
            chip.Picture.preserveAspect = true;
            chip.Picture.enabled = false;

            chip.Boss = CreateImage(chip.Root, "Boss", IconFactory.Get(IconType.BossMarker), Color.white);
            PlaceLeft(chip.Boss.rectTransform, PictureInset + BossIconInset);
            chip.Boss.preserveAspect = true;
            chip.Boss.enabled = false;

            chip.Flyer = CreateBadge(chip.Root, "Flyer", IconFactory.Get(IconType.Air), FlyerBadge);
            chip.Plate = CreateBadge(chip.Root, "Plate", TraitBadgeArt.Shield, PlateBadge);

            chip.Count = CreateText(chip.Root, "Count", font, TextAnchor.LowerRight, CountColour);
            RectTransform count = chip.Count.rectTransform;
            count.anchorMin = count.anchorMax = count.pivot = new Vector2(1f, 0f);
            count.anchoredPosition = new Vector2(-CountInset, 0f);
            count.sizeDelta = new Vector2(CountWidth, CountHeight);
            // Over a picture: a dark edge keeps the number readable on any creature, on every quality profile.
            var edge = chip.Count.gameObject.AddComponent<Outline>();
            edge.effectColor = CountEdge;
            edge.effectDistance = CountEdgeDistance;
            chip.Root.gameObject.SetActive(false);
            return chip;
        }

        // A mark in the chip's top right corner on a dark disc of its own, so it also reads over a picture.
        private static Image CreateBadge(RectTransform chip, string name, Sprite sprite, Color colour)
        {
            Image back = CreateImage(chip, name, IconFactory.Get(IconType.Disc), BadgeBack);
            RectTransform backRect = back.rectTransform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(1f, 1f);
            backRect.anchoredPosition = new Vector2(-BadgeEdge, -BadgeEdge);
            backRect.sizeDelta = new Vector2(BadgeSize + BadgeBackGrow, BadgeSize + BadgeBackGrow);
            Image mark = CreateImage(backRect, "Mark", sprite, colour);
            Stretch(mark.rectTransform, BadgeBackGrow * 0.5f + BadgeInset);
            mark.preserveAspect = true;
            return mark;
        }

        private static Image CreateImage(RectTransform parent, string name, Sprite sprite, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        // A square at the chip's left edge, the chip's height less the inset on every side.
        private static void PlaceLeft(RectTransform rect, float inset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(inset, 0f);
            rect.sizeDelta = new Vector2(ChipHeight - inset * 2f, ChipHeight - inset * 2f);
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Text CreateText(RectTransform parent, string name, Font font, TextAnchor alignment, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = UiKit.ReadableSize;
            text.alignment = alignment;
            text.color = colour;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
