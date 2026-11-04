var sb = new System.Text.StringBuilder();
foreach (string folder in Game.Editor.BlenderMarkerCatalog.Folders)
{
    foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/_Project/Art/Models/Blender/" + folder }))
    {
        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
        sb.AppendLine("# " + path);
        foreach (UnityEngine.Transform t in go.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            var mf = t.GetComponent<UnityEngine.MeshFilter>();
            string mesh = "";
            if (mf != null && mf.sharedMesh != null)
            {
                var m = mf.sharedMesh;
                UnityEngine.Bounds wb = UnityEngine.GeometryUtility.CalculateBounds(m.vertices, t.localToWorldMatrix);
                mesh = $" mesh={m.name} v={m.vertexCount} uv2={(m.uv2 != null && m.uv2.Length > 0)} worldBounds c={wb.center:F3} s={wb.size:F3}";
                if (m.vertexCount <= 8) { foreach (var v in m.vertices) mesh += " " + t.localToWorldMatrix.MultiplyPoint3x4(v).ToString("F2"); }
            }
            sb.AppendLine($"  {t.name} pos={t.position:F3} rot={t.rotation.eulerAngles:F0} scale={t.lossyScale:F2}{mesh}");
        }
    }
}
System.IO.File.WriteAllText("Tools/PlayTest/marker_nodes.txt", sb.ToString());
return "ok";
