// Round 60: finds the mesh islands (welded by position) of the mothership near the bay and prints centre and size in
// mesh space, to pick out the baked clamshell doors.
var sb = new System.Text.StringBuilder();
string path = System.IO.File.Exists("Tools/PlayTest/r60_island_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r60_island_arg.txt").Trim() : "Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership.fbx";
var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
var smr = go.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>(true);
var mesh = smr.sharedMesh;
var v = mesh.vertices; var tris = mesh.triangles;
int n = v.Length;
var parent = new int[n];
for (int i = 0; i < n; i++) parent[i] = i;
System.Func<int, int> find = null;
find = i => { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; };
System.Action<int, int> join = (a, b) => { a = find(a); b = find(b); if (a != b) parent[a] = b; };
var weld = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, int>();
for (int i = 0; i < n; i++)
{
    var key = new UnityEngine.Vector3Int(UnityEngine.Mathf.RoundToInt(v[i].x * 2000f), UnityEngine.Mathf.RoundToInt(v[i].y * 2000f), UnityEngine.Mathf.RoundToInt(v[i].z * 2000f));
    if (weld.TryGetValue(key, out int other)) join(i, other); else weld[key] = i;
}
for (int t = 0; t < tris.Length; t += 3) { join(tris[t], tris[t + 1]); join(tris[t], tris[t + 2]); }
var min = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();
var max = new System.Collections.Generic.Dictionary<int, UnityEngine.Vector3>();
var count = new System.Collections.Generic.Dictionary<int, int>();
for (int i = 0; i < n; i++)
{
    int r = find(i);
    if (!min.ContainsKey(r)) { min[r] = v[i]; max[r] = v[i]; count[r] = 0; }
    min[r] = UnityEngine.Vector3.Min(min[r], v[i]); max[r] = UnityEngine.Vector3.Max(max[r], v[i]); count[r]++;
}
sb.Append("islands ").Append(min.Count).Append("\n");
foreach (var r in min.Keys)
{
    var c = (min[r] + max[r]) * 0.5f; var s = max[r] - min[r];
    if (UnityEngine.Mathf.Abs(c.x) < 0.6f || UnityEngine.Mathf.Abs(c.x) > 1.4f) continue;
    if (s.x > 1.2f || s.y > 3.2f || s.z > 3.2f) continue;
    sb.Append(r).Append(" n=").Append(count[r]).Append(" c=").Append(c.ToString("F3")).Append(" s=").Append(s.ToString("F3")).Append("\n");
}
return sb.ToString();
