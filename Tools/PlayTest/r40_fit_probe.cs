// Round 40: Spencer's Capsule_Exit against the landing capsule's hull, in edit mode (a preview scene, nothing saved).
// r40_fit_arg.txt: "<capsule scale> <frame step> [last frame]". The capsule (its art, Open sampled at its end) stands at the
// origin at that scale; Spencer's root sits on its Exit socket with the capsule's facing, as FieldSet poses him. Every
// near mesh (body, face, visor) is baked per frame and every triangle edge that crosses the hull's surface is tested: a
// crossing in the doorway (the front face, |x| <= 0.2, 0.44..1.23 m up at scale 1) is him coming out of the door; any
// other crossing is a clip through the hull. The hull is the export's own lathe profile (Tools/Blender/build_cutscene.py).
string[] arg = System.IO.File.ReadAllText("Tools/PlayTest/r40_fit_arg.txt").Trim().Split(' ');
float s = float.Parse(arg[0], System.Globalization.CultureInfo.InvariantCulture);
int stepF = int.Parse(arg[1]);
int lastF = arg.Length > 2 ? int.Parse(arg[2]) : 90;
var heroArt = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroArtDefinition>("Assets/_Project/Data/Art/Story/MercerArt.asset");
var capArt = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/Story/Cutscene_Capsule.asset");
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var sb = new System.Text.StringBuilder();
try
{
    var holder = new UnityEngine.GameObject("CapsuleHolder");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, scene);
    holder.transform.localScale = UnityEngine.Vector3.one * s;
    var cap = Game.Core.ArtModel.CreateSampled(holder.transform, capArt, false);
    cap.Sample(Game.Core.ModelClipRole.Open, cap.LengthOf(Game.Core.ModelClipRole.Open), false);
    var exit = cap.Find("Exit");
    var ramp = cap.Find("Ramp");
    UnityEngine.Vector3 exitLocal = holder.transform.InverseTransformPoint(exit.position);
    sb.Append("scale ").Append(s).Append(" exit local ").Append(exitLocal.ToString("F3")).Append(" ramp hinge ").Append(holder.transform.InverseTransformPoint(ramp.position).ToString("F3")).Append('\n');
    // Unity +z is the capsule's front when the exit lies there.
    float front = UnityEngine.Mathf.Sign(exitLocal.z);
    var heroHolder = new UnityEngine.GameObject("HeroHolder");
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(heroHolder, scene);
    heroHolder.transform.SetPositionAndRotation(exit.position, UnityEngine.Quaternion.LookRotation(holder.transform.forward * front));
    var hero = Game.Core.ArtModel.CreateSampled(heroHolder.transform, heroArt.Body, false);
    float cos30 = UnityEngine.Mathf.Cos(30f * UnityEngine.Mathf.Deg2Rad);
    System.Func<float[,], float, float> lerpProf = (prof, h) =>
    {
        for (int i = 0; i + 1 < prof.GetLength(0); i++)
            if (h >= prof[i, 1] && h <= prof[i + 1, 1]) { float t = (h - prof[i, 1]) / UnityEngine.Mathf.Max(1e-6f, prof[i + 1, 1] - prof[i, 1]); return UnityEngine.Mathf.Lerp(prof[i, 0], prof[i + 1, 0], t); }
        return -1f;
    };
    var shield = new float[,] { { 0f, 0.12f }, { 0.3f, 0.135f }, { 0.52f, 0.18f }, { 0.66f, 0.26f }, { 0.71f, 0.31f }, { 0.69f, 0.35f } };
    var waist = new float[,] { { 0.67f, 0.35f }, { 0.69f, 0.39f }, { 0.665f, 0.44f } };
    var hull = new float[,] { { 0.64f, 0.44f }, { 0.6f, 0.62f }, { 0.47f, 1.26f } };
    var shoulder = new float[,] { { 0.48f, 1.26f }, { 0.36f, 1.42f } };
    var capTop = new float[,] { { 0.31f, 1.42f }, { 0.25f, 1.52f } };
    // Inside the hull, in scale-1 capsule space with z towards the exit.
    System.Func<UnityEngine.Vector3, bool> inside = p =>
    {
        float h = p.y; float x = p.x; float z = p.z * front;
        float radial = UnityEngine.Mathf.Sqrt(x * x + z * z);
        if (h < 0.12f || h > 1.6f) return false;
        if (h <= 0.35f) return radial <= lerpProf(shield, h);
        if (h > 1.5f) return radial <= 0.16f;
        if (h > 1.42f) return radial <= lerpProf(capTop, h) * 0.966f;
        float r = h <= 0.44f ? lerpProf(waist, h) : h <= 1.26f ? lerpProf(hull, h) : lerpProf(shoulder, h);
        float apothem = r * cos30;
        for (int k = 0; k < 6; k++)
        {
            float a = k * 60f * UnityEngine.Mathf.Deg2Rad;
            if (x * UnityEngine.Mathf.Sin(a) + z * UnityEngine.Mathf.Cos(a) > apothem) return false;
        }
        return true;
    };
    System.Func<UnityEngine.Vector3, bool> inDoor = c => c.z * front > 0.3f && UnityEngine.Mathf.Abs(c.x) <= 0.22f && c.y >= 0.44f && c.y <= 1.23f;
    var renderers = new System.Collections.Generic.List<UnityEngine.SkinnedMeshRenderer>();
    foreach (var r in hero.Renderers) if (r is UnityEngine.SkinnedMeshRenderer sk && !r.name.EndsWith("_Far")) renderers.Add(sk);
    var baked = new UnityEngine.Mesh();
    int total = 0, framesHit = 0, totalSunk = 0;
    for (int f = 0; f <= lastF; f += stepF)
    {
        hero.Sample(Game.Core.ModelClipRole.CapsuleExit, f / 30f, false);
        int hits = 0, insideVerts = 0, sunk = 0; float sinkMax = 0f, sinkX = 0f, sinkZ = 0f; float maxX = 0f, maxH = 0f, deep = 0f;
        var where = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var sk in renderers)
        {
            sk.BakeMesh(baked, true);
            var v = baked.vertices; var tris = baked.triangles;
            var local = new UnityEngine.Vector3[v.Length]; var ins = new bool[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                local[i] = holder.transform.InverseTransformPoint(sk.transform.TransformPoint(v[i]));
                ins[i] = inside(local[i]);
                // The open ramp (scale 1: from the hinge 0.46 m up at z 0.58 down to the ground at z 1.2): a sole more than 3 cm under it sinks.
                var q = local[i]; float zq = q.z * front;
                if (zq > 0.58f && zq < 1.2f && UnityEngine.Mathf.Abs(q.x) < 0.21f) { float rh = 0.46f * (1.2f - zq) / 0.62f; if (q.y < rh - 0.03f / s && q.y > rh - 0.3f) { sunk++; sinkMax = UnityEngine.Mathf.Max(sinkMax, (rh - q.y) * s); sinkX = q.x; sinkZ = zq; } }
                if (ins[i]) { insideVerts++; maxX = UnityEngine.Mathf.Max(maxX, UnityEngine.Mathf.Abs(local[i].x)); maxH = UnityEngine.Mathf.Max(maxH, local[i].y); deep = UnityEngine.Mathf.Max(deep, 0.52f - local[i].z * front); }
            }
            var seen = new System.Collections.Generic.HashSet<long>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int a = tris[t + e], b = tris[t + (e + 1) % 3];
                    if (ins[a] == ins[b]) continue;
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (!seen.Add(key)) continue;
                    UnityEngine.Vector3 pi = ins[a] ? local[a] : local[b], po = ins[a] ? local[b] : local[a];
                    for (int it = 0; it < 10; it++) { var m = (pi + po) * 0.5f; if (inside(m)) pi = m; else po = m; }
                    var c = (pi + po) * 0.5f;
                    if (inDoor(c)) continue;
                    hits++;
                    string zone = (c.y > 1.26f ? "top" : c.y < 0.44f ? "low" : "side") + (c.z * front > 0.3f ? "-front" : c.z * front < -0.3f ? "-back" : "-flank") + "/" + sk.name;
                    where[zone] = where.TryGetValue(zone, out int n) ? n + 1 : 1;
                    if (hits <= 4) where["at(" + c.x.ToString("0.000") + "," + c.y.ToString("0.000") + "," + (c.z * front).ToString("0.000") + ")"] = 1;
                }
            }
        }
        total += hits; if (hits > 0) framesHit++; totalSunk += sunk;
        sb.Append("f").Append(f).Append(" hits ").Append(hits).Append(" sunkInRamp ").Append(sunk).Append(" maxSinkM ").Append(sinkMax.ToString("0.000")).Append(" atX ").Append(sinkX.ToString("0.00")).Append(" atZ ").Append(sinkZ.ToString("0.00")).Append(" inside ").Append(insideVerts).Append(" maxAbsX ").Append(maxX.ToString("0.00")).Append(" maxH ").Append(maxH.ToString("0.00")).Append(" depth ").Append(deep.ToString("0.00"));
        foreach (var kv in where) sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
        sb.Append('\n');
    }
    sb.Append("TOTAL hits ").Append(total).Append(" in ").Append(framesHit).Append(" frames; vertices sunk over 3 cm into the open ramp ").Append(totalSunk).Append('\n');
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
}
System.IO.File.WriteAllText("Tools/PlayTest/r40_fit_probe.txt", sb.ToString());
return sb.ToString();
