// Proof that menu creatures walk head first: pauses, looks at the creature named in enemy_arg.txt from its side, renders
// <look_arg>_a.png, steps frames_arg.txt frames, renders <look_arg>_b.png with the camera unchanged. Reports positions.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim();
string stem = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
int frames = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/frames_arg.txt").Trim());
UnityEditor.EditorApplication.isPaused = true;
Game.Enemies.Enemy target = null;
foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>()) { if (e.IsAlive && e.name == wanted) { target = e; break; } }
if (target == null) return "none";
var cam = UnityEngine.Camera.main;
var drift = cam.GetComponent<Game.UI.MenuCameraDrift>(); if (drift != null) drift.enabled = false;
UnityEngine.Vector3 side = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, target.Heading);
var rot = UnityEngine.Quaternion.LookRotation(-side + UnityEngine.Vector3.down * 0.6f);
cam.transform.rotation = rot;
cam.transform.position = target.transform.position + target.Heading * 0.5f - rot * UnityEngine.Vector3.forward * 3f;
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>()) canvas.enabled = false;
var sb = new System.Text.StringBuilder();
for (int shot = 0; shot < 2; shot++)
{
    if (shot == 1) for (int i = 0; i < frames; i++) UnityEditor.EditorApplication.Step();
    var texture = new UnityEngine.RenderTexture(1280, 720, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
    float aspect = cam.aspect;
    cam.targetTexture = texture; cam.aspect = 1280f / 720f; cam.Render(); cam.targetTexture = null; cam.aspect = aspect;
    UnityEngine.RenderTexture.active = texture;
    var image = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
    image.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0); image.Apply();
    UnityEngine.RenderTexture.active = null; texture.Release();
    System.IO.File.WriteAllBytes(stem + (shot == 0 ? "_a.png" : "_b.png"), image.EncodeToPNG());
    sb.Append(" pos" + shot + "=" + target.transform.position.ToString("F2"));
}
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>()) canvas.enabled = true;
return "heading " + target.Heading.ToString("F2") + sb;
