using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// The range of a tower or path object on the ground: the painted ring band with its slowly turning dash over a
    /// faint fill (README round 25b), scaled to the range and tinted valid or invalid. Without the art a plain line.
    /// </summary>
    public sealed class RangeIndicator : MonoBehaviour
    {
        private const int Segments = 72;
        private const float Height = 0.07f;
        private const float Width = 0.07f;
        private const float Emission = 3f;
        // The ring and fill stay below the bloom threshold (README): a hot rim makes the bloom fill the whole disc.
        private const float RingStrength = 0.8f;

        private LineRenderer _line;
        private MeshRenderer _band;
        private MeshRenderer _fill;
        private MaterialPropertyBlock _block;

        public static RangeIndicator Create(Transform parent)
        {
            var go = new GameObject("RangeIndicator");
            go.transform.SetParent(parent, false);
            var indicator = go.AddComponent<RangeIndicator>();
            MarkerArt art = MarkerArt.Active;
            if (art != null && art.Has(MarkerKind.RangeBand) && art.Has(MarkerKind.RangeFill))
            {
                indicator._fill = art.Place(MarkerKind.RangeFill, go.transform);
                indicator._band = art.Place(MarkerKind.RangeBand, go.transform);
                indicator._block = new MaterialPropertyBlock();
            }
            else
            {
                indicator._line = go.AddComponent<LineRenderer>();
                indicator._line.loop = true;
                indicator._line.useWorldSpace = true;
                indicator._line.positionCount = Segments;
                indicator._line.widthMultiplier = Width;
                indicator._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            indicator.gameObject.SetActive(false);
            return indicator;
        }

        public void Show(Vector3 center, float radius, Color color)
        {
            gameObject.SetActive(true);
            if (_band != null)
            {
                transform.position = new Vector3(center.x, center.y + Height, center.z);
                transform.localScale = new Vector3(radius, 1f, radius);
                var tint = new Color(color.r * RingStrength, color.g * RingStrength, color.b * RingStrength, 1f);
                MarkerArt.SetTint(_band, _block, tint);
                MarkerArt.SetTint(_fill, _block, tint);
                return;
            }

            _line.sharedMaterial = PlaceholderVisuals.MaterialFor(color, Emission);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                _line.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * radius, Height, center.z + Mathf.Sin(angle) * radius));
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
