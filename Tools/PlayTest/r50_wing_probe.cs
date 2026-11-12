// Round 50: for the bats, the bat boss and the rays, every clip with its role, length, overlay weight and looping, and how far
// the wing bones swing within it (largest angle any wing bone turns from the clip's first frame, over 24 samples), against
// the body bone. A clip that holds the wings still while the cycle beats them reads as "the bat stops flying".
var rows = new[] { "Bat", "Bat_Moss", "BatBoss", "Skyray", "RayBoss" };
var sb = new System.Text.StringBuilder();
foreach (var id in rows)
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + id + ".asset");
    var instance = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(art.Model);
    var wings = new System.Collections.Generic.List<UnityEngine.Transform>();
    UnityEngine.Transform body = null;
    foreach (var t in instance.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (t.name.StartsWith("Arm_") || t.name.StartsWith("Hand_") || t.name.StartsWith("Fin_")) wings.Add(t);
        if (t.name == "Body") body = t;
    }

    sb.Append(id).Append(" [wings ").Append(wings.Count).Append(", upper '").Append(art.UpperBodyBone).Append("']: ");
    foreach (var clip in art.Clips)
    {
        if (clip.Clip == null) continue;
        float wingSwing = 0f, bodySwing = 0f;
        var start = new UnityEngine.Quaternion[wings.Count];
        UnityEngine.Quaternion bodyStart = UnityEngine.Quaternion.identity;
        for (int s = 0; s <= 24; s++)
        {
            clip.Clip.SampleAnimation(instance, clip.Clip.length * s / 24f);
            for (int w = 0; w < wings.Count; w++)
            {
                if (s == 0) start[w] = wings[w].localRotation;
                wingSwing = UnityEngine.Mathf.Max(wingSwing, UnityEngine.Quaternion.Angle(start[w], wings[w].localRotation));
            }

            if (body != null)
            {
                if (s == 0) bodyStart = body.localRotation;
                bodySwing = UnityEngine.Mathf.Max(bodySwing, UnityEngine.Quaternion.Angle(bodyStart, body.localRotation));
            }
        }

        sb.Append(clip.Clip.name).Append("/").Append(clip.Role).Append(" ").Append(clip.Length.ToString("0.00")).Append("s w").Append(clip.Weight.ToString("0.0"))
          .Append(clip.Clip.isLooping ? " loop" : " once").Append(" ev").Append(clip.EventSeconds.ToString("0.00"))
          .Append(" wing").Append(wingSwing.ToString("0")).Append(" body").Append(bodySwing.ToString("0")).Append("; ");
    }

    UnityEngine.Object.DestroyImmediate(instance);
    sb.Append(" || ");
}

return sb.ToString();
