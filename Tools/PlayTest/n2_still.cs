// Night 2 (from imp3b_still.cs, which renders skinned meshes one pose late): holds the editor paused, seeks the cutscene to "shot progress" (Tools/PlayTest/cut_arg.txt) and renders
// the main camera at 1920x1080 into the PNG named in Tools/PlayTest/look_arg.txt in the same call, so no real time passes
// between the seek and the picture.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
// Skinned meshes are deformed in the player loop, so a render right after a seek shows the previous pose: seek, let one
// frame skin it, then seek the same moment again and render.
int shotIndex = int.Parse(parts[0]); float shotProgress = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shotIndex, shotProgress);
UnityEditor.EditorApplication.Step();
player.Seek(shotIndex, shotProgress);
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
// Mean luminance (0..1 of the 8-bit picture) of the whole frame and of the middle box, where the subject is framed.
var px = image.GetPixels32(); double all = 0, mid = 0; int midCount = 0;
for (int i = 0; i < px.Length; i++) { double l = (0.2126 * px[i].r + 0.7152 * px[i].g + 0.0722 * px[i].b) / 255.0; all += l; int x = i % 1920, y = i / 1920; if (x > 576 && x < 1344 && y > 270 && y < 810) { mid += l; midCount++; } }
string lum = " lum " + (all / px.Length).ToString("0.000") + " mid " + (mid / midCount).ToString("0.000");
return "wrote " + System.IO.Path.GetFileName(path) + lum + " " + player.Sequencer.Current.Action + " t=" + player.Sequencer.LocalTime.ToString("0.00");
