// Round 88b: raises surprise pings where a still needs them and prints what the HUD made of them.
// Tools/PlayTest/r88b_ping_arg.txt, one line of words:
//   "vp:<x>:<y>"  a ping at the ground point under that viewport point of the play camera (0..1 is on screen; outside
//                 it the point is off screen in that direction), "rear:<x>:<y>" Voss's rear-rift warning there,
//   "margin:<percent>" the HUD's title-safe margin for this session (in memory only),
//   "dump"        the screen, the canvas, the safe area, every keep-out zone and every showing world label, in the
//                 safe area's space (so a marker on a panel can be read off the numbers).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var inv = System.Globalization.CultureInfo.InvariantCulture;
var camera = UnityEngine.Camera.main;
var view = UnityEngine.Object.FindAnyObjectByType<Game.UI.SurprisePingView>();
if (view == null) { return "no ping view"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
foreach (string word in System.IO.File.ReadAllText("Tools/PlayTest/r88b_ping_arg.txt").Trim().Split(' '))
{
    if (word.StartsWith("vp:") || word.StartsWith("rear:"))
    {
        string[] p = word.Split(':');
        var ray = camera.ViewportPointToRay(new UnityEngine.Vector3(float.Parse(p[1], inv), float.Parse(p[2], inv), 0f));
        float t = -ray.origin.y / ray.direction.y;
        UnityEngine.Vector3 at = ray.origin + ray.direction * t;
        if (p[0] == "vp") { context.SurpriseSpotted.Raise(at); } else { context.RearNestWarned.Raise(at); }
        sb.Append(p[0]).Append(" at ").Append(at.ToString("0.0")).Append("; ");
    }
    else if (word.StartsWith("margin:"))
    {
        // The title-safe margin of this session only (the Options slider's value in memory; nothing is saved).
        Game.UI.UiKit.SafeMarginPercent = float.Parse(word.Split(':')[1], inv);
        sb.Append("margin ").Append(Game.UI.UiKit.SafeMarginPercent).Append("; ");
    }
    else if (word == "dump")
    {
        var area = (UnityEngine.RectTransform)typeof(Game.UI.SurprisePingView).GetField("_area", flags).GetValue(view);
        var keepOut = (Game.UI.HudKeepOut)typeof(Game.UI.SurprisePingView).GetField("_keepOut", flags).GetValue(view);
        var canvas = area.GetComponentInParent<UnityEngine.Canvas>().rootCanvas;
        sb.Append("screen ").Append(UnityEngine.Screen.width).Append('x').Append(UnityEngine.Screen.height)
          .Append(" camera ").Append(camera.pixelWidth).Append('x').Append(camera.pixelHeight)
          .Append(" canvas ").Append(canvas.renderMode).Append(" scale ").Append(canvas.scaleFactor.ToString("0.000", inv))
          .Append(" area ").Append(area.rect.ToString("0")).Append(" showing ").Append(view.Showing).Append(" | zones:");
        foreach (UnityEngine.Rect zone in keepOut.Zones) { sb.Append(' ').Append(zone.ToString("0")); }
        sb.Append(" | labels:");
        var corners = new UnityEngine.Vector3[4];
        foreach (UnityEngine.Transform child in area)
        {
            if (!child.gameObject.activeSelf) { continue; }
            var rect = (UnityEngine.RectTransform)child;
            rect.GetWorldCorners(corners);
            UnityEngine.Vector3 min = area.InverseTransformPoint(corners[0]), max = area.InverseTransformPoint(corners[2]);
            sb.Append(' ').Append(child.name).Append('[').Append(min.x.ToString("0", inv)).Append(',').Append(min.y.ToString("0", inv)).Append(" .. ").Append(max.x.ToString("0", inv)).Append(',').Append(max.y.ToString("0", inv)).Append(']');
        }
    }
}
return sb.ToString();
