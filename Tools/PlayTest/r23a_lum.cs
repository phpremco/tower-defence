// Round 23a: mean brightness (0-255) of the PNGs listed one per line in Tools/PlayTest/r23a_lum.txt, to compare the field
// before the codex opens with the field after it closes.
var sb = new System.Text.StringBuilder();
foreach (var line in System.IO.File.ReadAllLines("Tools/PlayTest/r23a_lum.txt"))
{
    var path = line.Trim();
    if (path.Length == 0) { continue; }
    var image = new UnityEngine.Texture2D(2, 2);
    image.LoadImage(System.IO.File.ReadAllBytes(path));
    var pixels = image.GetPixels32();
    double r = 0, g = 0, b = 0;
    foreach (var p in pixels) { r += p.r; g += p.g; b += p.b; }
    int n = pixels.Length;
    sb.Append(System.IO.Path.GetFileName(path)).Append(": r ").Append((r / n).ToString("0.0")).Append(" g ").Append((g / n).ToString("0.0")).Append(" b ").Append((b / n).ToString("0.0")).Append(" | ");
    UnityEngine.Object.DestroyImmediate(image);
}
return sb.ToString();
