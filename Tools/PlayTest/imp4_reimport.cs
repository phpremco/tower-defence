// Import stage 4a: reimports the environment kit FBX files (after an importer change that only touches them), so the
// rest of the Blender models keep their cached import. Play Mode must be stopped.
int count = 0;
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/Models/Blender/Env" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (path.Contains("/ResourceNode/")) continue;
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
    count++;
}
return "reimported " + count;
