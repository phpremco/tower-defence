// Lists every slider on screen with its label and value, and which object the event system has selected.
var sb = new System.Text.StringBuilder();
foreach (var slider in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Slider>(UnityEngine.FindObjectsSortMode.None))
{
    sb.Append(slider.GetComponentInChildren<UnityEngine.UI.Text>().text).Append(" = ").Append(slider.value.ToString("0.00")).Append(" | ");
}

var selected = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
sb.Append("selected=").Append(selected != null ? selected.name : "none");
return sb.ToString();
