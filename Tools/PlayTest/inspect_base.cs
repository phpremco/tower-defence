// Reports the parts of the base structure: the turning part's mesh, materials and world bounds.
var sb = new System.Text.StringBuilder();
foreach (var spinner in UnityEngine.Object.FindObjectsByType<Game.Core.Spinner>())
{
    var renderer = spinner.GetComponent<UnityEngine.MeshRenderer>();
    var filter = spinner.GetComponent<UnityEngine.MeshFilter>();
    if (renderer == null || filter == null || filter.sharedMesh == null)
    {
        continue;
    }

    sb.Append(spinner.transform.parent.name).Append('/').Append(spinner.name).Append(": mesh ").Append(filter.sharedMesh.name);
    sb.Append(" verts ").Append(filter.sharedMesh.vertexCount).Append(" submeshes ").Append(filter.sharedMesh.subMeshCount);
    sb.Append(" bounds ").Append(renderer.bounds.center.ToString("0.00")).Append(" size ").Append(renderer.bounds.size.ToString("0.00"));
    sb.Append(" local ").Append(spinner.transform.localPosition.ToString("0.00"));
    foreach (var material in renderer.sharedMaterials)
    {
        sb.Append("\n   material ").Append(material.name).Append(" base ").Append(material.GetColor("_BaseColor").ToString("0.00"));
        sb.Append(" metallic ").Append(material.GetFloat("_Metallic")).Append(" smooth ").Append(material.GetFloat("_Smoothness"));
    }

    for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
    {
        sb.Append("\n   submesh ").Append(i).Append(" tris ").Append(filter.sharedMesh.GetSubMesh(i).indexCount / 3);
    }

    sb.Append('\n');
}

return sb.ToString();
