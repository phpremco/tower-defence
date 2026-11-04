int n = 0;
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("imp1_", new[] { "Assets/Temp" }))
{
    if (UnityEditor.AssetDatabase.DeleteAsset(UnityEditor.AssetDatabase.GUIDToAssetPath(guid))) n++;
}
return "deleted " + n;
