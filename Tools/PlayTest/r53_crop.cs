// Round 53: crops a PNG and scales it up by a whole factor (nearest pixel), one job per line of Tools/PlayTest/r53_crop.txt:
// "<input png>|<output png>|<x>|<y from top>|<width>|<height>|<factor>". Works in edit and play mode.
var sb = new System.Text.StringBuilder();
foreach (var line in System.IO.File.ReadAllLines("Tools/PlayTest/r53_crop.txt"))
{
    if (string.IsNullOrWhiteSpace(line)) { continue; }
    var f = line.Split('|');
    var source = new UnityEngine.Texture2D(2, 2);
    UnityEngine.ImageConversion.LoadImage(source, System.IO.File.ReadAllBytes(f[0]));
    int x = int.Parse(f[2]), yTop = int.Parse(f[3]), w = int.Parse(f[4]), h = int.Parse(f[5]), k = int.Parse(f[6]);
    x = UnityEngine.Mathf.Clamp(x, 0, source.width - w);
    yTop = UnityEngine.Mathf.Clamp(yTop, 0, source.height - h);
    int y = source.height - yTop - h;
    var pixels = source.GetPixels(x, y, w, h);
    var output = new UnityEngine.Texture2D(w * k, h * k, UnityEngine.TextureFormat.RGB24, false);
    var scaled = new UnityEngine.Color[w * k * h * k];
    for (int j = 0; j < h * k; j++) { for (int i = 0; i < w * k; i++) { scaled[j * w * k + i] = pixels[(j / k) * w + i / k]; } }
    output.SetPixels(scaled);
    output.Apply();
    System.IO.File.WriteAllBytes(f[1], UnityEngine.ImageConversion.EncodeToPNG(output));
    UnityEngine.Object.DestroyImmediate(source);
    UnityEngine.Object.DestroyImmediate(output);
    sb.Append("wrote ").Append(System.IO.Path.GetFileName(f[1])).Append("; ");
}
return sb.ToString();
