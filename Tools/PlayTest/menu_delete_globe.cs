// Removes the old code-baked globe maps (round menu 2026-11-04); the generator's "Planet style" step does the same.
var removed = 0;
foreach (var name in new[] { "Globe_albedo", "Globe_normal", "Globe_gloss", "Globe_lights", "Globe_clouds_0", "Globe_clouds_1" })
{
    if (UnityEditor.AssetDatabase.DeleteAsset("Assets/_Project/Art/Planet/" + name + ".png")) { removed++; }
}
if (UnityEditor.AssetDatabase.FindAssets("", new[] { "Assets/_Project/Art/Planet" }).Length == 0) { UnityEditor.AssetDatabase.DeleteAsset("Assets/_Project/Art/Planet"); }
UnityEditor.EditorUtility.SetDirty(UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset"));
UnityEditor.AssetDatabase.SaveAssets();
return "removed " + removed;
