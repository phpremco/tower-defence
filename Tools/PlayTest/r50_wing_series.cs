// Round 50: the angle of one wing bone from its rest pose over each clip (24 samples) for the bat and the bat boss, to see
// whether a strike beats the wings or holds them.
var sb = new System.Text.StringBuilder();
foreach (var spec in new[] { "Bat:Hand_L", "BatBoss:Hand_L", "Skyray:Fin_L_A3", "RayBoss:Fin_L_A3" })
{
    var parts = spec.Split(':');
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + parts[0] + ".asset");
    var instance = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(art.Model);
    UnityEngine.Transform bone = null;
    foreach (var t in instance.GetComponentsInChildren<UnityEngine.Transform>(true)) if (t.name == parts[1]) bone = t;
    var rest = bone.position - bone.parent.position;
    sb.Append(spec).Append(": ");
    foreach (var clip in art.Clips)
    {
        if (clip.Clip == null || (clip.Role != Game.Core.ModelClipRole.Move && clip.Role != Game.Core.ModelClipRole.Attack && clip.Role != Game.Core.ModelClipRole.Power && clip.Role != Game.Core.ModelClipRole.Idle)) continue;
        sb.Append(clip.Clip.name).Append("[");
        for (int s = 0; s <= 24; s++)
        {
            clip.Clip.SampleAnimation(instance, clip.Clip.length * s / 24f);
            var root = instance.transform;
            sb.Append(root.InverseTransformPoint(bone.position).y.ToString("0.00")).Append(s < 24 ? " " : "");
        }
        sb.Append("] ");
    }
    UnityEngine.Object.DestroyImmediate(instance);
    sb.Append(" || ");
}
return sb.ToString();
