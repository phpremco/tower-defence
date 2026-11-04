// Reports Voss's imported blend shapes, clips and map sizes, and the mothership's map size and LOD1.
var sb = new System.Text.StringBuilder();
foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Models/Blender/Voss/Voss.fbx"))
{
    if (o is UnityEngine.Mesh mesh) { sb.Append("mesh " + mesh.name + " shapes " + mesh.blendShapeCount + ":"); for (int i = 0; i < mesh.blendShapeCount; i++) sb.Append(" " + mesh.GetBlendShapeName(i)); sb.Append("; "); }
    if (o is UnityEngine.AnimationClip clip && !clip.name.StartsWith("__preview__")) sb.Append("clip " + clip.name + " " + clip.length.ToString("0.00") + "; ");
}
foreach (var p in new[] { "Assets/_Project/Art/Models/Blender/Voss/Voss_Albedo.png", "Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership_Albedo.png", "Assets/_Project/Art/Models/Blender/Planet_S69/Planet_S69_Albedo.png" })
{
    var t = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(p);
    sb.Append(System.IO.Path.GetFileName(p) + " " + (t ? t.width + "x" + t.height + " " + t.format : "none") + "; ");
}
return sb.ToString();
