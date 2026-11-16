// Round 73: a still at a chosen size with the HUD drawn over the finished picture, as the game shows it (an overlay canvas
// is never graded, vignetted or tilt-shifted; r70_still.cs puts the canvases into the main camera's post stack, which
// darkens the corner icons). The field renders with its post stack; the canvases render alone on a camera without post
// onto a transparent texture; the two are blended on the CPU (the UI shader leaves premultiplied colour and its alpha).
// Tools/PlayTest/look_arg.txt holds "<png path> <width> <height>". The canvases go back to overlay afterwards.
var parts = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim().Split(' ');
var path = parts[0];
int width = int.Parse(parts[1]);
int height = int.Parse(parts[2]);
var camera = UnityEngine.Camera.main;
var field = new UnityEngine.RenderTexture(width, height, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = field;
camera.aspect = (float)width / height;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
UnityEngine.RenderTexture.active = field;
var below = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGBA32, false);
below.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
below.Apply();

var uiGo = new UnityEngine.GameObject("R73StillUiCamera");
var ui = uiGo.AddComponent<UnityEngine.Camera>();
ui.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
ui.backgroundColor = new UnityEngine.Color(0f, 0f, 0f, 0f);
const int StillLayer = 31;
ui.cullingMask = 1 << StillLayer;
var layers = new System.Collections.Generic.Dictionary<UnityEngine.Transform, int>();
ui.allowHDR = false;
ui.allowMSAA = false;
var data = uiGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
data.renderPostProcessing = false;
var hud = new UnityEngine.RenderTexture(width, height, 24, UnityEngine.RenderTextureFormat.ARGB32);
ui.targetTexture = hud;
ui.aspect = (float)width / height;
var moved = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>())
{
    if (canvas.isRootCanvas && canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay && canvas.isActiveAndEnabled)
    {
        canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = ui;
        canvas.planeDistance = ui.nearClipPlane + 0.5f;
        moved.Add(canvas);
        foreach (var t in canvas.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            layers[t] = t.gameObject.layer;
            t.gameObject.layer = StillLayer;
        }
    }
}

UnityEngine.Canvas.ForceUpdateCanvases();
ui.Render();
foreach (var pair in layers)
{
    pair.Key.gameObject.layer = pair.Value;
}

foreach (var canvas in moved)
{
    canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
}

UnityEngine.RenderTexture.active = hud;
var over = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGBA32, false);
over.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
over.Apply();
UnityEngine.RenderTexture.active = null;
UnityEngine.Object.DestroyImmediate(uiGo);
field.Release();
hud.Release();
var a = below.GetPixels32();
var b = over.GetPixels32();
for (int i = 0; i < a.Length; i++)
{
    int keep = 255 - b[i].a;
    a[i].r = (byte)System.Math.Min(255, b[i].r + a[i].r * keep / 255);
    a[i].g = (byte)System.Math.Min(255, b[i].g + a[i].g * keep / 255);
    a[i].b = (byte)System.Math.Min(255, b[i].b + a[i].b * keep / 255);
    a[i].a = 255;
}

below.SetPixels32(a);
below.Apply();
System.IO.File.WriteAllBytes(path, below.EncodeToPNG());
return "wrote " + path + " (" + moved.Count + " canvases over the field)";
