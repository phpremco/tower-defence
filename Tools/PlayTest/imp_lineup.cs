// Lines up display models of the creatures named in imp_arg.txt ("<ortho half height> <Name> <Name> ...") across the
// middle of the view, each in its idle cycle, and sets the orthographic size; imp_lineup_clear.cs removes them.
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/imp_arg.txt").Trim().Split(' ');
float half = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
var cam = UnityEngine.Camera.main;
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
if (pan != null) pan.enabled = false;
cam.orthographicSize = half;
var ray = cam.ViewportPointToRay(new UnityEngine.Vector3(0.5f, 0.5f, 0f));
float t = -ray.origin.y / ray.direction.y;
UnityEngine.Vector3 centre = ray.origin + ray.direction * t;
var right = cam.transform.right; right.y = 0f; right.Normalize();
var old = UnityEngine.GameObject.Find("ImpLineup");
if (old != null) UnityEngine.Object.Destroy(old);
var root = new UnityEngine.GameObject("ImpLineup").transform;
int n = args.Length - 1;
float width = half * cam.aspect * 1.6f;
for (int i = 0; i < n; i++)
{
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + args[i + 1] + ".asset");
    if (def == null) continue;
    var shown = Game.Enemies.EnemyDisplayBuilder.Build(root, def);
    float x = n == 1 ? 0f : -width / 2f + width * i / (n - 1);
    shown.position = centre + right * x + (def.Flies ? UnityEngine.Vector3.up * 1.6f : UnityEngine.Vector3.zero);
    shown.rotation = UnityEngine.Quaternion.LookRotation(-right * 0.3f - cam.transform.up * 0f + new UnityEngine.Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z) * -1f);
}
UnityEngine.Time.timeScale = 1f;
return "lined up " + n + " at " + centre.ToString("0.0");
