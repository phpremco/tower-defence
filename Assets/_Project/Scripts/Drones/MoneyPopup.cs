using UnityEngine;

namespace Game.Drones
{
    /// <summary>A "+money" that rises from a point in the world and fades out, so a delivery is seen as well as counted.</summary>
    public sealed class MoneyPopup : MonoBehaviour
    {
        private const float Seconds = 1.3f;
        private const float Rise = 1.2f;
        private const float CharacterSize = 0.12f;
        private const int FontSize = 48;
        private const float Lift = 0.6f;

        private TextMesh _text;
        private Color _color;
        private float _age;

        /// <summary>The interface font (the skin's), set at boot; the built-in font stands in when nothing set it.</summary>
        public static Font Font { get; set; }

        public static void Show(Transform parent, Vector3 position, int value, Color color)
        {
            var go = new GameObject("MoneyPopup");
            go.transform.SetParent(parent, false);
            go.transform.position = position + Vector3.up * Lift;
            var popup = go.AddComponent<MoneyPopup>();
            popup._color = color;
            popup._text = go.AddComponent<TextMesh>();
            popup._text.text = $"+{value}";
            popup._text.font = Font != null ? Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            popup._text.fontSize = FontSize;
            popup._text.characterSize = CharacterSize;
            popup._text.anchor = TextAnchor.MiddleCenter;
            popup._text.alignment = TextAlignment.Center;
            popup._text.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = popup._text.font.material;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / Seconds;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += Vector3.up * (Rise * Time.deltaTime / Seconds);
            // Always face the camera, so the number reads from the fixed tilted view.
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }

            _text.color = new Color(_color.r, _color.g, _color.b, 1f - t * t);
        }
    }
}
