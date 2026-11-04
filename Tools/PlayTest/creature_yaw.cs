// One-off (2026-11-04): turns every creature art in Data/Art (not its sub folders) half round, as the generator's creature
// step now writes it, without re-syncing the Blender creature exports.
int n = 0;
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ModelArtDefinition", new[] { "Assets/_Project/Data/Art" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (path.LastIndexOf("/") != "Assets/_Project/Data/Art".Length) continue;
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(path);
    var so = new UnityEditor.SerializedObject(art);
    so.FindProperty("_yaw").floatValue = 180f;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(art);
    n++;
}
UnityEditor.AssetDatabase.SaveAssets();
return "turned " + n;
