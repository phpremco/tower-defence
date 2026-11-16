// Round 72: the UI object that holds the focus and its label (where the focus lands after a screen closes).
var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
if (selected == null) return "focus: none";
var text = selected.GetComponentInChildren<UnityEngine.UI.Text>();
return "focus: " + selected.name + " '" + (text != null ? text.text : "") + "'";
