// Round 53: what a boss's runtime bone looks (BoneScaleView holding bones after the clips) do to its bar height. One job per
// line of Tools/PlayTest/r53_held_top.txt: "<Art> <scale>=<bone or prefix*>,... [<scale>=...]". Reports the stored
// height, the clip measure, and the covered/max top of the Move and Idle cycles with those bones held at that scale.
var sb = new System.Text.StringBuilder();
foreach (var line in System.IO.File.ReadAllLines("Tools/PlayTest/r53_held_top.txt"))
{
    var f = line.Trim().Split(' ');
    if (f.Length < 2) { continue; }
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + f[0] + ".asset");
    if (art == null) { sb.Append(f[0]).Append(" missing; "); continue; }
        var probe = UnityEngine.Object.Instantiate(art.Model);
    var held = new System.Collections.Generic.List<UnityEngine.Transform>();
    var heldScale = new System.Collections.Generic.List<float>();
    foreach (var t in probe.GetComponentsInChildren<UnityEngine.Transform>())
    {
        for (int i = 1; i < f.Length; i++)
        {
            var g = f[i].Split('=');
            bool hit = false;
            foreach (var b in g[1].Split(',')) { if (t.name == b || (b.EndsWith("*") && t.name.StartsWith(b.TrimEnd('*')))) { hit = true; } }
            if (hit) { held.Add(t); heldScale.Add(float.Parse(g[0], System.Globalization.CultureInfo.InvariantCulture)); break; }
        }
    }
    var renderers = probe.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
    var baked = new UnityEngine.Mesh();
    var tops = new System.Collections.Generic.List<float>();
    foreach (var role in new[] { Game.Core.ModelClipRole.Move, Game.Core.ModelClipRole.Idle })
    {
        int index = art.IndexOf(role);
        if (index < 0) { continue; }
        var clip = art.Clips[index].Clip;
        for (int s = 0; s < 16; s++)
        {
            clip.SampleAnimation(probe, clip.length * s / 16f);
            for (int h = 0; h < held.Count; h++) { held[h].localScale = UnityEngine.Vector3.one * heldScale[h]; }
            float top = float.MinValue;
            foreach (var r in renderers)
            {
                r.BakeMesh(baked, true);
                foreach (var v in baked.vertices) { top = UnityEngine.Mathf.Max(top, (r.transform.position + r.transform.rotation * v).y); }
            }
            tops.Add(top - probe.transform.position.y);
        }
    }
    UnityEngine.Object.DestroyImmediate(baked);
    UnityEngine.Object.DestroyImmediate(probe);
    float measured = Game.Editor.CreatureBarHeight.Measure(art.Model, art);
    float max = float.MinValue; foreach (var t in tops) { max = UnityEngine.Mathf.Max(max, t); }
    sb.Append(f[0]).Append(" held ").Append(held.Count).Append(" bones (").Append(line.Trim()).Append(")").Append(": stored ").Append(art.Height.ToString("0.000"))
      .Append(" measure ").Append(measured.ToString("0.000")).Append(" heldCovered ").Append(Game.Editor.CreatureBarHeight.Covered(tops).ToString("0.000"))
      .Append(" heldMax ").Append(max.ToString("0.000")).Append("; ");
}
return sb.ToString();
