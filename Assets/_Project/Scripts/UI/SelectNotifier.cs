using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    /// <summary>Tells a screen when a button gets focus (stick, d-pad or mouse hover), so details can follow the selection.</summary>
    public sealed class SelectNotifier : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public Action Selected;

        /// <summary>False when only the focus counts, so a mouse crossing other buttons on its way does not change the details.</summary>
        public bool FollowPointer = true;

        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (FollowPointer)
            {
                Selected?.Invoke();
            }
        }
    }
}
