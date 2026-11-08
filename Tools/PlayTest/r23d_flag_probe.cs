// Round 23d: where the NTI flag stands on the base display copy (edit mode): its renderers' bounds against the base's.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.BaseArtDefinition>("Assets/_Project/Data/Art/Colony/BaseArt.asset");
var holder = new UnityEngine.GameObject("FlagProbe");
var sb = new System.Text.StringBuilder();
try
{
    Game.Levels.BaseArtView.BuildDisplay(holder.transform, art);
    var all = new UnityEngine.Bounds(); bool first = true;
    foreach (var r in holder.GetComponentsInChildren<UnityEngine.Renderer>(true))
    {
        if (r.name.StartsWith("NTI") || r.transform.root != holder.transform && false) { }
        bool flag = false;
        for (var t = r.transform; t != null; t = t.parent) if (t.name.StartsWith("NTI_Flag")) flag = true;
        if (flag) sb.Append("flag ").Append(r.name).Append(" enabled ").Append(r.enabled).Append(" ").Append(r.bounds.center.ToString("0.00")).Append(" size ").Append(r.bounds.size.ToString("0.00")).Append("; ");
        else { if (first) { all = r.bounds; first = false; } else all.Encapsulate(r.bounds); }
    }
    sb.Append("base bounds ").Append(all.center.ToString("0.00")).Append(" size ").Append(all.size.ToString("0.00"));
}
finally { UnityEngine.Object.DestroyImmediate(holder); }
return sb.ToString();
