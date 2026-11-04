// Round menu 2026-11-04: on the upgrade screen, selects every subject (or only those whose name contains the text in
// Tools/PlayTest/zoom_arg.txt, empty = all), renders its preview window into <look_arg.txt folder>/preview_<name>.png
// and lists what stands on the turntable: per renderer the mesh and whether it is an imported asset (Blender) or built
// at run time (code). Stage pieces are listed apart.
string filter = System.IO.File.Exists("Tools/PlayTest/zoom_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim() : "";
string folder = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var screen = UnityEngine.Object.FindAnyObjectByType<Game.UI.UpgradeScreen>();
if (screen == null) { return "no upgrade screen"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var subjects = (System.Collections.IList)typeof(Game.UI.UpgradeScreen).GetField("_subjects", flags).GetValue(screen);
var preview = (Game.UI.ModelPreview)typeof(Game.UI.UpgradeScreen).GetField("_preview", flags).GetValue(screen);
var turntable = (UnityEngine.Transform)typeof(Game.UI.ModelPreview).GetField("_turntable", flags).GetValue(preview);
var camera = (UnityEngine.Camera)typeof(Game.UI.ModelPreview).GetField("_camera", flags).GetValue(preview);
var sb = new System.Text.StringBuilder();
foreach (object subject in subjects)
{
    string name = (string)subject.GetType().GetField("Name").GetValue(subject);
    if (filter.Length > 0 && name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
    typeof(Game.UI.UpgradeScreen).GetMethod("Select", flags).Invoke(screen, new[] { subject });
    camera.Render();
    var rt = camera.targetTexture; UnityEngine.RenderTexture.active = rt;
    var image = new UnityEngine.Texture2D(rt.width, rt.height, UnityEngine.TextureFormat.RGB24, false);
    image.ReadPixels(new UnityEngine.Rect(0, 0, rt.width, rt.height), 0, 0); image.Apply(); UnityEngine.RenderTexture.active = null;
    System.IO.File.WriteAllBytes(folder + "/preview_" + name.Replace(' ', '_') + ".png", image.EncodeToPNG());
    UnityEngine.Object.Destroy(image);
    int imported = 0, built = 0; var builtNames = new System.Collections.Generic.List<string>();
    foreach (var r in turntable.GetComponentsInChildren<UnityEngine.Renderer>())
    {
        if (!r.enabled) continue;
        UnityEngine.Mesh mesh = r is UnityEngine.SkinnedMeshRenderer s ? s.sharedMesh : (r.GetComponent<UnityEngine.MeshFilter>() != null ? r.GetComponent<UnityEngine.MeshFilter>().sharedMesh : null);
        if (r is UnityEngine.ParticleSystemRenderer || r is UnityEngine.LineRenderer || r is UnityEngine.TrailRenderer) continue;
        if (mesh != null && UnityEditor.AssetDatabase.Contains(mesh)) imported++; else { built++; builtNames.Add(r.name + ":" + (mesh != null ? mesh.name : "-")); }
    }
    sb.Append(name).Append(" imported=").Append(imported).Append(" built=").Append(built);
    if (built > 0) sb.Append(" [").Append(string.Join(",", builtNames.GetRange(0, System.Math.Min(6, builtNames.Count)))).Append("]");
    sb.Append(" | ");
}
return sb.ToString();
