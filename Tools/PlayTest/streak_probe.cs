// Lists the capsule streaks of the orbit shot: active, position, scale, colour.
var sb = new System.Text.StringBuilder();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.RectTransform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (!r.name.StartsWith("Capsule")) continue;
    var img = r.GetComponent<UnityEngine.UI.RawImage>();
    sb.Append(r.name + " act=" + r.gameObject.activeInHierarchy + " self=" + r.gameObject.activeSelf + " pos=" + r.anchoredPosition.ToString("0") + " sc=" + r.localScale.ToString("0.0") + " col=" + (img != null ? img.color.ToString() : "-") + " | ");
}
return sb.ToString();
