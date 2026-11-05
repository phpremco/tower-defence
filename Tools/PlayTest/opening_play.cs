// Opening cutscene: seeks "shot progress frames" (Tools/PlayTest/cut_arg.txt), then plays that many frames at a fixed
// 1/30 s so played-only moments (the Queen's scream, a snap) happen, and renders the main camera at 1920x1080 into the
// PNG named in Tools/PlayTest/look_arg.txt.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
int shotIndex = int.Parse(parts[0]); float shotProgress = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture); int frames = int.Parse(parts[2]);
player.Seek(shotIndex, shotProgress);
UnityEngine.Time.captureDeltaTime = 1f / 30f;
for (int i = 0; i < frames; i++) { UnityEditor.EditorApplication.Step(); }
UnityEngine.Time.captureDeltaTime = 0f;
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var camera = UnityEngine.Camera.main;
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture; camera.aspect = 1920f / 1080f; camera.Render(); camera.targetTexture = null; camera.aspect = aspect;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
UnityEngine.RenderTexture.active = null; texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + System.IO.Path.GetFileName(path) + " " + player.Sequencer.Current.Action + " t=" + player.Sequencer.LocalTime.ToString("0.00");
