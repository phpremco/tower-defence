// Renders the main camera at 1920x1080 with the interface into the PNG named in Tools/PlayTest/look_arg.txt, so a
// still has the real 16:9 framing (capture_game_view stretches the wide Game view). Overlay canvases are switched to
// camera space for the picture and stay so for the rest of the play session.
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
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
UnityEngine.Canvas.ForceUpdateCanvases();
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
return "wrote " + path;
