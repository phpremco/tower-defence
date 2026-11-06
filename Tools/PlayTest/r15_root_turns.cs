// Round 15: lists every model art whose model's root carries a turn of its own (an FBX axis conversion on a one-mesh root).
string report = "";
foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:ModelArtDefinition"))
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (art == null || art.Model == null) { continue; }
    var rotation = art.Model.transform.localRotation;
    if (UnityEngine.Quaternion.Angle(rotation, UnityEngine.Quaternion.identity) > 0.5f)
    {
        report += art.name + " root " + rotation.eulerAngles.ToString("0") + " renderer on root " + (art.Model.GetComponent<UnityEngine.Renderer>() != null) + "\n";
    }
}
return report == "" ? "none" : report;
