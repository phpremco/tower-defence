// Round 64: renders the main camera with the whole screen-space interface (every root canvas, the loading screen and the
// result panel included) off-screen at the size given, so the Game view is left alone and the interface is seen at 1080p
// and at 720p. Tools/PlayTest/r64_shot_arg.txt: "<width> <height> <png path>". The overlay canvases are drawn through the
// camera for the picture and put back afterwards. Returns what the loading screen shows, when one is up.
var shotArgs = System.IO.File.ReadAllText("Tools/PlayTest/r64_shot_arg.txt").Trim().Split(' ');
int shotW = int.Parse(shotArgs[0]);
int shotH = int.Parse(shotArgs[1]);
string shotPath = string.Join(" ", shotArgs, 2, shotArgs.Length - 2);
var shotCamera = UnityEngine.Camera.main;
if (shotCamera == null) return "no main camera";
var moved = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
{
    if (!canvas.isRootCanvas || canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay) continue;
    canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
    canvas.worldCamera = shotCamera;
    canvas.planeDistance = shotCamera.nearClipPlane + 0.5f;
    moved.Add(canvas);
}
var shotTexture = new UnityEngine.RenderTexture(shotW, shotH, 24, UnityEngine.RenderTextureFormat.ARGB32);
float shotAspect = shotCamera.aspect;
shotCamera.targetTexture = shotTexture;
shotCamera.aspect = (float)shotW / shotH;
UnityEngine.Canvas.ForceUpdateCanvases();
shotCamera.Render();
shotCamera.targetTexture = null;
shotCamera.aspect = shotAspect;
UnityEngine.RenderTexture.active = shotTexture;
var shotImage = new UnityEngine.Texture2D(shotW, shotH, UnityEngine.TextureFormat.RGB24, false);
shotImage.ReadPixels(new UnityEngine.Rect(0, 0, shotW, shotH), 0, 0);
shotImage.Apply();
UnityEngine.RenderTexture.active = null;
shotTexture.Release();
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(shotPath));
System.IO.File.WriteAllBytes(shotPath, shotImage.EncodeToPNG());
UnityEngine.Object.Destroy(shotImage);
foreach (var canvas in moved) { canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay; }
var loading = UnityEngine.Object.FindAnyObjectByType<Game.UI.LoadingScreen>();
var inv = System.Globalization.CultureInfo.InvariantCulture;
string state = loading != null ? " | loading reported " + loading.Reported.ToString("0.00", inv) + " shown " + loading.Shown.ToString("0.00", inv) + " alpha " + loading.GetComponentInChildren<UnityEngine.CanvasGroup>().alpha.ToString("0.00", inv) : " | no loading screen";
return "wrote " + shotPath + " scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " timeScale " + UnityEngine.Time.timeScale.ToString("0.00", inv) + state;
