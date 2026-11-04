// Night 2: the base timelapse right now: every module (active, height above its rest, scale share, drop time, drone) and
// every builder (position, height).
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sets = (System.Collections.IDictionary)typeof(Game.UI.CutscenePlayer).GetField("_sets", flags).GetValue(player);
var field = sets[Game.Levels.CutsceneSet.Field];
var fbase = field.GetType().GetField("_base", flags).GetValue(field);
var modules = (System.Collections.IList)fbase.GetType().GetField("_modules", flags).GetValue(fbase);
var sb = new System.Text.StringBuilder(player.Sequencer.Current.Action + " p=" + player.Sequencer.Progress.ToString("0.00") + " | ");
foreach (var m in modules)
{
    var t = m.GetType();
    var root = (UnityEngine.Transform)t.GetField("Root").GetValue(m);
    var rest = (UnityEngine.Vector3)t.GetField("RestPosition").GetValue(m);
    var restScale = (UnityEngine.Vector3)t.GetField("RestScale").GetValue(m);
    sb.Append(root.name + (root.gameObject.activeSelf ? "" : "(off)") + " dy " + (root.position.y - rest.y).ToString("0.0") + " s " + (root.localScale.x / UnityEngine.Mathf.Max(0.0001f, restScale.x)).ToString("0.00") + " at " + ((float)t.GetField("Start").GetValue(m)).ToString("0.00") + " pick " + ((float)t.GetField("Pick").GetValue(m)).ToString("0.00") + " d" + t.GetField("Drone").GetValue(m) + " | ");
}
var builders = (System.Collections.IList)fbase.GetType().GetField("_builders", flags).GetValue(fbase);
foreach (var b in builders) { var d = (Game.UI.CutsceneDrone)b.GetType().GetField("Drone").GetValue(b); sb.Append("B " + d.Holder.position.ToString("0.0") + (d.Holder.gameObject.activeSelf ? "" : "off") + " "); }
return sb.ToString();
