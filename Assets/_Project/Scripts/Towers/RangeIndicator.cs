using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>A glowing ring on the ground that shows the range of a tower.</summary>
    public sealed class RangeIndicator : MonoBehaviour
    {
        private const int Segments = 72;
        private const float Height = 0.07f;
        private const float Width = 0.07f;
        private const float Emission = 3f;

        private LineRenderer _line;

        public static RangeIndicator Create(Transform parent)
        {
            var go = new GameObject("RangeIndicator");
            go.transform.SetParent(parent, false);
            var indicator = go.AddComponent<RangeIndicator>();
            indicator._line = go.AddComponent<LineRenderer>();
            indicator._line.loop = true;
            indicator._line.useWorldSpace = true;
            indicator._line.positionCount = Segments;
            indicator._line.widthMultiplier = Width;
            indicator._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            indicator.gameObject.SetActive(false);
            return indicator;
        }

        public void Show(Vector3 center, float radius, Color color)
        {
            gameObject.SetActive(true);
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
