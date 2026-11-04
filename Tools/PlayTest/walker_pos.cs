// Prints the position and facing yaw of every live menu creature (for menu_zoom.cs).
var sb = new System.Text.StringBuilder();
foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
{
    if (!e.IsAlive) continue;
    var p = e.transform.position;
    sb.Append(e.name + " " + p.x.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " " + p.y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " " + p.z.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " heading " + e.Heading.ToString("F2") + "; ");
}
return sb.ToString();
