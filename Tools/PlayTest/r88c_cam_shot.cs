// Round 88c: a still of whatever the main camera sees, in any scene (the menu has no level context, so r88_shot.cs does
// not serve it). Tools/PlayTest/r88c_cam_arg.txt: first line "<width> <height> <ui|clean>", second line the png path.
// Overlay canvases are drawn through the camera for the still and put back afterwards.
var lines = System.IO.File.ReadAllLines("Tools/PlayTest/r88c_cam_arg.txt");
var a = lines[0].Trim().Split(' ');
string png = lines[1].Trim();
int W = int.Parse(a[0]), H = int.Parse(a[1]);
bool ui = a.Length > 2 && a[2] == "ui";
var camera = UnityEngine.Camera.main;
if (camera == null) return "no main camera";
var moved = new System.Collections.Generic.List<UnityEngine.Canvas>();
var hidden = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
{
    if (!canvas.isRootCanvas || canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay) continue;
    if (ui) { canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 0.5f; moved.Add(canvas); }
    else if (canvas.enabled) { canvas.enabled = false; hidden.Add(canvas); }
}
UnityEngine.Canvas.ForceUpdateCanvases();
var texture = new UnityEngine.RenderTexture(W, H, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = (float)W / H;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
var previous = UnityEngine.RenderTexture.active;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = previous;
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(png));
System.IO.File.WriteAllBytes(png, UnityEngine.ImageConversion.EncodeToPNG(image));
UnityEngine.Object.Destroy(image);
texture.Release();
UnityEngine.Object.Destroy(texture);
foreach (var canvas in moved) canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
foreach (var canvas in hidden) canvas.enabled = true;
return "wrote " + png;
