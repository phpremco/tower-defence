// Round 50: writes the catalog's beam sockets into each creature art row (only `_beamSockets`, no rebuild), so the ray boss's
// laser and the volleys leave from their bones. Lists the rows it changed and the first socket each rig has.
var sb = new System.Text.StringBuilder();
foreach (var entry in Game.Editor.BlenderArtCatalog.Creatures)
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/" + entry.Id + ".asset");
    if (art == null) continue;
    if (string.Join(",", art.BeamSockets) == string.Join(",", entry.BeamSockets)) continue;
    var so = new UnityEditor.SerializedObject(art);
    var list = so.FindProperty("_beamSockets");
    list.arraySize = entry.BeamSockets.Length;
    for (int i = 0; i < entry.BeamSockets.Length; i++) list.GetArrayElementAtIndex(i).stringValue = entry.BeamSockets[i];
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(art);
    sb.Append(entry.Id + " -> " + string.Join(",", entry.BeamSockets) + "; ");
}

UnityEditor.AssetDatabase.SaveAssets();
return sb.Length == 0 ? "nothing to change" : sb.ToString();
