// QA round 32: renders the main camera with the interface at the size in Tools/PlayTest/qa/still_arg.txt
// ("<png path> <width> <height>") with the canvases laid out for that size: the overlay canvases switch to camera space
// on a camera that draws into the render texture first, so CanvasScaler and the layouts see the target size, as on a
// screen of that resolution. The Game view is left alone. Canvases go back to overlay afterwards.
var args = System.IO.File.ReadAllText("Tools/PlayTest/qa/still_arg.txt").Trim().Split('|');
string path = args[0];
int w = int.Parse(args[1]), h = int.Parse(args[2]);
var camera = UnityEngine.Camera.main;
if (camera == null) { foreach (var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None)) { if (c.isActiveAndEnabled) { camera = c; break; } } }
var texture = new UnityEngine.RenderTexture(w, h, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = (float)w / h;
var switched = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
{
    if (canvas.isRootCanvas && canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay)
    {
        canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = camera.nearClipPlane + 0.5f;
        switched.Add(canvas);
    }
}
UnityEngine.Canvas.ForceUpdateCanvases();
foreach (var canvas in switched) { foreach (var r in canvas.GetComponentsInChildren<UnityEngine.RectTransform>()) { UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(r); } }
UnityEngine.Canvas.ForceUpdateCanvases();
string sizes = "";
foreach (var canvas in switched) { sizes += canvas.pixelRect.width.ToString("0") + "x" + canvas.pixelRect.height.ToString("0") + " "; }
// The tilt-shift pass would blur the camera-space interface; it is off for the still (the overlay UI never gets it).
var passes = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Universal.FullScreenPassRendererFeature>();
var wasOn = new bool[passes.Length];
for (int i = 0; i < passes.Length; i++) { wasOn[i] = passes[i].isActive; passes[i].SetActive(false); }
camera.Render();
for (int i = 0; i < passes.Length; i++) { passes[i].SetActive(wasOn[i]); }
var image = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGB24, false);
UnityEngine.RenderTexture.active = texture;
image.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
camera.targetTexture = null;
camera.aspect = aspect;
foreach (var canvas in switched) { canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay; }
UnityEngine.Canvas.ForceUpdateCanvases();
texture.Release();
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + path + " canvases " + sizes;
