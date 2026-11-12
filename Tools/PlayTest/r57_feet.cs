// Round 57: one line per call for the first living enemy named in Tools/PlayTest/enemy_arg.txt: the game time, its world
// position, and the world position of every foot tip of its near rig, as "t px pz | name x y z | ...". A foot tip is the
// baked vertex, of those the childless "Leg_" bone owns most, lying farthest from that bone's joint (of the rig being
// drawn: the visible skinned renderer, the one with the most bones if both are). r57_feet.py reads a run of these lines and measures how far a planted foot slides.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
var inv = System.Globalization.CultureInfo.InvariantCulture;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    UnityEngine.SkinnedMeshRenderer near = null;
    foreach (var r in enemy.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
    {
        // The rig being drawn (at the play zoom a small creature shows its far LOD; a culled rig does not animate), the larger first.
        bool better = near == null || (r.isVisible && !near.isVisible) || (r.isVisible == near.isVisible && r.bones.Length > near.bones.Length);
        if (better) { near = r; }
    }

    if (near == null) { return "no rig"; }
    var sb = new System.Text.StringBuilder();
    var p = enemy.transform.position;
    sb.Append(UnityEngine.Time.time.ToString("0.0000", inv)).Append(' ').Append(p.x.ToString("0.0000", inv)).Append(' ').Append(p.z.ToString("0.0000", inv));
    var baked = new UnityEngine.Mesh();
    near.BakeMesh(baked, true);
    var vertices = baked.vertices;
    var weights = near.sharedMesh.boneWeights;
    var toWorld = near.transform.localToWorldMatrix;
    var bones = near.bones;
    for (int b = 0; b < bones.Length; b++)
    {
        if (bones[b] == null || bones[b].childCount != 0 || !bones[b].name.StartsWith("Leg_")) { continue; }
        var joint = bones[b].position;
        float far = -1f;
        var tip = joint;
        for (int v = 0; v < vertices.Length && v < weights.Length; v++)
        {
            if (weights[v].boneIndex0 != b) { continue; }
            var w = toWorld.MultiplyPoint3x4(vertices[v]);
            float d = (w - joint).sqrMagnitude;
            if (d > far) { far = d; tip = w; }
        }

        sb.Append(" | ").Append(bones[b].name).Append(' ').Append(tip.x.ToString("0.0000", inv)).Append(' ').Append(tip.y.ToString("0.0000", inv)).Append(' ').Append(tip.z.ToString("0.0000", inv));
    }

    UnityEngine.Object.DestroyImmediate(baked);
    return sb.ToString() + " | rig " + near.name + " visible " + near.isVisible;
}

return "no living " + wanted;
