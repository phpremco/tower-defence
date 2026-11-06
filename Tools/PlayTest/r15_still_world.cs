// Round 15: as render_still.cs but without any interface (every canvas is switched off for the picture), so a wreck
// under the defeat screen can be judged. Path from look_arg.txt.

var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var camera = UnityEngine.Camera.main;
var canvases = UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>();
var wasOn = new bool[canvases.Length];
for (int i = 0; i < canvases.Length; i++) { wasOn[i] = canvases[i].enabled; canvases[i].enabled = false; }
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
for (int i = 0; i < canvases.Length; i++) { canvases[i].enabled = wasOn[i]; }
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + path;
