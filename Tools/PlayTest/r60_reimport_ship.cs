// Round 60: reimports the mothership's two FBX files so the importer's new readability rule (BlenderModelImporter.IsMothership) applies.
foreach (var path in new[] { "Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership.fbx", "Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership_LOD1.fbx" })
{
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
}
var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership.fbx").GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>(true).sharedMesh;
var far = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership_LOD1.fbx").GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>(true).sharedMesh;
return "readable near " + mesh.isReadable + " far " + far.isReadable;
