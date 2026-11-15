// Round 63: a magnified still. Seeks "shot progress" (cut_arg.txt) like n2_still.cs, then reads r63_zoom_arg.txt:
// "<target> <fov> [<camera>]" where target and camera are a scene object's name (its renderers' bounds centre) or "x,y,z";
// the shot camera (moved to <camera> when given) is aimed at the target with that field of view and renders 1920x1080
// into look_arg.txt. The shot camera is put back afterwards.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
player.Seek(int.Parse(parts[0]), float.Parse(parts[1], inv));
UnityEditor.EditorApplication.Step();
player.Seek(int.Parse(parts[0]), float.Parse(parts[1], inv));
var words = System.IO.File.ReadAllText("Tools/PlayTest/r63_zoom_arg.txt").Trim().Split(' ');
System.Func<string, UnityEngine.Vector3> where = s =>
{
    if (s.Contains(","))
    {
        var c = s.Split(',');
        return new UnityEngine.Vector3(float.Parse(c[0], inv), float.Parse(c[1], inv), float.Parse(c[2], inv));
    }
    var go = UnityEngine.GameObject.Find(s);
    if (go == null) return UnityEngine.Vector3.zero;
    var b = new UnityEngine.Bounds(go.transform.position, UnityEngine.Vector3.zero);
    foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) if (!(r is UnityEngine.ParticleSystemRenderer) && r.enabled) b.Encapsulate(r.bounds);
    return b.center;
};
var target = where(words[0]);
float fov = float.Parse(words[1], inv);
var camera = UnityEngine.Camera.main;
var pos0 = camera.transform.position; var rot0 = camera.transform.rotation; float fov0 = camera.fieldOfView;
if (words.Length > 2) camera.transform.position = where(words[2]);
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
camera.transform.SetPositionAndRotation(pos0, rot0);
camera.fieldOfView = fov0;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
return "wrote " + System.IO.Path.GetFileName(path) + " target " + target.ToString("0.00");
