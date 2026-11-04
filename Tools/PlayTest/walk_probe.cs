// Menu walkers face where they go: first call stores every live creature's position, the next compares the movement
// since then with the facing of its body (EnemyView._body forward) and of its model root. Writes walk_probe.txt.
string store = "Tools/PlayTest/walk_probe_pos.txt";
var bodyField = typeof(Game.Enemies.EnemyView).GetField("_body", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var sb = new System.Text.StringBuilder();
var lines = System.IO.File.Exists(store) ? System.IO.File.ReadAllLines(store) : new string[0];
var old = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();
foreach (var l in lines) { var p = l.Split(';'); old[int.Parse(p[0])] = new UnityEngine.Vector3(float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture)); }
var now = new System.Text.StringBuilder();
foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
{
    if (!e.IsAlive) continue;
    var pos = e.transform.position;
    now.AppendLine(e.GetHashCode() + ";" + pos.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";" + pos.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";" + pos.z.ToString(System.Globalization.CultureInfo.InvariantCulture));
    if (!old.TryGetValue(e.GetHashCode(), out var before)) continue;
    var move = pos - before; move.y = 0f;
    if (move.magnitude < 0.05f) continue;
    var body = (UnityEngine.Transform)bodyField.GetValue(e.GetComponent<Game.Enemies.EnemyView>());
    var f = body.forward; f.y = 0f;
    string model = "";
    var anim = e.GetComponentInChildren<UnityEngine.Animator>();
    if (anim != null) { var mf = anim.transform.forward; mf.y = 0f; model = " modelDot=" + UnityEngine.Vector3.Dot(mf.normalized, move.normalized).ToString("F2") + " modelLocalYaw=" + anim.transform.localEulerAngles.y.ToString("F0"); }
    sb.Append(e.name + " bodyDot=" + UnityEngine.Vector3.Dot(f.normalized, move.normalized).ToString("F2") + model + "; ");
}
System.IO.File.WriteAllText(store, now.ToString());
System.IO.File.WriteAllText("Tools/PlayTest/walk_probe.txt", sb.ToString());
return sb.Length > 0 ? sb.ToString() : "stored " + lines.Length;
