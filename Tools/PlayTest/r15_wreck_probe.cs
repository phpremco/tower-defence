// Round 15: builds every model art whose root carries its own turn (one-mesh exports) as the level does (ArtModel.Create)
// and reports its world bounds: a flat wreck must be wide in x and z and low in y.
string report = "";
foreach (var name in new[] { "Colony/Base_Wreck", "Colony/Base_Damage", "Colony/Gate_Wreck", "Gate_IceBlock", "RotOvergrowth" })
{
    var path = "Assets/_Project/Data/Art/" + name + ".asset";
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(path);
    if (art == null) { var found = UnityEditor.AssetDatabase.FindAssets(System.IO.Path.GetFileName(name) + " t:ModelArtDefinition"); if (found.Length > 0) { art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(found[0])); } }
    if (art == null) { report += name + " missing\n"; continue; }
    var holder = new UnityEngine.GameObject("R15Probe");
    var model = Game.Core.ArtModel.CreateStill(holder.transform, art);
    var b = model.Renderers[0].bounds;
    foreach (var r in model.Renderers) { b.Encapsulate(r.bounds); }
    report += art.name + " size " + b.size.ToString("0.00") + " min y " + b.min.y.ToString("0.00") + "\n";
    UnityEngine.Object.DestroyImmediate(holder);
}
return report;
