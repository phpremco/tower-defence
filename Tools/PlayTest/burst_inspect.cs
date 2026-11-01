// Lists every ImpactFlash in the scene: active state, position, screen position, age of its core and particle counts.
var sb = new System.Text.StringBuilder();
var cam = UnityEngine.Camera.main;
sb.Append("timeScale=" + UnityEngine.Time.timeScale + " ");
foreach (var flash in UnityEngine.Object.FindObjectsByType<Game.Combat.ImpactFlash>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (!flash.gameObject.activeSelf) continue;
    var t = flash.transform;
    var s = cam.WorldToScreenPoint(t.position);
    sb.Append(flash.name + " active=" + flash.gameObject.activeSelf + " at " + t.position.ToString("0.0") + " screen " + s.ToString("0") + " children=" + t.childCount);
    foreach (UnityEngine.Transform child in t)
    {
        var ps = child.GetComponent<UnityEngine.ParticleSystem>();
        sb.Append(" [" + child.name + " active=" + child.gameObject.activeSelf + " scale=" + child.localScale.x.ToString("0.00") + (ps ? " particles=" + ps.particleCount : "") + "]");
    }
    sb.Append("; ");
}
return sb.Length == 0 ? "no flashes" : sb.ToString();
