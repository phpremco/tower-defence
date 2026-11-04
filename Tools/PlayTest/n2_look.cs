// Night 2: seeks the cutscene to "shot progress" (cut_arg.txt), then puts the camera at "px py pz lx ly lz fov" from
// Tools/PlayTest/n2_look_arg.txt (world space; "drill", "base", "core", "heap" or "gate" as a first word makes it
// relative to that place of the field layout) and renders 1920x1080 into look_arg.txt, for close inspection only.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
int shotIndex = int.Parse(parts[0]); float shotProgress = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shotIndex, shotProgress);
UnityEditor.EditorApplication.Step();
player.Seek(shotIndex, shotProgress);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sets = (System.Collections.IDictionary)typeof(Game.UI.CutscenePlayer).GetField("_sets", flags).GetValue(player);
var field = sets.Contains(Game.Levels.CutsceneSet.Field) ? sets[Game.Levels.CutsceneSet.Field] : null;
var layout = field != null ? (Game.UI.FieldLayout)field.GetType().GetField("_layout", flags).GetValue(field) : null;
var words = System.IO.File.ReadAllText("Tools/PlayTest/n2_look_arg.txt").Trim().Split(' ');
var origin = UnityEngine.Vector3.zero; int at = 0;
if (layout != null && words[0] == "drill") { origin = layout.Drill; at = 1; }
if (layout != null && words[0] == "base") { origin = layout.Base; at = 1; }
if (layout != null && words[0] == "core") { origin = layout.Core; at = 1; }
if (layout != null && words[0] == "gate") { origin = layout.GateCentre; at = 1; }
if (layout != null && words[0] == "dig") { origin = layout.DigSite; at = 1; }
System.Func<int, float> f = i => float.Parse(words[at + i], System.Globalization.CultureInfo.InvariantCulture);
var camera = UnityEngine.Camera.main;
var pos = origin + new UnityEngine.Vector3(f(0), f(1), f(2));
var look = origin + new UnityEngine.Vector3(f(3), f(4), f(5));
camera.transform.SetPositionAndRotation(pos, UnityEngine.Quaternion.LookRotation(look - pos));
camera.fieldOfView = f(6);
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture; camera.aspect = 1920f / 1080f; camera.Render(); camera.targetTexture = null; camera.aspect = aspect;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
UnityEngine.RenderTexture.active = null; texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + path;
