// Round 38: renders the main camera off-screen at 1920x1080 without the screen-space interface (world-space health bars
// stay) into the PNG named in Tools/PlayTest/look_arg.txt; the Game view is left alone.
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var camera = UnityEngine.Camera.main;
var hidden = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
{
    if (canvas.isRootCanvas && canvas.renderMode != UnityEngine.RenderMode.WorldSpace && canvas.enabled) { canvas.enabled = false; hidden.Add(canvas); }
}
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = 1920f / 1080f;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
foreach (var c in hidden) { c.enabled = true; }
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
UnityEngine.Object.Destroy(image);
return "wrote " + System.IO.Path.GetFileName(path);
