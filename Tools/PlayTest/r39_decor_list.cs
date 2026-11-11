// Round 39: the field's instanced pieces by mesh (name, material, count, mean height), to tell crystals from rocks.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
player.Seek(3, 0.5f);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var counts = new System.Collections.Generic.SortedDictionary<string, int>();
foreach (var inst in UnityEngine.Object.FindObjectsByType<Game.Levels.EnvironmentInstancer>(UnityEngine.FindObjectsSortMode.None))
{
    var batches = (System.Collections.IList)typeof(Game.Levels.EnvironmentInstancer).GetField("_batches", flags).GetValue(inst);
    foreach (var b in batches)
    {
        var t = b.GetType();
        var mesh = (UnityEngine.Mesh)t.GetField("Mesh").GetValue(b);
        var all = (UnityEngine.Matrix4x4[])t.GetField("All").GetValue(b);
        if (mesh == null || all == null) continue;
        string key = inst.transform.parent.name + "/" + inst.name + " " + mesh.name + " h" + mesh.bounds.size.y.ToString("0.0");
        counts[key] = (counts.TryGetValue(key, out int c) ? c : 0) + all.Length;
    }
}
var sb = new System.Text.StringBuilder();
foreach (var kv in counts) sb.Append(kv.Key).Append(" x").Append(kv.Value).Append('\n');
System.IO.File.WriteAllText("Tools/PlayTest/r39_decor_list.txt", sb.ToString());
return sb.Length > 3000 ? sb.ToString().Substring(0, 3000) : sb.ToString();
