// Round 39: a magnified still from the shot's own camera. Tools/PlayTest/cut_arg.txt "<shot> <progress>", and
// Tools/PlayTest/r39_zoom_arg.txt "<fov> <target> [dx dy dz]": the camera stays where the shot has it and turns to the
// target (drill, base, core, capsule, hero, beacon, haulerN, builderN or "x y z"), plus the offset, with a narrow lens.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
int shotIndex = int.Parse(parts[0]); float shotProgress = float.Parse(parts[1], inv);
player.Seek(shotIndex, shotProgress);
UnityEditor.EditorApplication.Step();
player.Seek(shotIndex, shotProgress);
var words = System.IO.File.ReadAllText("Tools/PlayTest/r39_zoom_arg.txt").Trim().Split(' ');
float fov = float.Parse(words[0], inv);
var target = UnityEngine.Vector3.zero; int next = 2;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sets = (System.Collections.IDictionary)typeof(Game.UI.CutscenePlayer).GetField("_sets", flags).GetValue(player);
var fieldSet = sets.Contains(Game.Levels.CutsceneSet.Field) ? sets[Game.Levels.CutsceneSet.Field] : null;
var layout = fieldSet != null ? (Game.UI.FieldLayout)fieldSet.GetType().GetField("_layout", flags).GetValue(fieldSet) : null;
System.Func<string, UnityEngine.Vector3> named = n =>
{
    foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None)) if (t.name.ToLowerInvariant() == n && t.gameObject.activeInHierarchy) return t.position;
    return UnityEngine.Vector3.zero;
};
switch (words[1])
{
    case "drill": target = layout.Drill; break;
    case "base": target = layout.Base; break;
    case "core": target = layout.Core; break;
    case "capsule": target = named("capsule"); break;
    case "hero": target = named("mercer"); break;
    case "beacon": target = named("beacon"); break;
    default:
        if (words[1].StartsWith("hauler") || words[1].StartsWith("builder")) target = named(words[1]);
        else { target = new UnityEngine.Vector3(float.Parse(words[1], inv), float.Parse(words[2], inv), float.Parse(words[3], inv)); next = 4; }
        break;
}
if (words.Length >= next + 3) target += new UnityEngine.Vector3(float.Parse(words[next], inv), float.Parse(words[next + 1], inv), float.Parse(words[next + 2], inv));
var camera = UnityEngine.Camera.main;
camera.transform.rotation = UnityEngine.Quaternion.LookRotation(target - camera.transform.position);
camera.fieldOfView = fov;
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture; camera.aspect = 1920f / 1080f; camera.Render(); camera.targetTexture = null; camera.aspect = aspect;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
UnityEngine.RenderTexture.active = null; texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + System.IO.Path.GetFileName(path) + " target " + target + " from " + camera.transform.position;
