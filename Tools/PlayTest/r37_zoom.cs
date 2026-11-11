// Round 37: a magnified still of the orbit shot. Seeks "shot progress" (Tools/PlayTest/cut_arg.txt) like n2_still.cs, then
// reads Tools/PlayTest/r37_zoom_arg.txt: "fov tx ty tz [side]" - keeps the shot camera's position, aims it at the ship-local
// point (tx ty tz) with that field of view; with "side" the camera instead sits 14 m off the ship's starboard (+x, ship
// space) looking at the point, to judge depth. Renders 1920x1080 into look_arg.txt and prints the camera in ship space.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
int shotIndex = int.Parse(parts[0]); float shotProgress = float.Parse(parts[1], inv);
player.Seek(shotIndex, shotProgress);
UnityEditor.EditorApplication.Step();
player.Seek(shotIndex, shotProgress);
var words = System.IO.File.ReadAllText("Tools/PlayTest/r37_zoom_arg.txt").Trim().Split(' ');
var ship = UnityEngine.GameObject.Find("Mothership").transform;
var camera = UnityEngine.Camera.main;
var target = ship.TransformPoint(new UnityEngine.Vector3(float.Parse(words[1], inv), float.Parse(words[2], inv), float.Parse(words[3], inv)));
string info = "cam local " + ship.InverseTransformPoint(camera.transform.position).ToString("0.0") + " fwd " + ship.InverseTransformDirection(camera.transform.forward).ToString("0.00") + " fov " + camera.fieldOfView.ToString("0.0");
float fov0 = camera.fieldOfView; var pos0 = camera.transform.position; var rot0 = camera.transform.rotation;
if (words.Length > 4 && words[4] == "side") camera.transform.position = ship.TransformPoint(new UnityEngine.Vector3(14f, -3f, -0.5f));
if (words.Length > 4 && words[4] == "under") camera.transform.position = ship.TransformPoint(new UnityEngine.Vector3(3f, -14f, -2f));
camera.transform.rotation = UnityEngine.Quaternion.LookRotation(target - camera.transform.position);
camera.fieldOfView = float.Parse(words[0], inv);
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = 1920f / 1080f;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
camera.fieldOfView = fov0; camera.transform.SetPositionAndRotation(pos0, rot0);
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + System.IO.Path.GetFileName(path) + " " + info;
