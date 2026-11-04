// Lists the root-level curve bindings (path "") and rotation curves of the drone art clips, to see whether a sampled clip overwrites the model root.
var sb = new System.Text.StringBuilder();
foreach (var path in new[] { "Assets/_Project/Data/Art/Story/Drone_Builder.asset", "Assets/_Project/Data/Art/Story/Drone_Hauler.asset", "Assets/_Project/Data/Art/Colony/Drone_Repair.asset", "Assets/_Project/Data/Art/Colony/Drone_Harvest.asset" })
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(path);
    if (art == null) { sb.Append(path + " missing; "); continue; }
    sb.Append(art.name + " yaw " + art.Yaw + " scale " + art.Scale + ": ");
    foreach (var clip in art.Clips)
    {
        var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip.Clip);
        int root = 0; var names = new System.Collections.Generic.HashSet<string>();
        foreach (var b in bindings) { if (b.path == "" ) { root++; names.Add(b.propertyName); } }
        var paths = new System.Collections.Generic.HashSet<string>();
        foreach (var b in bindings) { if (b.propertyName.Contains("Rotation")) paths.Add(b.path); }
        sb.Append(clip.Role + "(" + clip.Clip.name + " len " + clip.Length.ToString("0.0") + " root " + root + " [" + string.Join(",", names) + "] rotPaths [" + string.Join(",", paths) + "]) ");
    }
    sb.Append(" | ");
}
return sb.ToString();
