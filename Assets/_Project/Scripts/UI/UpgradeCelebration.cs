using System.Collections;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Makes buying a permanent upgrade feel like an achievement: the preview flashes and spins, the new pip pops
    /// in with a burst of sparks, the stat bar grows to its new value while the number counts up, the points
    /// tally down with ticks, a banner rises over the model and a fanfare plays. The screen stays usable meanwhile.
    /// </summary>
    public sealed class UpgradeCelebration : MonoBehaviour
    {
        /// <summary>Everything one celebration animates. Null parts are skipped.</summary>
        public sealed class Cue
        {
            public RectTransform PreviewFrame;
            public ModelPreview Preview;
            public Image Pip;
            public Color PipColor;
            public RectTransform BarFill;
            public float BarFrom;
            public float BarTo;
            public Text Value;
            public float ValueFrom;
            public float ValueTo;
            public string ValueFormat;
            public string ValueSuffix;
            public Text Points;
            public int PointsFrom;
            public int PointsTo;
            public string PointsLabel;
            public Image TileStar;
            public bool Mastered;
            public string Title;
        }

        private const float FlashSeconds = 0.45f;
        private const float PipPopSeconds = 0.4f;
        private const float PipPopScale = 2.6f;
        private const float BarSeconds = 0.55f;
        private const float BarDelay = 0.15f;
        private const float PointsSeconds = 0.5f;
        private const float BannerSeconds = 1.1f;
        private const float BannerRise = 40f;
        private const int BannerSize = 32;
        private const int SparkCount = 16;
        private const float SparkSeconds = 0.7f;
        private const float SparkSize = 12f;
        private const float SparkSpeed = 260f;
        private const float SparkGravity = 320f;
        private const float StarPopSeconds = 0.5f;
        private const float StarPopScale = 2.2f;
        private const float TickInterval = 0.05f;
        private static readonly Color FlashColor = new Color(0.8f, 0.95f, 1f, 0.55f);
        private static readonly Color MasteredColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color UpgradedColor = new Color(0.55f, 0.95f, 1f);

        private AudioSource _audio;
        private AudioMix _mix;

        private void Awake()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            Services.TryGet(out _mix);
        }

        public void Play(Cue cue)
        {
            float gain = _mix != null ? _mix.EffectsGain : 1f;
            _audio.PlayOneShot(cue.Mastered ? ProceduralSfx.Mastery() : ProceduralSfx.Upgrade(), gain);
            cue.Preview?.Burst();

            if (cue.PreviewFrame != null)
            {
                StartCoroutine(Flash(cue.PreviewFrame));
                StartCoroutine(Banner(cue.PreviewFrame, cue.Mastered ? "MASTERED" : "UPGRADED", cue.Mastered ? MasteredColor : UpgradedColor));
            }

            if (cue.Pip != null)
            {
                StartCoroutine(PopPip(cue.Pip, cue.PipColor));
                StartCoroutine(Sparks(cue.Pip.rectTransform, cue.PipColor));
            }

            if (cue.BarFill != null || cue.Value != null)
            {
                StartCoroutine(GrowStat(cue));
            }

            if (cue.Points != null)
            {
                StartCoroutine(Tally(cue, gain));
            }

            if (cue.TileStar != null && cue.Mastered)
            {
                StartCoroutine(PopStar(cue.TileStar));
            }
        }

        private static IEnumerator Flash(RectTransform frame)
        {
            var image = new GameObject("Flash", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(frame, false);
            image.raycastTarget = false;
            Stretch(image.rectTransform);
            float t = 0f;
            while (t < FlashSeconds)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - t / FlashSeconds;
                image.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, FlashColor.a * a * a);
                yield return null;
            }

            Destroy(image.gameObject);
        }

        // The word rises from the middle of the preview and fades while it grows a little, like a stamp of approval.
        private static IEnumerator Banner(RectTransform frame, string word, Color color)
        {
            Text text = UiKit.CreateFixedText(frame, word, BannerSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(frame.rect.width, 80f));
            text.raycastTarget = false;
            text.fontStyle = FontStyle.Bold;
            float t = 0f;
            while (t < BannerSeconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / BannerSeconds);
                float pop = 1f + 0.35f * (1f - Mathf.Pow(1f - Mathf.Min(1f, p * 4f), 3f));
                text.rectTransform.localScale = Vector3.one * pop;
                text.rectTransform.anchoredPosition = new Vector2(0f, p * BannerRise);
                float alpha = p < 0.2f ? p / 0.2f : 1f - Mathf.Pow((p - 0.2f) / 0.8f, 2f);
                text.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            Destroy(text.gameObject);
        }

        private static IEnumerator PopPip(Image pip, Color color)
        {
            RectTransform rect = pip.rectTransform;
            float t = 0f;
            while (t < PipPopSeconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / PipPopSeconds);
                // Overshoot: big and white first, then settling to its size and colour with a small bounce.
                float back = 1f + (PipPopScale - 1f) * (1f - p) * (1f - p) + 0.15f * Mathf.Sin(p * Mathf.PI * 2f) * (1f - p);
                rect.localScale = Vector3.one * back;
                pip.color = Color.Lerp(Color.white, color, p);
                yield return null;
            }

            rect.localScale = Vector3.one;
            pip.color = color;
        }

        private IEnumerator Sparks(RectTransform origin, Color color)
        {
            RectTransform parent = origin.parent as RectTransform;
            var sparks = new RectTransform[SparkCount];
            var velocities = new Vector2[SparkCount];
            Vector2 start = origin.anchoredPosition;
            for (int i = 0; i < SparkCount; i++)
            {
                var image = new GameObject("Spark", typeof(RectTransform)).AddComponent<Image>();
                image.transform.SetParent(parent, false);
                image.sprite = IconFactory.Get(IconType.Disc);
                image.color = Color.Lerp(color, Color.white, 0.4f);
                image.raycastTarget = false;
                RectTransform rect = image.rectTransform;
                rect.anchorMin = origin.anchorMin;
                rect.anchorMax = origin.anchorMax;
                rect.pivot = origin.pivot;
                rect.anchoredPosition = start;
                float size = SparkSize * Random.Range(0.5f, 1.2f);
                rect.sizeDelta = new Vector2(size, size);
                float angle = (i + Random.value) / SparkCount * Mathf.PI * 2f;
                velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SparkSpeed * Random.Range(0.5f, 1f);
                sparks[i] = rect;
            }

            float t = 0f;
            while (t < SparkSeconds)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float fade = 1f - t / SparkSeconds;
                for (int i = 0; i < SparkCount; i++)
                {
                    velocities[i].y -= SparkGravity * dt;
                    sparks[i].anchoredPosition += velocities[i] * dt;
                    sparks[i].localScale = Vector3.one * fade;
                }

                yield return null;
            }

            for (int i = 0; i < SparkCount; i++)
            {
                Destroy(sparks[i].gameObject);
            }
        }

        private static IEnumerator GrowStat(Cue cue)
        {
            yield return new WaitForSecondsRealtime(BarDelay);
            float t = 0f;
            while (t < BarSeconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / BarSeconds);
                float eased = 1f - (1f - p) * (1f - p);
                if (cue.BarFill != null)
                {
                    cue.BarFill.anchorMax = new Vector2(Mathf.Lerp(cue.BarFrom, cue.BarTo, eased), 1f);
                }

                if (cue.Value != null)
                {
                    cue.Value.text = Mathf.Lerp(cue.ValueFrom, cue.ValueTo, eased).ToString(cue.ValueFormat) + cue.ValueSuffix;
                    cue.Value.color = Color.Lerp(Color.white, UpgradedColor, Mathf.Sin(p * Mathf.PI));
                }

                yield return null;
            }

            if (cue.Value != null)
            {
                cue.Value.text = cue.ValueTo.ToString(cue.ValueFormat) + cue.ValueSuffix;
                cue.Value.color = Color.white;
            }
        }

        private IEnumerator Tally(Cue cue, float gain)
        {
            float t = 0f;
            float nextTick = 0f;
            while (t < PointsSeconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / PointsSeconds);
                int shown = Mathf.RoundToInt(Mathf.Lerp(cue.PointsFrom, cue.PointsTo, p));
                cue.Points.text = $"{cue.PointsLabel} {shown}";
                if (t >= nextTick)
                {
                    nextTick += TickInterval;
                    _audio.PlayOneShot(ProceduralSfx.Tick(), gain * 0.5f);
                }

                yield return null;
            }

            cue.Points.text = $"{cue.PointsLabel} {cue.PointsTo}";
        }

        private static IEnumerator PopStar(Image star)
        {
            star.enabled = true;
            RectTransform rect = star.rectTransform;
            float t = 0f;
            while (t < StarPopSeconds)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / StarPopSeconds);
                rect.localScale = Vector3.one * (1f + (StarPopScale - 1f) * (1f - p) * (1f - p));
                rect.localRotation = Quaternion.Euler(0f, 0f, (1f - p) * 180f);
                yield return null;
            }

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
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
