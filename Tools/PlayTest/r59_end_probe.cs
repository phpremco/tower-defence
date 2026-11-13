// Round 59: where each capsule and the lens are near the end of its fall (shot 3), and whether its landing site is behind the rim.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var inv = System.Globalization.CultureInfo.InvariantCulture;
var sb = new System.Text.StringBuilder();
float length = 11.5f;
var c = Game.UI.OrbitSet.PlanetCentre; float R = Game.UI.OrbitSet.PlanetRadius;
for (int i = 0; i < 5; i++)
{
    float release = 1.2f + 0.5f * i;
    float t = release + 6.48f;
    player.Seek(3, t / length);
    var set = player.CurrentSet;
    object drop = set.GetType().GetField("_capsules", flags).GetValue(set);
    var list = (System.Collections.IList)drop.GetType().GetField("_capsules", flags).GetValue(drop);
    var root = (UnityEngine.Transform)list[i].GetType().GetField("Root", flags).GetValue(list[i]);
    var target = (UnityEngine.Vector3)drop.GetType().GetMethod("TargetOf", new[] { typeof(int) }).Invoke(drop, new object[] { i });
    var cam = UnityEngine.Camera.main.transform.position;
    var n = (target - c).normalized; var m = (root.position - c).normalized;
    sb.Append("c").Append(i).Append(" t=").Append(t.ToString("0.00", inv)).Append(" cam ").Append(cam.ToString("0.00")).Append(" root ").Append(root.position.ToString("0.00")).Append(" r ").Append((root.position - c).magnitude.ToString("0.00", inv))
      .Append(" target ").Append(target.ToString("0.00")).Append(" n.(cam-c) site ").Append(UnityEngine.Vector3.Dot(n, cam - c).ToString("0.0", inv)).Append(" now ").Append(UnityEngine.Vector3.Dot(m, cam - c).ToString("0.0", inv)).Append(" | ");
}
return sb.ToString();
