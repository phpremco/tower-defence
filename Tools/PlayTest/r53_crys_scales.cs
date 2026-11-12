// Round 53: for the art row named in Tools/PlayTest/r53_art_arg.txt (default Shattered), samples each Move/Idle/Roar/Split/
// PhaseBreak clip at its start and middle on a copy of the model and reports the Crys_n / Shell_n / Core bone scales and the
// baked top, so the clip's phase-1 crystal scale can be compared with what play shows.
string id = System.IO.File.Exists("Tools/PlayTest/r53_art_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r53_art_arg.txt").Trim() : "Shattered";
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + id + ".asset");
if (art == null) { return "no art " + id; }
var probe = UnityEngine.Object.Instantiate(art.Model);
var sb = new System.Text.StringBuilder(id + " height " + art.Height.ToString("0.000") + " | bind: ");
var renderers = probe.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
System.Func<float> top = () =>
{
    float t = float.MinValue;
    var baked = new UnityEngine.Mesh();
    foreach (var r in renderers)
    {
        r.BakeMesh(baked, true);
        foreach (var v in baked.vertices) { t = UnityEngine.Mathf.Max(t, (r.transform.position + r.transform.rotation * v).y); }
    }
    UnityEngine.Object.DestroyImmediate(baked);
    return t;
};
System.Func<string> scales = () =>
{
    var s = new System.Text.StringBuilder();
    foreach (var b in probe.GetComponentsInChildren<UnityEngine.Transform>())
    {
        if (b.name.StartsWith("Crys_") || b.name.StartsWith("Core") || b.name == "Crest") { s.Append(b.name).Append('=').Append(b.localScale.x.ToString("0.00")).Append(' '); }
    }
    return s.ToString();
};
sb.Append("top ").Append(top().ToString("0.000")).Append(' ').Append(scales()).Append(" || ");
foreach (var clip in art.Clips)
{
    if (clip == null || clip.Clip == null) { continue; }
    foreach (float f in new[] { 0f, 0.5f, 0.99f })
    {
        clip.Clip.SampleAnimation(probe, clip.Clip.length * f);
        sb.Append(clip.Clip.name).Append('@').Append(f.ToString("0.0")).Append(" top ").Append(top().ToString("0.00")).Append(' ').Append(scales()).Append(" | ");
    }
}
UnityEngine.Object.DestroyImmediate(probe);
return sb.ToString();
