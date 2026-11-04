// Puts the main camera at an offset from the cutscene mothership (in the ship's own space, from Tools/PlayTest/fb2_ship_arg.txt
// "x y z"), aims it at the ship and renders a 1920x1080 still into the PNG named in Tools/PlayTest/look_arg.txt, all in one call
// so the cutscene camera cannot move it back first. Reports the bay light and the running lights.
var inv = System.Globalization.CultureInfo.InvariantCulture;
var p = System.IO.File.ReadAllText("Tools/PlayTest/fb2_ship_arg.txt").Trim().Split(' ');
var ship = UnityEngine.GameObject.Find("Mothership");
if (ship == null) return "no ship";
var offset = new UnityEngine.Vector3(float.Parse(p[0], inv), float.Parse(p[1], inv), float.Parse(p[2], inv));
var camera = UnityEngine.Camera.main;
var t = ship.transform;
camera.transform.position = t.TransformPoint(offset);
camera.transform.rotation = UnityEngine.Quaternion.LookRotation(t.position - camera.transform.position, t.up);
float fov = camera.fieldOfView;
camera.fieldOfView = 40f;
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = 1920f / 1080f;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
camera.fieldOfView = fov;
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
var info = new System.Text.StringBuilder("wrote " + path);
foreach (var light in ship.GetComponentsInChildren<UnityEngine.Light>())
{
    info.Append(" | " + light.name + " " + light.intensity.ToString("0.00", inv));
}
var lod = ship.GetComponentInChildren<UnityEngine.LODGroup>();
if (lod != null) info.Append(" | lods " + lod.lodCount);
return info.ToString();
