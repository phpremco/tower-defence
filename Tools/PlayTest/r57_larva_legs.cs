// Round 57: the Fire Larva's Crawl, near and far clip, per frame the largest leg-bone turn and which bone, so the round-56
// gait's quick lifts can be read against the 60-degree flip guard.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/FireLarva.asset");
var clip = art.Clips[art.IndexOf(Game.Core.ModelClipRole.Move)];
var sb = new System.Text.StringBuilder("rate-free; ");
foreach (var pair in new[] { (art.Model, clip.Clip, "near"), (art.Lod1, clip.FarClip, "far") })
{
    var model = UnityEngine.Object.Instantiate(pair.Item1);
    var legs = new System.Collections.Generic.List<UnityEngine.Transform>();
    foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name.StartsWith("Leg_")) legs.Add(t);
    int frames = UnityEngine.Mathf.RoundToInt(pair.Item2.length * 30f);
    var prev = new UnityEngine.Quaternion[legs.Count];
    sb.Append(pair.Item3 + " " + frames + "f:");
    for (int f = 0; f <= frames + 1; f++)
    {
        pair.Item2.SampleAnimation(model, f > frames ? 1f / 30f : f / 30f);
        float max = 0f; string who = "";
        for (int i = 0; i < legs.Count; i++) { if (f > 0) { float a = UnityEngine.Quaternion.Angle(prev[i], legs[i].localRotation); if (a > max) { max = a; who = legs[i].name; } } prev[i] = legs[i].localRotation; }
        if (f > 0) sb.Append(" f" + f + " " + max.ToString("0") + who.Replace("Leg_", ""));
    }
    sb.Append("; ");
    UnityEngine.Object.DestroyImmediate(model);
}
return sb.ToString();
