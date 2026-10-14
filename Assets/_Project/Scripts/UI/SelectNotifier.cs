using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    /// <summary>Tells a screen when a button gets focus (stick, d-pad or mouse hover), so details can follow the selection.</summary>
    public sealed class SelectNotifier : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public Action Selected;

        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();

        public void OnPointerEnter(PointerEventData eventData) => Selected?.Invoke();
    }
}
