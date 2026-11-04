// Lists the bones, meshes (world rest pose) and clips of one synced FBX: imp_arg.txt holds "<Folder>/<File without .fbx>".
string asset = System.IO.File.ReadAllText("Tools/PlayTest/imp_arg.txt").Trim();
string folder = "Assets/_Project/Art/Models/Blender/" + System.IO.Path.GetDirectoryName(asset).Replace("\\", "/") + "/";
asset = System.IO.Path.GetFileName(asset);
var sb = new System.Text.StringBuilder();
foreach (string file in new[] { asset + ".fbx" })
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder + file);
    if (go == null) { sb.AppendLine(file + ": none"); continue; }
    sb.AppendLine("== " + file);
    foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        string path = UnityEditor.AnimationUtility.CalculateTransformPath(t, go.transform);
        var smr = t.GetComponent<UnityEngine.SkinnedMeshRenderer>();
        sb.Append(path).Append(" p=").Append(t.localPosition.ToString("F3")).Append(" w=").Append(t.position.ToString("F3")).Append(" up=").Append(t.up.ToString("F2")).Append(" r=").Append(t.localEulerAngles.ToString("F0")).Append(" s=").Append(t.localScale.ToString("F2"));
        if (smr != null) sb.Append(" SMR tris=").Append(smr.sharedMesh.triangles.Length / 3).Append(" bones=").Append(smr.bones.Length).Append(" root=").Append(smr.rootBone != null ? smr.rootBone.name : "-").Append(" bounds=").Append(smr.bounds.ToString());
        if (t.GetComponent<UnityEngine.Animator>() != null) sb.Append(" ANIMATOR avatar=").Append(t.GetComponent<UnityEngine.Animator>().avatar);
        sb.AppendLine();
    }
    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(folder + file))
    {
        var clip = o as UnityEngine.AnimationClip;
        if (clip != null && !clip.name.StartsWith("__preview")) sb.AppendLine("clip " + clip.name + " len=" + clip.length.ToString("F3") + " loop=" + clip.isLooping + " curves=" + UnityEditor.AnimationUtility.GetCurveBindings(clip).Length);
    }
}
System.IO.File.WriteAllText("Tools/PlayTest/imp2_inspect.txt", sb.ToString());
return "ok";
