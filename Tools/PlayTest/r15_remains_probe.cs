// Round 15: the remains meshes of every environment set (plan span after their node, height) and their material colours.
string report = "";
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:EnvironmentSet"))
{
    var set = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.EnvironmentSet>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    report += set.name + ":";
    foreach (Game.Levels.DecorRemains kind in new[] { Game.Levels.DecorRemains.Crystal, Game.Levels.DecorRemains.Flora, Game.Levels.DecorRemains.Stone })
    {
        var model = set.Remains(kind);
        if (model == null) { report += " " + kind + "=none"; continue; }
        var b = model.Mesh.bounds;
        var n = model.Node;
        var ex = n.MultiplyVector(b.extents);
        var m = model.MaterialFor(false);
        report += " " + kind + " ext " + new UnityEngine.Vector3(UnityEngine.Mathf.Abs(ex.x), UnityEngine.Mathf.Abs(ex.y), UnityEngine.Mathf.Abs(ex.z)).ToString("0.00") + " mat " + m.name + " " + m.shader.name + (m.HasProperty("_EmissionColor") ? " em " + m.GetColor("_EmissionColor").ToString("0.00") : "") + " col " + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString("0.00") : "");
    }
    report += "\n";
}
return report;
