using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The base radar shown over the moment the ground starts to move: a dark scope with range rings and a turning
    /// sweep, one contact, then ten, then dozens that crowd together into one red mass while the count gives up.
    /// Every pose comes from the shot time, so any moment can be sought.
    /// </summary>
    public sealed class RadarOverlay : MonoBehaviour
    {
        private const float Size = 420f;
        private const float Margin = 70f;
        private const int Contacts = 42;
        private const int SweepTrail = 7;
        private const float SweepDegreesPerSecond = 150f;
        private const float SweepTrailDegrees = 5f;
        private const float DotSize = 16f;
        private const float BlobSize = 300f;
        private const float FadeSpeed = 4f;
        private const float FirstContactAt = 0.08f;
        private const float TenContactsAt = 0.3f;
        private const float SwarmAt = 0.5f;
        private const float MassAt = 0.8f;
        private const float PingFade = 1.6f;
        private const int Seed = 1337;
        private const int HeaderSize = 22;
        private const int CountSize = 26;

        private static readonly Color ScopeColour = new Color(0.02f, 0.09f, 0.1f, 0.88f);
        private static readonly Color GridColour = new Color(0.3f, 0.95f, 0.85f, 0.35f);
        private static readonly Color SweepColour = new Color(0.4f, 1f, 0.85f, 0.75f);
        private static readonly Color ContactColour = new Color(1f, 0.75f, 0.3f);
        private static readonly Color HostileColour = new Color(1f, 0.15f, 0.1f);
        private static readonly Color TextColour = new Color(0.55f, 1f, 0.9f);

        private CanvasGroup _group;
        private RectTransform _sweep;
        private RectTransform[] _dots;
        private RawImage[] _dotImages;
        private Vector2[] _scatter;
        private Vector2[] _crowd;
        private RawImage _blob;
        private TextMeshProUGUI _count;
        private float _alpha;
        private bool _shown;
        private int _shownCount = int.MinValue;

        public bool IsShown => _shown;

        public void Build(RectTransform root)
        {
            RectTransform scope = NewRect("Scope", root);
            scope.anchorMin = new Vector2(1f, 0.5f);
            scope.anchorMax = new Vector2(1f, 0.5f);
            scope.pivot = new Vector2(1f, 0.5f);
            scope.sizeDelta = new Vector2(Size, Size);
            scope.anchoredPosition = new Vector2(-Margin, Size * 0.18f);
            _group = scope.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            Raw(scope, "Back", DialogueArt.Radial, ScopeColour, Size * 1.25f);
            for (int ring = 1; ring <= 3; ring++)
            {
                Raw(scope, $"Ring{ring}", SpriteKit.Ring(), GridColour, Size * ring / 3f);
            }

            Line(scope, "Cross", GridColour, new Vector2(Size, 2f), 0f);
            Line(scope, "CrossV", GridColour, new Vector2(2f, Size), 0f);

            _sweep = NewRect("Sweep", scope);
            _sweep.sizeDelta = Vector2.zero;
            for (int i = 0; i < SweepTrail; i++)
            {
                Color colour = SweepColour;
                colour.a *= 1f - i / (float)SweepTrail;
                RectTransform arm = Line(_sweep, $"Arm{i}", colour, new Vector2(i == 0 ? 3f : 6f, Size * 0.5f), -i * SweepTrailDegrees);
                arm.pivot = new Vector2(0.5f, 0f);
                arm.anchoredPosition = Vector2.zero;
            }

            _blob = Raw(scope, "Mass", DialogueArt.Radial, HostileColour, BlobSize);
            BuildContacts(scope);

            TextMeshProUGUI header = Text(scope, "Header", HeaderSize, "PERIMETER SCAN");
            header.rectTransform.anchoredPosition = new Vector2(0f, Size * 0.5f + 26f);
            _count = Text(scope, "Count", CountSize, string.Empty);
            _count.rectTransform.anchoredPosition = new Vector2(0f, -Size * 0.5f - 28f);
        }

        public void Show(bool shown) => _shown = shown;

        /// <summary>Poses the radar for this moment of its shot.</summary>
        public void Tick(float time, float progress)
        {
            _alpha = Mathf.MoveTowards(_alpha, _shown ? 1f : 0f, Time.unscaledDeltaTime * FadeSpeed);
            _group.alpha = _alpha;
            if (_alpha <= 0f)
            {
                return;
            }

            float sweep = time * SweepDegreesPerSecond;
            _sweep.localRotation = Quaternion.Euler(0f, 0f, -sweep);
            int visible = VisibleContacts(progress);
            float crowd = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SwarmAt, 1f, progress));
            for (int i = 0; i < _dots.Length; i++)
            {
                bool on = i < visible;
                _dots[i].gameObject.SetActive(on);
                if (!on)
                {
                    continue;
                }

                Vector2 position = Vector2.Lerp(_scatter[i], _crowd[i], crowd);
                _dots[i].anchoredPosition = position;
                // A contact flares when the sweep passes over it and fades until the next pass.
                float angle = Mathf.Atan2(position.x, position.y) * Mathf.Rad2Deg;
                float since = Mathf.Repeat(sweep - angle, 360f) / SweepDegreesPerSecond;
                float ping = Mathf.Exp(-since * PingFade);
                Color colour = Color.Lerp(ContactColour, HostileColour, crowd);
                colour.a = 0.35f + 0.65f * ping;
                _dotImages[i].color = colour;
            }

            float mass = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SwarmAt, MassAt, progress));
            float pulse = 1f + 0.08f * Mathf.Sin(time * 6f);
            _blob.rectTransform.localScale = Vector3.one * Mathf.Max(0.001f, mass * pulse);
            Color blob = HostileColour;
            blob.a = 0.75f * mass;
            _blob.color = blob;
            int shownCount = progress < MassAt ? visible : -1;
            if (shownCount != _shownCount)
            {
                _shownCount = shownCount;
                _count.text = shownCount >= 0 ? $"CONTACTS  {shownCount}" : "CONTACTS  ???";
            }

            _count.color = progress < MassAt ? TextColour : HostileColour;
        }

        private static int VisibleContacts(float progress)
        {
            if (progress < FirstContactAt)
            {
                return 0;
            }

            if (progress < TenContactsAt)
            {
                return 1;
            }

            if (progress < SwarmAt)
            {
                return 10;
            }

            return Mathf.RoundToInt(Mathf.Lerp(10f, Contacts, Mathf.InverseLerp(SwarmAt, MassAt, progress)));
        }

        // Contacts start scattered over the outer half of the scope, all on the side the road comes from, and end
        // packed in a tight mass that crawls inward.
        private void BuildContacts(RectTransform scope)
        {
            var random = new System.Random(Seed);
            _dots = new RectTransform[Contacts];
            _dotImages = new RawImage[Contacts];
            _scatter = new Vector2[Contacts];
            _crowd = new Vector2[Contacts];
            float radius = Size * 0.5f;
            var massCentre = new Vector2(-0.42f, 0.28f) * radius;
            for (int i = 0; i < Contacts; i++)
            {
                float angle = Mathf.Lerp(200f, 340f, (float)random.NextDouble()) * Mathf.Deg2Rad;
                float distance = Mathf.Lerp(0.45f, 0.92f, (float)random.NextDouble()) * radius;
                _scatter[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * distance;
                float spread = Mathf.Sqrt((float)random.NextDouble()) * radius * 0.2f;
                float turn = (float)random.NextDouble() * Mathf.PI * 2f;
                _crowd[i] = massCentre + new Vector2(Mathf.Cos(turn), Mathf.Sin(turn)) * spread;
                _dotImages[i] = Raw(scope, $"Contact{i}", DialogueArt.Radial, ContactColour, DotSize * (i == 0 ? 1.5f : 1f));
                _dots[i] = _dotImages[i].rectTransform;
                _dots[i].gameObject.SetActive(false);
            }

            _blob.rectTransform.anchoredPosition = massCentre;
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static RawImage Raw(RectTransform parent, string name, Texture texture, Color colour, float size)
        {
            RawImage image = NewRect(name, parent).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = colour;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        private static RectTransform Line(RectTransform parent, string name, Color colour, Vector2 size, float degrees)
        {
            Image image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, degrees);
            return rect;
        }

        private static TextMeshProUGUI Text(RectTransform parent, string name, int size, string text)
        {
            TextMeshProUGUI label = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 8f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = TextColour;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(Size, 36f);
            label.text = text;
            return label;
        }
    }
}
