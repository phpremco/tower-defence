// Compares two PNGs named in Tools/PlayTest/diff_arg.txt (two paths, one per line): mean and max channel difference, and the share of pixels that differ by more than 0.05.
var lines = System.IO.File.ReadAllLines("Tools/PlayTest/diff_arg.txt");
var a = new UnityEngine.Texture2D(2, 2); UnityEngine.ImageConversion.LoadImage(a, System.IO.File.ReadAllBytes(lines[0].Trim()));
var b = new UnityEngine.Texture2D(2, 2); UnityEngine.ImageConversion.LoadImage(b, System.IO.File.ReadAllBytes(lines[1].Trim()));
if (a.width != b.width || a.height != b.height) return "size differs " + a.width + "x" + a.height + " vs " + b.width + "x" + b.height;
var pa = a.GetPixels(); var pb = b.GetPixels();
double sum = 0; float max = 0; int off = 0;
for (int i = 0; i < pa.Length; i++) { float d = (UnityEngine.Mathf.Abs(pa[i].r - pb[i].r) + UnityEngine.Mathf.Abs(pa[i].g - pb[i].g) + UnityEngine.Mathf.Abs(pa[i].b - pb[i].b)) / 3f; sum += d; if (d > max) max = d; if (d > 0.05f) off++; }
return "mean " + (sum / pa.Length).ToString("0.0000") + " max " + max.ToString("0.000") + " over0.05 " + (100.0 * off / pa.Length).ToString("0.00") + "%";
