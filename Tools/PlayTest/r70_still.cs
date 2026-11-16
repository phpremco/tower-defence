// Round 70: render_still.cs at a chosen size. Tools/PlayTest/look_arg.txt holds "<png path> <width> <height>". The camera
// renders into a texture of that size before the canvases update, so a 1280x720 still shows the 720p layout.
var parts = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim().Split(' ');
var path = parts[0];
int width = int.Parse(parts[1]);
int height = int.Parse(parts[2]);
var camera = UnityEngine.Camera.main;
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>())
{
    if (canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay)
    {
        canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = camera.nearClipPlane + 0.5f;
    }
}
var texture = new UnityEngine.RenderTexture(width, height, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = (float)width / height;
UnityEngine.Canvas.ForceUpdateCanvases();
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + path;
