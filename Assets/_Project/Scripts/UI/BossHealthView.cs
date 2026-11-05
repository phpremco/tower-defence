using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The boss's health and armour as one wide bar at the top of the screen, named, for as long as the boss lives.
    /// The small bar over its head is lost in the crowd; this one tells the player at a glance whether the shots
    /// are still chewing through plate or finally into the flesh.
    /// </summary>
    public sealed class BossHealthView : MonoBehaviour
    {
        private const float Width = 560f;
        private const float Top = -64f;
        private const float HealthHeight = 20f;
        private const float ArmorHeight = 10f;
        private const float Gap = 3f;
        private const float LabelHeight = 30f;
        private const int LabelSize = 24;
        private const float Border = 2f;
        private static readonly Color Backing = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color HealthColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color ArmorColor = new Color(0.7f, 0.88f, 1f);
        private static readonly Color LabelColor = new Color(1f, 0.6f, 0.55f);
        private static readonly Color MarkerColor = new Color(1f, 0.92f, 0.75f, 0.95f);
        private const float MarkerWidth = 3f;
        private const float MarkerOverhang = 4f;
        // Larger than the 60 of round 4c: the learned weapon must read at a glance beside the bar.
        private const float IconSize = 92f;
        private const float IconInset = 6f;
        private const string CardSprite = "Card_Disabled";
        private const float StrikeLength = 1.15f;
        private const float StrikeWidth = 5f;
        private const float StrikeDegrees = 45f;
        private const int ShareSize = 22;
        private const float ShareLift = 2f;
        private const float LumaRed = 0.3f;
        private const float LumaGreen = 0.59f;
        private const float LumaBlue = 0.11f;
        private static readonly Color StrikeColour = new Color(0.7f, 0.4f, 1f, 0.9f);
        private static readonly Color RingColour = new Color(0.72f, 0.45f, 1f, 1f);
        private const float IconGap = 10f;
        private const float NoticeHeight = 26f;
        private const int NoticeSize = 20;
        private const float NoticeSeconds = 5f;
        private const float NoticeFade = 1f;
        private const string OraPrefix = "ORA: ";
        private static readonly Color OraColour = new Color(0.35f, 0.85f, 1f);
        // A learned weapon's icon is shown greyed out: that weapon does only part of its damage now.
        private static readonly Color DimmedIcon = new Color(0.72f, 0.72f, 0.78f, 0.8f);

        private readonly List<GameObject> _markers = new List<GameObject>();
        private RectTransform _healthRow;
        private int _phase = -1;

        private LevelContext _context;
        private RectTransform _root;
        private RectTransform _healthFill;
        private RectTransform _armorFill;
        private GameObject _armorRow;
        private Text _label;
        private Enemy _boss;
        private Image _dampened;
        private Image _dampenedCard;

        /// <summary>The boss bar with its name and rows, for the HUD keep-out (active only while a boss lives).</summary>
        public RectTransform Panel => _root;

        /// <summary>The dampened-weapon card that hangs beside the bar.</summary>
        public RectTransform DampenedCard => _dampenedCard != null ? _dampenedCard.rectTransform : null;
        private Text _dampenedShare;
        private readonly Dictionary<Sprite, Sprite> _greyed = new Dictionary<Sprite, Sprite>();
        private TowerDefinition _shownDampened;
        private Text _notice;
        private float _noticeLeft;

        public void Initialize(LevelContext context, RectTransform parent)
        {
            _context = context;
            _root = new GameObject("BossHealth", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.SetParent(parent, false);
            _root.anchorMin = new Vector2(0.5f, 1f);
            _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = new Vector2(0f, Top);
            _root.sizeDelta = new Vector2(Width, LabelHeight + ArmorHeight + Gap + HealthHeight);

            _label = UiKit.CreateFixedText(_root, string.Empty, LabelSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Width, LabelHeight));
            _label.color = LabelColor;

            _armorRow = Row(_root, "Armor", -LabelHeight, ArmorHeight, ArmorColor, out _armorFill);
            _healthRow = Row(_root, "Health", -(LabelHeight + ArmorHeight + Gap), HealthHeight, HealthColor, out _healthFill).GetComponent<RectTransform>();

            CreateDampened();
            _notice = UiKit.CreateFixedText(_root, string.Empty, NoticeSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -(LabelHeight + ArmorHeight + Gap + HealthHeight + Gap)), new Vector2(Width, NoticeHeight));
            _notice.color = OraColour;

            _root.gameObject.SetActive(false);
            context.EnemySpawned.Subscribe(OnSpawned);
            context.EnemyKilled.Subscribe(OnKilled);
            context.BossAdapted.Subscribe(OnAdapted);
        }

        private void OnDestroy()
        {
            foreach (Sprite grey in _greyed.Values)
            {
                Destroy(grey.texture);
                Destroy(grey);
            }

            if (_context == null)
            {
                return;
            }

            _context.EnemySpawned.Unsubscribe(OnSpawned);
            _context.EnemyKilled.Unsubscribe(OnKilled);
            _context.BossAdapted.Unsubscribe(OnAdapted);
        }

        // A dark backing with a fill that is cut from the left as the value drops.
        private static GameObject Row(RectTransform parent, string name, float top, float height, Color colour, out RectTransform fill)
        {
            var back = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            back.transform.SetParent(parent, false);
            back.color = Backing;
            back.raycastTarget = false;
            RectTransform rect = back.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(0f, height);

            var fillImage = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            fillImage.transform.SetParent(rect, false);
            fillImage.color = colour;
            fillImage.raycastTarget = false;
            fill = fillImage.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(Border, Border);
            fill.offsetMax = new Vector2(-Border, -Border);
            return back.gameObject;
        }

        private void OnSpawned(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                _boss = enemy;
                _phase = -1;
                _noticeLeft = 0f;
                _label.text = enemy.Definition.DisplayName.ToUpperInvariant();
                MarkPhases(enemy.Definition.Boss);
            }
        }

        private void OnKilled(Enemy enemy)
        {
            if (enemy == _boss)
            {
                _boss = null;
            }
        }

        private void LateUpdate()
        {
            bool show = _boss != null && _boss.IsAlive && _context.Outcome == LevelOutcome.Running;
            if (_root.gameObject.activeSelf != show)
            {
                _root.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            Fill(_healthFill, _boss.Health.Fraction);
            ShowPhase();
            ShowDampened();
            ShowNotice();
            bool armoured = _boss.Health.ArmorMax > 0f;
            if (_armorRow.activeSelf != armoured)
            {
                _armorRow.SetActive(armoured);
            }

            if (armoured)
            {
                Fill(_armorFill, _boss.Health.ArmorFraction);
            }
        }

        // A thin mark on the health bar where each later phase begins, so the player sees the next change coming.
        private void MarkPhases(BossDefinition boss)
        {
            foreach (GameObject marker in _markers)
            {
                Destroy(marker);
            }

            _markers.Clear();
            if (boss == null)
            {
                return;
            }

            for (int i = 1; i < boss.Phases.Length; i++)
            {
                var image = new GameObject("Phase" + i, typeof(RectTransform)).AddComponent<Image>();
                image.transform.SetParent(_healthRow, false);
                image.color = MarkerColor;
                image.raycastTarget = false;
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(boss.Phases[i].StartsAt, 0f);
                rect.anchorMax = new Vector2(boss.Phases[i].StartsAt, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(MarkerWidth, MarkerOverhang * 2f);
                rect.anchoredPosition = Vector2.zero;
                _markers.Add(image.gameObject);
            }
        }

        // A continent boss's name carries the phase it is in.
        private void ShowPhase()
        {
            BossDefinition boss = _boss.Definition.Boss;
            if (boss == null)
            {
                return;
            }

            int phase = boss.PhaseAt(_boss.Health.Fraction);
            if (phase != _phase)
            {
                _phase = phase;
                _label.text = $"{_boss.Definition.DisplayName.ToUpperInvariant()}  -  {boss.PhaseName(phase).ToUpperInvariant()}";
            }
        }

        // The card of the weapon the boss has learned at the right end of the bar: the tower's own picture from the tower
        // bar, drained of colour, under a violet strike, with the share it loses.
        private void CreateDampened()
        {
            _dampenedCard = new GameObject("Dampened", typeof(RectTransform)).AddComponent<Image>();
            _dampenedCard.transform.SetParent(_root, false);
            _dampenedCard.raycastTarget = false;
            Sprite frame = UiArtSet.Active != null ? UiArtSet.Active.Sprite(CardSprite) : null;
            _dampenedCard.sprite = frame;
            _dampenedCard.type = frame != null ? Image.Type.Sliced : Image.Type.Simple;
            _dampenedCard.color = frame != null ? Color.white : Backing;
            RectTransform rect = _dampenedCard.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(IconGap, 0f);
            rect.sizeDelta = new Vector2(IconSize, IconSize);

            _dampened = new GameObject("Picture", typeof(RectTransform)).AddComponent<Image>();
            _dampened.transform.SetParent(rect, false);
            _dampened.raycastTarget = false;
            _dampened.preserveAspect = true;
            _dampened.color = DimmedIcon;
            Stretch(_dampened.rectTransform, IconInset);

            Game.Core.BossFxArt art = Game.Core.BossFxArt.Active;
            if (art != null && art.Dampened != null)
            {
                // README round 29: the broken ring with its "off" slash over the greyed picture, in the adaptation violet.
                var ring = new GameObject("DampenedRing", typeof(RectTransform)).AddComponent<RawImage>();
                ring.transform.SetParent(rect, false);
                ring.raycastTarget = false;
                ring.texture = art.Dampened;
                ring.color = RingColour;
                Stretch(ring.rectTransform, IconInset);
            }
            else
            {
                var strike = new GameObject("Strike", typeof(RectTransform)).AddComponent<Image>();
                strike.transform.SetParent(rect, false);
                strike.raycastTarget = false;
                strike.color = StrikeColour;
                RectTransform strikeRect = strike.rectTransform;
                strikeRect.anchorMin = strikeRect.anchorMax = strikeRect.pivot = new Vector2(0.5f, 0.5f);
                strikeRect.sizeDelta = new Vector2(IconSize * StrikeLength, StrikeWidth);
                strikeRect.localRotation = Quaternion.Euler(0f, 0f, StrikeDegrees);
            }

            _dampenedShare = UiKit.CreateFixedText(rect, string.Empty, ShareSize, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0f, ShareLift), new Vector2(IconSize, ShareSize + ShareLift));
            _dampenedShare.color = OraColour;
            _dampenedCard.gameObject.SetActive(false);
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private void ShowDampened()
        {
            TowerDefinition learned = _context.BossDampened;
            if (learned == _shownDampened)
            {
                return;
            }

            _shownDampened = learned;
            _dampenedCard.gameObject.SetActive(false);
            if (learned == null || !TowerModelBuilder.HasModel(learned))
            {
                return;
            }

            float share = _boss != null && _boss.Definition.Boss != null ? _boss.Definition.Boss.Recognition.DampenedShare : 1f;
            _dampenedShare.text = $"-{Mathf.RoundToInt((1f - share) * 100f)}%";
            // The same key and model as the tower bar's card, so it is that very picture (cached since the HUD was built).
            ModelIconRenderer.Request(learned, stage => TowerModelBuilder.Build(stage, learned, 0), sprite =>
            {
                if (_dampened != null && _shownDampened == learned && sprite != null)
                {
                    _dampened.sprite = Greyed(sprite);
                    _dampenedCard.gameObject.SetActive(true);
                }
            });
        }

        // A colourless copy of a tower picture, made once per tower: tinting alone keeps the weapon's own colour.
        private Sprite Greyed(Sprite source)
        {
            if (_greyed.TryGetValue(source, out Sprite grey))
            {
                return grey;
            }

            Texture2D texture = source.texture;
            if (!texture.isReadable)
            {
                return source;
            }

            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                byte light = (byte)Mathf.RoundToInt(p.r * LumaRed + p.g * LumaGreen + p.b * LumaBlue);
                pixels[i] = new Color32(light, light, light, p.a);
            }

            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false) { filterMode = texture.filterMode };
            copy.SetPixels32(pixels);
            copy.Apply();
            grey = Sprite.Create(copy, source.rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
            _greyed[source] = grey;
            return grey;
        }

        private void OnAdapted(string line)
        {
            _notice.text = OraPrefix + line;
            _noticeLeft = NoticeSeconds;
        }

        // ORA's line stays a few seconds under the bar, then fades.
        private void ShowNotice()
        {
            _noticeLeft = Mathf.Max(0f, _noticeLeft - Time.unscaledDeltaTime);
            Color colour = OraColour;
            colour.a = Mathf.Clamp01(_noticeLeft / NoticeFade);
            _notice.color = colour;
        }

        private static void Fill(RectTransform fill, float fraction)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }
    }
}
