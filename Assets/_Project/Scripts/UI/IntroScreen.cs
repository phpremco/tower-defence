using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The opening cutscene, played once on first launch: the story beats fade in over stars, the planet closing in,
    /// its regions lighting up and the dive onto the first region. Any button skips it. Ends in the main menu.
    /// </summary>
    public sealed class IntroScreen : ScreenBase
    {
        private const int PlanetTexture = 1024;
        private const int HaloTexture = 256;
        private const float PlanetSize = 900f;
        private const float HaloScale = 1.25f;
        private const float RegionRadius = 0.3f;
        private const float MapWidth = 1500f;
        private const float MapHeight = 560f;
        private const float FrameScale = MapWidth / PlanetSize;
        private const float ApproachFrom = 0.08f;
        private const float ApproachTo = 0.7f;
        private const float SurfaceTo = 1f;
        private const float LandingTo = 3.2f;
        private const float TextFade = 0.7f;
        private const float EndFade = 1.4f;
        private const float RegionBlinkRate = 3f;
        private const float ShipSize = 10f;
        private const float ShipTrail = 0.6f;
        private const int TextSize = 40;
        private const float TextWidth = 1300f;
        private const float TextBottom = 130f;
        private const int HintSize = 22;
        private static readonly Vector2 ShipStart = new Vector2(-900f, -420f);
        private static readonly Vector2 ShipEnd = new Vector2(-60f, 40f);
        private static readonly Color HaloColor = new Color(0.45f, 0.8f, 1f, 0.5f);
        private static readonly Color HintColor = new Color(0.6f, 0.7f, 0.85f, 0.7f);
        private static readonly Color ShipColor = new Color(0.8f, 0.95f, 1f);

        private CampaignCatalog _catalog;
        private IInputProvider _input;
        private IReadOnlyList<StoryBeat> _beats;
        private RectTransform _planet;
        private RawImage _regions;
        private RectTransform _ship;
        private CanvasGroup _textGroup;
        private Text _text;
        private Image _fade;
        private Vector2 _landingPoint;
        private int _beat = -1;
        private float _elapsed;
        private bool _finished;

        protected override void Build(RectTransform root)
        {
            if (!Services.TryGet(out _catalog) || _catalog.Story == null || _catalog.Story.IntroBeats.Count == 0)
            {
                Finish();
                return;
            }

            _beats = _catalog.Story.IntroBeats;
            if (Services.TryGet(out _input))
            {
                _input.Confirmed += Finish;
                _input.Cancelled += Finish;
            }

            BuildPlanet(root);
            BuildShip(root);
            BuildText(root);
            _fade = UiKit.CreatePanel(root, "Fade", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            _fade.sprite = null;
            _fade.color = new Color(0f, 0f, 0f, 0f);
            NextBeat();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Confirmed -= Finish;
                _input.Cancelled -= Finish;
            }
        }

        private void BuildPlanet(RectTransform root)
        {
            _planet = CreateRect("Planet", root, new Vector2(PlanetSize, PlanetSize));
            RawImage halo = CreateRawImage("Halo", _planet, PlanetPainter.PaintHalo(HaloTexture), HaloColor);
            halo.rectTransform.sizeDelta = new Vector2(PlanetSize * HaloScale, PlanetSize * HaloScale);

            var regions = new List<PlanetPainter.Region>();
            var lit = new List<PlanetPainter.Region>();
            foreach (MapNode node in _catalog.Nodes)
            {
                Color colour = node.Level != null && node.Level.Theme != null ? Color.Lerp(node.Level.Theme.GroundColor, node.Level.Theme.AccentColor, 0.5f) * 1.15f : Color.grey;
                colour.a = 1f;
                regions.Add(new PlanetPainter.Region { Position = node.MapPosition, Colour = colour, Radius = RegionRadius });
                lit.Add(new PlanetPainter.Region { Position = node.MapPosition, Colour = Color.Lerp(colour, Color.white, 0.5f), Radius = RegionRadius });
            }

            var frame = new Vector2(FrameScale, MapHeight / PlanetSize);
            CreateRawImage("Surface", _planet, PlanetPainter.PaintSurface(PlanetTexture, regions, frame), Color.white);
            // The same surface painted with brighter regions, faded in and out over the first to make the regions blink.
            _regions = CreateRawImage("Lit", _planet, PlanetPainter.PaintSurface(PlanetTexture, lit, frame), new Color(1f, 1f, 1f, 0f));
            _planet.localScale = Vector3.zero;

            Vector2 first = _catalog.Nodes.Count > 0 ? _catalog.Nodes[0].MapPosition : new Vector2(0.5f, 0.5f);
            _landingPoint = Vector2.Scale(first - new Vector2(0.5f, 0.5f), new Vector2(MapWidth, MapHeight)) * (PlanetSize / MapWidth);
        }

        private void BuildShip(RectTransform root)
        {
            _ship = CreateRect("Ship", root, new Vector2(ShipSize, ShipSize));
            var body = _ship.gameObject.AddComponent<Image>();
            body.sprite = IconFactory.Get(IconType.Disc);
            body.color = ShipColor;
            body.raycastTarget = false;
            var trail = new GameObject("Trail", typeof(RectTransform)).AddComponent<Image>();
            trail.transform.SetParent(_ship, false);
            trail.sprite = IconFactory.Get(IconType.Disc);
            trail.color = new Color(ShipColor.r, ShipColor.g, ShipColor.b, 0.35f);
            trail.raycastTarget = false;
            trail.rectTransform.sizeDelta = new Vector2(ShipSize * 2.4f, ShipSize * 2.4f);
            _ship.gameObject.SetActive(false);
        }

        private void BuildText(RectTransform root)
        {
            _text = UiKit.CreateFixedText(root, string.Empty, TextSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, TextBottom), new Vector2(TextWidth, 140f));
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _textGroup = _text.gameObject.AddComponent<CanvasGroup>();
            _textGroup.alpha = 0f;

            Text hint = UiKit.CreateFixedText(root, "Press any button to skip", HintSize, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-40f, 24f), new Vector2(500f, 30f));
            hint.color = HintColor;
        }

        private void Update()
        {
            if (_finished || _beats == null)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            StoryBeat beat = _beats[_beat];
            if (_elapsed >= beat.Seconds)
            {
                if (_beat + 1 >= _beats.Count)
                {
                    Finish();
                    return;
                }

                NextBeat();
                beat = _beats[_beat];
            }

            float progress = Mathf.Clamp01(_elapsed / beat.Seconds);
            _textGroup.alpha = Mathf.Min(Smooth(0f, TextFade, _elapsed), 1f - Smooth(beat.Seconds - TextFade, beat.Seconds, _elapsed));
            DrawScene(beat, progress);
        }

        private void NextBeat()
        {
            _beat++;
            _elapsed = 0f;
            _text.text = _beats[_beat].Text;
        }

        // Each scene moves the planet and the ship a little further: approaching, looming, then diving onto the landing site.
        private void DrawScene(StoryBeat beat, float progress)
        {
            float eased = Smooth(0f, 1f, progress);
            switch (beat.Scene)
            {
                case StoryScene.Space:
                    _planet.localScale = Vector3.zero;
                    _ship.gameObject.SetActive(false);
                    break;
                case StoryScene.Approach:
                    _planet.localScale = Vector3.one * Mathf.Lerp(ApproachFrom, ApproachTo, eased);
                    _planet.anchoredPosition = Vector2.zero;
                    _ship.gameObject.SetActive(true);
                    _ship.anchoredPosition = Vector2.Lerp(ShipStart, ShipEnd, eased);
                    _ship.localScale = Vector3.one * Mathf.Lerp(1.6f, ShipTrail, eased);
                    break;
                case StoryScene.Surface:
                    _planet.localScale = Vector3.one * Mathf.Lerp(ApproachTo, SurfaceTo, eased);
                    _ship.gameObject.SetActive(false);
                    _regions.color = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Sin(_elapsed * RegionBlinkRate));
                    break;
                case StoryScene.Landing:
                    float scale = Mathf.Lerp(SurfaceTo, LandingTo, eased);
                    _planet.localScale = Vector3.one * scale;
                    _planet.anchoredPosition = -_landingPoint * scale * eased;
                    _regions.color = new Color(1f, 1f, 1f, 0f);
                    _fade.color = new Color(0f, 0f, 0f, Smooth(beat.Seconds - EndFade, beat.Seconds, _elapsed));
                    break;
            }
        }

        private void Finish()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            Progress.MarkIntroSeen();
            Go(SceneNames.MainMenu);
        }

        private static float Smooth(float from, float to, float value)
        {
            float t = Mathf.Clamp01((value - from) / (to - from));
            return t * t * (3f - 2f * t);
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static RawImage CreateRawImage(string name, RectTransform parent, Texture2D texture, Color tint)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(parent, false);
            image.texture = texture;
            image.color = tint;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = parent.sizeDelta;
            return image;
        }
    }
}
