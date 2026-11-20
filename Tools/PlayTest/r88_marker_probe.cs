// Round 88a: the HUD markers of the surprise ping as placed: target, where the camera sees it, the arrow's place and turn.
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.SurprisePingView>();
if (view == null) { return "no view"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var markers = (System.Array)typeof(Game.UI.SurprisePingView).GetField("_markers", flags).GetValue(view);
var area = (UnityEngine.RectTransform)typeof(Game.UI.SurprisePingView).GetField("_area", flags).GetValue(view);
var camera = (UnityEngine.Camera)typeof(Game.UI.SurprisePingView).GetField("_camera", flags).GetValue(view);
var sb = new System.Text.StringBuilder("screen " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + " camera " + camera.pixelWidth + "x" + camera.pixelHeight + " area " + area.rect + " | ");
foreach (object marker in markers)
{
    var t = marker.GetType();
    var target = (UnityEngine.Vector3)t.GetField("Target").GetValue(marker);
    var arrow = (UnityEngine.RectTransform)t.GetField("Arrow").GetValue(marker);
    var ring = (UnityEngine.RectTransform)t.GetField("Ring").GetValue(marker);
    sb.Append("target ").Append(target.ToString("0.0")).Append(" screen ").Append(camera.WorldToScreenPoint(target).ToString("0")).Append(" arrow ").Append(arrow.gameObject.activeSelf ? arrow.anchoredPosition.ToString("0") + " turn " + arrow.localEulerAngles.z.ToString("0") : "off").Append(" ring ").Append(ring.gameObject.activeSelf ? ring.anchoredPosition.ToString("0") : "off").Append(" | ");
}
return sb.ToString();
