// QA round 33: a before/after contact sheet. Tools/PlayTest/qa/sheet_arg.txt: the output path on the first line, then
// one row per line, "before.png|after.png" (names in ArtSource/Previews/QA; "-" leaves a grey cell). 640x360 cells.
var lines = System.IO.File.ReadAllLines("Tools/PlayTest/qa/sheet_arg.txt");
const int W = 640, H = 360, Gap = 8;
string dir = "ArtSource/Previews/QA/";
int rows = lines.Length - 1;
var sheet = new UnityEngine.Texture2D(W * 2 + Gap * 3, rows * (H + Gap) + Gap, UnityEngine.TextureFormat.RGB24, false);
var fill = new UnityEngine.Color32[sheet.width * sheet.height];
for (int i = 0; i < fill.Length; i++) fill[i] = new UnityEngine.Color32(12, 14, 20, 255);
sheet.SetPixels32(fill);
for (int r = 0; r < rows; r++)
{
    var pair = lines[r + 1].Split('|');
    for (int c = 0; c < 2; c++)
    {
        int x0 = Gap + c * (W + Gap);
        int y0 = sheet.height - (r + 1) * (H + Gap);
        string name = pair[c].Trim();
        if (name == "-" || !System.IO.File.Exists(dir + name))
        {
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) sheet.SetPixel(x0 + x, y0 + y, new UnityEngine.Color(0.18f, 0.19f, 0.22f));
            continue;
        }
        var src = new UnityEngine.Texture2D(2, 2);
        src.LoadImage(System.IO.File.ReadAllBytes(dir + name));
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) sheet.SetPixel(x0 + x, y0 + y, src.GetPixelBilinear((x + 0.5f) / W, (y + 0.5f) / H));
        UnityEngine.Object.DestroyImmediate(src);
    }
}
sheet.Apply();
System.IO.File.WriteAllBytes(lines[0].Trim(), sheet.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(sheet);
return "sheet " + rows + " rows -> " + lines[0].Trim();
