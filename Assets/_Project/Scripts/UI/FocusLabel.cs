using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Lights a control's label in the focus gold while the control holds the gamepad or keyboard focus, for controls
    /// whose own focus art is too small to find at a glance (the slider handle).
    /// </summary>
    public sealed class FocusLabel : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private static readonly Color FocusColour = new Color(1f, 0.85f, 0.4f);

        private Text _label;
        private Color _normal;

        public void Initialize(Text label)
        {
            _label = label;
            _normal = label.color;
        }

        public void OnSelect(BaseEventData eventData) => Paint(true);

        public void OnDeselect(BaseEventData eventData) => Paint(false);

        // A page that is hidden while one of its controls has the focus gets no deselect, so it starts unlit next time
        // and lights again if it comes back with the focus still on it.
        private void OnDisable() => Paint(false);

        private void OnEnable() => Paint(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);

        private void Paint(bool focused)
        {
            if (_label != null)
            {
                _label.color = focused ? FocusColour : _normal;
            }
        }
    }
}
