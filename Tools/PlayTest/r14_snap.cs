// Round 14: renders a still (as render_still.cs, path in look_arg.txt) only when a bay's lid is open and its drone part of
// the way in or out (depth between the two numbers in r14_wait.txt), and only once per path. Call it repeatedly while time
// runs slowly (r14_slow.cs); it reports the bays each time.
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var view = UnityEngine.Object.FindAnyObjectByType<Game.Levels.BaseDockView>();
if (view == null) { return "no bays"; }
string[] range = System.IO.File.ReadAllText("Tools/PlayTest/r14_wait.txt").Trim().Split(' ');
float low = float.Parse(range[0], System.Globalization.CultureInfo.InvariantCulture);
float high = float.Parse(range[1], System.Globalization.CultureInfo.InvariantCulture);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var open = (float[])typeof(Game.Levels.BaseDockView).GetField("_open", flags).GetValue(view);
var depth = (float[])typeof(Game.Levels.BaseDockView).GetField("_depth", flags).GetValue(view);
string state = "";
bool ready = false;
for (int i = 0; i < open.Length; i++)
{
    state += " bay" + (i + 1) + " " + open[i].ToString("0.00") + "/" + depth[i].ToString("0.00");
    ready |= open[i] > 0.9f && depth[i] > low && depth[i] < high;
}

if (!ready || System.IO.File.Exists(path)) { return (System.IO.File.Exists(path) ? "done" : "waiting") + state; }
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
return "snapped" + state;
