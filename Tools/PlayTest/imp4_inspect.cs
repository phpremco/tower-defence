// Import stage 4a: the environment set's meshes (bounds, submeshes, vertex colours) and decor scales, to check the axes
// and the flora sway colours after import.
var set = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.EnvironmentSet>("Assets/_Project/Data/Art/Environment/CrystalPlains.asset");
if (set == null) return "no set";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var sb = new System.Text.StringBuilder();
System.Action<string, UnityEngine.Mesh> describe = (label, mesh) =>
{
    if (mesh == null) { sb.Append(label).Append(": none\n"); return; }
    var b = mesh.bounds;
    sb.Append(label).Append(' ').Append(mesh.name).Append(" v").Append(mesh.vertexCount).Append(" sub").Append(mesh.subMeshCount)
      .Append(" c").Append(b.center.ToString("0.00")).Append(" s").Append(b.size.ToString("0.00"));
    if (mesh.isReadable)
    {
        var colours = mesh.colors;
        sb.Append(" colours ").Append(colours.Length);
        if (colours.Length > 0)
        {
            float minR = 1, maxR = 0, minG = 1, maxG = 0, minB = 1, maxB = 0;
            foreach (var c in colours) { minR = System.Math.Min(minR, c.r); maxR = System.Math.Max(maxR, c.r); minG = System.Math.Min(minG, c.g); maxG = System.Math.Max(maxG, c.g); minB = System.Math.Min(minB, c.b); maxB = System.Math.Max(maxB, c.b); }
            sb.Append(" R").Append(minR.ToString("0.00", inv)).Append("-").Append(maxR.ToString("0.00", inv)).Append(" G").Append(minG.ToString("0.00", inv)).Append("-").Append(maxG.ToString("0.00", inv)).Append(" B").Append(minB.ToString("0.00", inv)).Append("-").Append(maxB.ToString("0.00", inv));
            // Does R follow the height? correlation of R with y.
            var v = mesh.vertices; double sy = 0, sr = 0, syy = 0, srr = 0, syr = 0; int n = v.Length;
            for (int i = 0; i < n; i++) { sy += v[i].y; sr += colours[i].r; syy += v[i].y * v[i].y; srr += colours[i].r * colours[i].r; syr += v[i].y * colours[i].r; }
            double cov = syr / n - sy / n * sr / n; double vy = syy / n - sy * sy / n / n; double vr = srr / n - sr * sr / n / n;
            sb.Append(" corr(R,y) ").Append((cov / System.Math.Sqrt(System.Math.Max(1e-9, vy * vr))).ToString("0.00", inv));
        }
    }
    sb.Append('\n');
};
var so = new UnityEditor.SerializedObject(set);
var pieces = so.FindProperty("_pieces");
for (int i = 0; i < pieces.arraySize; i++)
{
    var p = (Game.Levels.EnvironmentPiece)pieces.GetArrayElementAtIndex(i).boxedValue;
    describe(p.Kind.ToString(), p.Model.Mesh);
}
var decor = so.FindProperty("_decor");
for (int i = 0; i < decor.arraySize; i++)
{
    var d = (Game.Levels.EnvironmentDecor)decor.GetArrayElementAtIndex(i).boxedValue;
    sb.Append("DECOR ").Append(d.Kind).Append(" scale ").Append(d.Scale.ToString("0.00", inv)).Append('\n');
    foreach (var m in d.Variants) describe("  ", m.Mesh);
}
foreach (var m in set.Scatter) describe("scatter", m.Mesh);
foreach (var m in set.Silhouettes) describe("silhouette", m.Mesh);
var origins = so.FindProperty("_origins");
for (int i = 0; i < origins.arraySize; i++)
{
    var o = (Game.Levels.EnvironmentOrigin)origins.GetArrayElementAtIndex(i).boxedValue;
    describe("origin " + o.Terrain, o.Model.Mesh);
}
sb.Append("ground tile ").Append(set.Ground.TileSize).Append(" road ").Append(set.Road.TileSize).Append(" plaza ").Append(set.Plaza.TileSize).Append(" shader ").Append(set.GroundShader != null);
return sb.ToString();
