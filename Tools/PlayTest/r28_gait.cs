// Round 28: foot slide of a walking creature in play. Finds the first living enemy named in Tools/PlayTest/enemy_arg.txt,
// takes the leaf leg bones of its near skinned mesh (Leg_*), finds each foot tip from the mesh (the farthest vertex along
// the bone's own Y among the vertices that bone carries), then steps 40 single frames and sums, per foot, how far a
// planted tip moves over the ground against how far the body moved in the same frames. Appends one line to
// Tools/PlayTest/r28_gait.txt. Play mode, waves off; run it a few times for more steps.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
Game.Enemies.Enemy target = null;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == wanted) { target = enemy; break; }
}

if (target == null) return "no living " + wanted;
UnityEngine.SkinnedMeshRenderer skin = null;
foreach (var s in target.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
{
    if (!s.name.Contains("_Far") && (skin == null || s.sharedMesh.vertexCount > skin.sharedMesh.vertexCount)) skin = s;
}

if (skin == null) return "no skinned mesh";
// The clip player culls rigs that are not drawn (CullCompletely); a CLI-stepped, unfocused editor draws nothing, so the
// probe forces this creature's rigs to evaluate and steps a few frames to let the pose settle.
foreach (var animator in target.GetComponentsInChildren<UnityEngine.Animator>(true)) animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
UnityEditor.EditorApplication.isPaused = true;
for (int w = 0; w < 3; w++) UnityEditor.EditorApplication.Step();
var bones = skin.bones;
var mesh = skin.sharedMesh;
var weights = mesh.boneWeights;
var verts = mesh.vertices;
var binds = mesh.bindposes;
var feet = new System.Collections.Generic.List<int>();
for (int b = 0; b < bones.Length; b++)
{
    if (bones[b] == null || !bones[b].name.StartsWith("Leg_")) continue;
    bool leaf = true;
    foreach (UnityEngine.Transform child in bones[b]) if (child.name.StartsWith("Leg_")) leaf = false;
    if (leaf) feet.Add(b);
}

var tips = new UnityEngine.Vector3[feet.Count];
for (int f = 0; f < feet.Count; f++)
{
    int b = feet[f];
    float best = -1f;
    for (int v = 0; v < verts.Length; v++)
    {
        var w = weights[v];
        bool carried = (w.boneIndex0 == b && w.weight0 > 0.5f);
        if (!carried) continue;
        UnityEngine.Vector3 local = binds[b].MultiplyPoint3x4(verts[v]);
        if (local.y > best) { best = local.y; tips[f] = local; }
    }
}

const int Frames = 40;
var pos = new UnityEngine.Vector3[Frames + 1, feet.Count];
var body = new UnityEngine.Vector3[Frames + 1];
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i <= Frames; i++)
{
    if (i > 0) UnityEditor.EditorApplication.Step();
    body[i] = target.Position;
    for (int f = 0; f < feet.Count; f++) pos[i, f] = bones[feet[f]].TransformPoint(tips[f]);
}

// Planted: within 0.8 cm of the lowest height that foot reached in the sample. Besides the sums, the median of the per-frame
// ratio (foot ground motion over body ground motion) is robust to touchdown and lift-off frames and to turns.
float slide = 0f, travel = 0f, lifted = 0f;
var ratios = new System.Collections.Generic.List<float>();
int planted = 0;
for (int f = 0; f < feet.Count; f++)
{
    float low = float.MaxValue;
    for (int i = 0; i <= Frames; i++) low = UnityEngine.Mathf.Min(low, pos[i, f].y);
    for (int i = 1; i <= Frames; i++)
    {
        if (pos[i, f].y > low + 0.008f || pos[i - 1, f].y > low + 0.008f) { lifted++; continue; }
        planted++;
        var d = pos[i, f] - pos[i - 1, f]; d.y = 0f;
        var m = body[i] - body[i - 1]; m.y = 0f;
        slide += d.magnitude;
        travel += m.magnitude;
        if (m.magnitude > 0.001f) ratios.Add(d.magnitude / m.magnitude);
    }
}

ratios.Sort();
float median = ratios.Count > 0 ? ratios[ratios.Count / 2] : -1f;
var bodyMove = body[Frames] - body[0]; bodyMove.y = 0f;
string line = wanted + " feet " + feet.Count + " frames " + Frames + " body " + bodyMove.magnitude.ToString("0.000") + " m, planted foot-frames " + planted + " lifted " + lifted
    + ", planted slide " + slide.ToString("0.0000") + " m vs body " + travel.ToString("0.0000") + " m, slip share " + (travel > 0f ? slide / travel : 0f).ToString("0.000") + ", median ratio " + median.ToString("0.000") + " of " + ratios.Count
    + ", dt " + UnityEngine.Time.deltaTime.ToString("0.0000") + ", speed " + target.Definition.Speed.ToString("0.00") + " frame " + UnityEngine.Time.frameCount;
System.IO.File.AppendAllText("Tools/PlayTest/r28_gait.txt", line + "\n");
return line;
