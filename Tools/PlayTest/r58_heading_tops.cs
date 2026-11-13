// Round 58: the Sealed One's lifting clips measured at every 15 degrees of camera heading (CreatureBarHeight.ClipBars) against
// what the view reads back (ModelClip.BarTopAt) at each heading and halfway between two (round 58 first wrote four headings and
// interpolated: up to 0.6 model units under the true top between them).
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/SealedOne.asset");
var sb = new System.Text.StringBuilder();
foreach (var clip in art.Clips)
{
    if (clip.Clip == null || !clip.LiftsBar) { continue; }
    float[] tops = Game.Editor.CreatureBarHeight.ClipBars(art.Model, clip.Clip, new[] { "Ring_1", "Ring_2", "Ring_3" });
    float worst = 0f;
    sb.Append(clip.Clip.name).Append(":");
    for (int h = 0; h < tops.Length; h++)
    {
        float heading = h * 360f / tops.Length;
        worst = UnityEngine.Mathf.Max(worst, UnityEngine.Mathf.Abs(tops[h] - clip.BarTopAt(heading)));
        sb.Append(" ").Append(heading.ToString("0")).Append("=").Append(tops[h].ToString("0.00"));
    }

    sb.Append(" | written vs measured max diff ").Append(worst.ToString("0.000")).Append("; ");
}

return sb.ToString();
