var sb = new System.Text.StringBuilder();
string[] paths = { "UI_HealthBar/UI_HealthBar.fbx", "Marker_RoadCue/Marker_RoadCue.fbx", "Marker_HeroSpot/Marker_HeroSpot.fbx", "Decal_GlowPool/Decal_Quad.fbx", "Net_Strand/Net_Strand.fbx" };
foreach (string p in paths)
{
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Art/Models/Blender/" + p);
    foreach (var mf in go.GetComponentsInChildren<UnityEngine.MeshFilter>())
    {
        var m = mf.sharedMesh;
        var v = m.vertices; var uv = m.uv; var uv2 = m.uv2;
        sb.Append(m.name + ":");
        for (int i = 0; i < v.Length && i < 8; i++)
        {
            sb.Append(" " + mf.transform.localToWorldMatrix.MultiplyPoint3x4(v[i]).ToString("F2") + " uv" + uv[i].ToString("F2") + (uv2.Length > 0 ? " uv2" + uv2[i].ToString("F2") : ""));
        }
        sb.AppendLine(" normal0=" + mf.transform.localToWorldMatrix.MultiplyVector(m.normals[0]).ToString("F2"));
    }
}
System.IO.File.WriteAllText("Tools/PlayTest/marker_uvs.txt", sb.ToString());
return "ok";
