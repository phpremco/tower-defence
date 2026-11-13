// Round 58: per clip of the crest rows (SealedOne, Shattered), the top of its sampled poses as the play camera projects the
// crest (Game.Editor.CreatureBarHeight.ClipTops: per pose the four headings towards, across, away, across), against the
// art's bar height: max over all, max per heading, the 75% cover, and the clip's written BarTop.
var sb = new System.Text.StringBuilder();
foreach (var row in new[] { ("SealedOne", new[] { "Ring_1", "Ring_2", "Ring_3" }), ("Shattered", new[] { "Core_Air", "Core_Road", "Core_Land" }) })
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + row.Item1 + ".asset");
    sb.Append(row.Item1).Append(" bar ").Append(art.Height.ToString("0.000")).Append(" :: ");
    foreach (var clip in art.Clips)
    {
        if (clip.Clip == null) { continue; }
        var tops = Game.Editor.CreatureBarHeight.ClipTops(art.Model, clip.Clip, row.Item2, new[] { 0f, 90f, 180f, 270f });
        var perHeading = new float[4];
        for (int i = 0; i < tops.Count; i++) { perHeading[i % 4] = UnityEngine.Mathf.Max(perHeading[i % 4], tops[i]); }
        float max = 0f; foreach (float t in tops) { max = UnityEngine.Mathf.Max(max, t); }
        float cover = Game.Editor.CreatureBarHeight.Covered(new System.Collections.Generic.List<float>(tops));
        sb.Append(clip.Clip.name).Append("(").Append(clip.Role).Append(") max ").Append(max.ToString("0.00"))
          .Append(" h[").Append(string.Join(" ", System.Array.ConvertAll(perHeading, h => h.ToString("0.00")))).Append("] cover ").Append(cover.ToString("0.00"))
          .Append(" written ").Append(clip.BarTop.ToString("0.00")).Append("; ");
    }
    sb.Append(" || ");
}
return sb.ToString();
