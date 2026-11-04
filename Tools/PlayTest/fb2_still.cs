// Planet pass: like imp3b_still.cs (editor paused, seek and render in one call), with the fb1_tune.txt shader lines applied after the seek.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
player.Seek(int.Parse(parts[0]), float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
var inv = System.Globalization.CultureInfo.InvariantCulture;
foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>())
{
    var m = renderer.sharedMaterial;
    if (m == null || m.shader == null) continue;
    foreach (var raw in System.IO.File.ReadAllLines("Tools/PlayTest/fb1_tune.txt"))
    {
        var p = raw.Trim().Split( );
        if (p.Length < 3 || p[0].StartsWith("#") || m.shader.name != "Game/" + p[0]) continue;
        if (p.Length >= 5) m.SetColor(p[1], new UnityEngine.Color(float.Parse(p[2], inv), float.Parse(p[3], inv), float.Parse(p[4], inv), p.Length > 5 ? float.Parse(p[5], inv) : 1f));
        else m.SetFloat(p[1], float.Parse(p[2], inv));
    }
}
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var camera = UnityEngine.Camera.main;
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = 1920f / 1080f;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + path + " " + player.Sequencer.Current.Action + " t=" + player.Sequencer.LocalTime.ToString("0.00");
