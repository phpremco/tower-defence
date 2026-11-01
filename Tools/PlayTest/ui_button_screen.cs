// Reports the screen pixel position of the active Button whose label is in Tools/PlayTest/map_arg.txt, for a simulated click.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/map_arg.txt").Trim();
foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None))
{
    var text = button.GetComponentInChildren<UnityEngine.UI.Text>();
    if (text == null || text.text != wanted || !button.gameObject.activeInHierarchy) { continue; }
    var canvas = button.GetComponentInParent<UnityEngine.Canvas>();
    UnityEngine.Vector2 p = UnityEngine.RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, button.transform.position);
    return p.x.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " " + p.y.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
}
return "no button " + wanted;
