// Night 2: the underground art as imported: per eye the vertex count, colour channels, sub-meshes, where the cornea sits
// against the eyeball after the node matrix; per cave piece the bounds in its own frame and where the glow sits.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.UndergroundArt>("Assets/_Project/Data/Art/Story/UndergroundArt.asset");
var sb = new System.Text.StringBuilder();
foreach (var eye in art.Eyes)
{
    var m = eye.Mesh; var v = m.vertices; var c = m.colors; var node = eye.Node;
    sb.Append(eye.Name + " verts " + v.Length + " colours " + c.Length + " subs " + m.subMeshCount + " node " + node.rotation.eulerAngles.ToString("0") + " s " + node.lossyScale.ToString("0.00"));
    UnityEngine.Vector3 cornea = UnityEngine.Vector3.zero; int n = 0; var tri = m.GetTriangles(1);
    foreach (int i in tri) { cornea += node.MultiplyPoint3x4(v[i]); n++; }
    UnityEngine.Vector3 lid = UnityEngine.Vector3.zero; int ln = 0; UnityEngine.Vector3 ball = UnityEngine.Vector3.zero; int bn = 0;
    for (int i = 0; i < v.Length; i++) { if (c.Length > 0 && c[i].r > 0.5f) { lid += node.MultiplyPoint3x4(v[i]); ln++; } if (c.Length > 0 && c[i].b > 0.5f) { ball += node.MultiplyPoint3x4(v[i]); bn++; } }
    var b = new UnityEngine.Bounds(); foreach (var p in v) b.Encapsulate(node.MultiplyPoint3x4(p));
    sb.Append(" cornea mean " + (n > 0 ? (cornea / n).ToString("0.000") : "-") + " upper lid mean " + (ln > 0 ? (lid / ln).ToString("0.000") : "-") + " (" + ln + ") ball " + (bn > 0 ? (ball / bn).ToString("0.000") : "-") + " (" + bn + ") bounds " + b.ToString("0.00") + " axis " + eye.Axis.ToString("0.00") + " deg " + eye.UpperLidDegrees + "/" + eye.LowerLidDegrees + "/" + eye.LookDegrees + " | ");
}
foreach (Game.Levels.CavePiece piece in System.Enum.GetValues(typeof(Game.Levels.CavePiece)))
{
    var p = art.Piece(piece); if (p == null) { sb.Append(piece + " MISSING | "); continue; }
    var v = p.Mesh.vertices; var b = new UnityEngine.Bounds(p.Node.MultiplyPoint3x4(v[0]), UnityEngine.Vector3.zero); foreach (var q in v) b.Encapsulate(p.Node.MultiplyPoint3x4(q));
    sb.Append(piece + " " + b.ToString("0.00") + " tris " + p.Mesh.triangles.Length / 3 + " | ");
}
return sb.ToString();
