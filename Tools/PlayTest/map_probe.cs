// Reports the state of the planet map: layer, region, focus, zoom, and where the visible badges and markers project.
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.WorldMapScreen>();
if (screen == null) { return "no map screen"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var t = typeof(Game.UI.WorldMapScreen);
var globe = (Game.UI.PlanetGlobe)t.GetField("_globe", flags).GetValue(screen);
var sb = new System.Text.StringBuilder();
sb.Append("entered=" + t.GetField("_entered", flags).GetValue(screen) + " region=" + t.GetField("_region", flags).GetValue(screen) + " focused=" + t.GetField("_focused", flags).GetValue(screen) + " chosen=" + t.GetField("_chosen", flags).GetValue(screen) + " zoom=" + globe.Zoom.ToString("0.00") + " turning=" + globe.Turning);
var caption = (UnityEngine.UI.Text)t.GetField("_caption", flags).GetValue(screen);
sb.Append(" caption='" + caption.text + "'");
foreach (var group in UnityEngine.Object.FindObjectsByType<UnityEngine.CanvasGroup>(UnityEngine.FindObjectsSortMode.None))
{
    if ((group.name == "Badge" || group.name == "Marker") && group.alpha > 0.05f)
    {
        var r = group.GetComponent<UnityEngine.RectTransform>();
        sb.Append(" | " + group.name + " a=" + group.alpha.ToString("0.00") + " at=" + r.anchoredPosition.ToString("0"));
    }
}
return sb.ToString();
