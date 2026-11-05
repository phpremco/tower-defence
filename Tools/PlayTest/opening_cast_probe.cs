// Lists every enemy definition: flies, speed, the imported art's clip roles (opening cutscene brood choice).
var sb = new System.Text.StringBuilder();
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Game.Enemies.EnemyDefinition"))
{
    var d = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    var so = new UnityEditor.SerializedObject(d);
    var art = so.FindProperty("_art");
    string roles = "";
    var a = art != null ? art.objectReferenceValue as Game.Core.ModelArtDefinition : null;
    if (a != null) foreach (var c in a.Clips) roles += c.Role + ",";
    sb.Append(d.name + " flies=" + d.Flies + " speed=" + d.Speed + " art=" + (a != null ? a.name : "none") + " [" + roles + "] | ");
}
return sb.ToString();
