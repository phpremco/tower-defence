// Round 59: a magnified still of one salvo capsule in the launch shot. Seeks "shot progress" (Tools/PlayTest/cut_arg.txt) like
// n2_still.cs, then reads Tools/PlayTest/r59_zoom_arg.txt: "<capsule> <nozzle|body|head|tail|mid> <fov>": keeps the shot
// camera's position and aims it with that field of view at the capsule's booster nozzle (the plume's start), its body, its
// head glow, the far end of its trail (the tail) or the middle of the trail. Renders 1920x1080 into look_arg.txt.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
int shotIndex = int.Parse(parts[0]); float shotProgress = float.Parse(parts[1], inv);
player.Seek(shotIndex, shotProgress);
UnityEditor.EditorApplication.Step();
player.Seek(shotIndex, shotProgress);
var words = System.IO.File.ReadAllText("Tools/PlayTest/r59_zoom_arg.txt").Trim().Split(' ');
int index = int.Parse(words[0]); string what = words[1]; float fov = float.Parse(words[2], inv);
var set = player.CurrentSet;
object drop = set.GetType().GetField("_capsules", flags).GetValue(set);
var list = (System.Collections.IList)drop.GetType().GetField("_capsules", flags).GetValue(drop);
object cap = list[index];
System.Func<string, object> get = n => cap.GetType().GetField(n, flags).GetValue(cap);
UnityEngine.Vector3 target;
var trail = (UnityEngine.LineRenderer)get("Trail");
switch (what)
{
    case "nozzle":
        object stage = get("Stage");
        var nozzle = (UnityEngine.Transform)typeof(Game.UI.CapsuleStage).GetField("_nozzle", flags).GetValue(stage);
        target = nozzle.position;
        break;
    case "head":
        target = ((UnityEngine.Transform)get("Head")).position;
        break;
    case "tail":
        target = trail.GetPosition(trail.positionCount - 1);
        break;
    case "mid":
        target = trail.GetPosition(trail.positionCount / 2);
        break;
    case "bay":
        var shipT = UnityEngine.GameObject.Find("Mothership").transform;
        target = shipT.TransformPoint((UnityEngine.Vector3)set.GetType().GetField("_bay", flags).GetValue(set));
        break;
    default:
        target = ((UnityEngine.Transform)get("Root")).position;
        break;
}
var camPos0 = UnityEngine.Camera.main.transform.position;
if (words.Length > 3)
{
    var shipS = UnityEngine.GameObject.Find("Mothership").transform;
    if (words[3] == "side") UnityEngine.Camera.main.transform.position = shipS.TransformPoint(new UnityEngine.Vector3(14f, -3f, -0.5f));
    if (words[3] == "under") UnityEngine.Camera.main.transform.position = shipS.TransformPoint(new UnityEngine.Vector3(3f, -14f, -2f));
}
var camera = UnityEngine.Camera.main;
float fov0 = camera.fieldOfView; var rot0 = camera.transform.rotation;
camera.transform.rotation = UnityEngine.Quaternion.LookRotation(target - camera.transform.position);
camera.fieldOfView = fov;
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
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
camera.fieldOfView = fov0; camera.transform.rotation = rot0; camera.transform.position = camPos0;
return "wrote " + System.IO.Path.GetFileName(path) + " capsule " + index + " " + what + " at " + target.ToString("0.00") + " dist " + UnityEngine.Vector3.Distance(target, camera.transform.position).ToString("0.0");
