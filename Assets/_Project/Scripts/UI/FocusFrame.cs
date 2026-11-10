using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A glowing gold frame round a button while it holds the keyboard or gamepad focus. The focus sprite alone is a thin
    /// blue line that vanished on the dark menu buttons; the frame reads at a glance from the couch. It breathes a little
    /// so the eye finds it, on unscaled time because menus show while the game clock stands still.
    /// </summary>
    public sealed class FocusFrame : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private const float Outset = 6f;
        private const float PulseSpeed = 4f;
        private const float MinAlpha = 0.7f;
        private static readonly Color Gold = new Color(1f, 0.83f, 0.35f, 1f);

        private Image _frame;

        /// <summary>Adds the frame to a button; without the dialogue art (a bare test scene) it adds nothing.</summary>
        public static void Add(Selectable button)
        {
            Sprite edge = DialogueArt.Edge;
            if (edge == null)
            {
                return;
            }

            var go = new GameObject("FocusFrame", typeof(RectTransform));
            go.transform.SetParent(button.transform, false);
            var image = go.AddComponent<Image>();
            image.sprite = edge;
            image.type = Image.Type.Sliced;
            image.color = Gold;
            image.raycastTarget = false;
            image.enabled = false;
            RectTransform rect = image.rectTransform;
            float reach = DialogueArt.EdgeOverhang + Outset;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-reach, -reach);
            rect.offsetMax = new Vector2(reach, reach);

            var frame = button.gameObject.AddComponent<FocusFrame>();
            frame._frame = image;
        }

        public void OnSelect(BaseEventData eventData) => _frame.enabled = true;

        public void OnDeselect(BaseEventData eventData) => _frame.enabled = false;

        // A button hidden while focused gets no deselect call; check it is still the one focused.
        private void Update()
        {
            if (!_frame.enabled)
            {
                return;
            }

            EventSystem events = EventSystem.current;
            if (events == null || events.currentSelectedGameObject != gameObject)
            {
                _frame.enabled = false;
                return;
            }

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
            _frame.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Lerp(MinAlpha, 1f, wave));
        }

        private void OnEnable()
        {
            EventSystem events = EventSystem.current;
            if (_frame != null)
            {
                _frame.enabled = events != null && events.currentSelectedGameObject == gameObject;
            }
        }

        private void OnDisable()
        {
            if (_frame != null)
            {
                _frame.enabled = false;
            }
        }
    }
}
