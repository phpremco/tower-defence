// Import stage 4a: triangles and instances the environment instancer draws, per mesh (all batches, in view or not).
var instancer = UnityEngine.Object.FindAnyObjectByType<Game.Levels.EnvironmentInstancer>();
if (instancer == null) return "no instancer";
var field = typeof(Game.Levels.EnvironmentInstancer).GetField("_batches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var batches = (System.Collections.IList)field.GetValue(instancer);
var perMesh = new System.Collections.Generic.Dictionary<string, (int Count, long Tris, int Shadow)>();
long total = 0;
foreach (object batch in batches)
{
    var type = batch.GetType();
    var mesh = (UnityEngine.Mesh)type.GetField("Mesh").GetValue(batch);
    var matrices = (UnityEngine.Matrix4x4[])type.GetField("Matrices").GetValue(batch);
    bool shadows = (bool)type.GetField("Shadows").GetValue(batch);
    long tris = mesh.GetIndexCount(0) / 3 * (long)matrices.Length;
    perMesh.TryGetValue(mesh.name, out var row);
    perMesh[mesh.name] = (row.Count + matrices.Length, row.Tris + tris, row.Shadow + (shadows ? matrices.Length : 0));
    total += tris;
}
var sb = new System.Text.StringBuilder();
sb.Append("instances ").Append(instancer.Count).Append(" batches ").Append(instancer.BatchCount).Append(" tris ").Append(total / 1000).Append("k | ");
var rows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, (int Count, long Tris, int Shadow)>>(perMesh);
rows.Sort((a, b) => b.Value.Tris.CompareTo(a.Value.Tris));
foreach (var row in rows) sb.Append(row.Key).Append(' ').Append(row.Value.Count).Append("x ").Append(row.Value.Tris / 1000).Append("k sh").Append(row.Value.Shadow).Append(" | ");
return sb.ToString();
