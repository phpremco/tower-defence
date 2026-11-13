using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    /// <summary>
    /// Whether the mouse is over this control, for panels that describe the card under the pointer as well as the one
    /// holding the gamepad focus (never the pointer alone: every description also follows the focus).
    /// </summary>
    public sealed class PointerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool Hovered { get; private set; }

        public void OnPointerEnter(PointerEventData eventData) => Hovered = true;

        public void OnPointerExit(PointerEventData eventData) => Hovered = false;

        // A hidden card gets no exit, so it starts unpointed when it shows again.
        private void OnDisable() => Hovered = false;
    }
}
