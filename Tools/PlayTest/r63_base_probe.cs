// Round 63: is the base sunk into the ground? Seeks "shot progress" (cut_arg.txt), then reports the base command root's
// height, the lowest point of its meshes, and the drawn ground (FieldLayout.SurfaceAt) under the footprint's corners and
// centre. "sunk" = the deepest footprint point where the ground stands above the mesh's bottom (metres).
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
player.Seek(int.Parse(parts[0]), float.Parse(parts[1], inv));
UnityEditor.EditorApplication.Step();
player.Seek(int.Parse(parts[0]), float.Parse(parts[1], inv));
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var set = player.CurrentSet;
if (!(set is Game.UI.FieldSet)) return "not a field shot";
var layout = (Game.UI.FieldLayout)set.GetType().GetField("_layout", flags).GetValue(set);
var command = UnityEngine.GameObject.Find("Command");
if (command == null) return "no Command";
var sb = new System.Text.StringBuilder();
sb.Append("base layout y ").Append(layout.Base.y.ToString("0.000", inv)).Append(" command y ").Append(command.transform.position.y.ToString("0.000", inv));
var b = new UnityEngine.Bounds(command.transform.position, UnityEngine.Vector3.zero);
foreach (var r in command.GetComponentsInChildren<UnityEngine.Renderer>())
{
    if (!r.enabled || !r.gameObject.activeInHierarchy || r is UnityEngine.ParticleSystemRenderer) continue;
    b.Encapsulate(r.bounds);
}
sb.Append(" mesh min y ").Append(b.min.y.ToString("0.000", inv)).Append(" max y ").Append(b.max.y.ToString("0.000", inv)).Append(" size ").Append(b.size.ToString("0.00"));
float worst = -9f;
for (int ix = 0; ix <= 4; ix++)
for (int iz = 0; iz <= 4; iz++)
{
    float x = UnityEngine.Mathf.Lerp(b.min.x, b.max.x, ix / 4f), z = UnityEngine.Mathf.Lerp(b.min.z, b.max.z, iz / 4f);
    float g = layout.SurfaceAt(x, z);
    worst = UnityEngine.Mathf.Max(worst, g - b.min.y);
}
sb.Append(" ground-minus-bottom max ").Append(worst.ToString("0.000", inv));
// The drawn terrain itself: ray down onto any collider-free mesh is not possible, so also report the level's cell height.
sb.Append(" surface at centre ").Append(layout.SurfaceAt(layout.Base.x, layout.Base.z).ToString("0.000", inv));
return sb.ToString();
