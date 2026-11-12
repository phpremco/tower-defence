// Round 45: renders the main camera off-screen at 1920x1080 without the screen-space interface (as r38_still.cs) into the
// PNG named in Tools/PlayTest/look_arg.txt. When Tools/PlayTest/r45_face.txt names an enemy ("<Name> [extra yaw]"), its
// first living one is turned to face the camera for this one render (the play camera looks down at 44 degrees, so a boss
// walking away shows only its back) and turned back afterwards; "-" leaves every heading alone.
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var faceSpec = System.IO.File.Exists("Tools/PlayTest/r45_face.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r45_face.txt").Trim().Split(' ') : new[] { "-" };
var camera = UnityEngine.Camera.main;
UnityEngine.Transform turned = null;
var before = UnityEngine.Quaternion.identity;
string note = "";
if (faceSpec[0] != "-")
{
    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
    {
        if (!enemy.IsAlive || enemy.Definition.name != faceSpec[0]) { continue; }
        var toCamera = -camera.transform.forward;
        toCamera.y = 0f;
        float extra = faceSpec.Length > 1 ? float.Parse(faceSpec[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
        foreach (var child in enemy.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            // The model root's yaw turns the export's -Z head onto the owner's heading: its back (-forward) is where the
            // head looks, so the root is turned to look away from the camera.
            if (!child.name.EndsWith("_model")) { continue; }
            turned = child;
            before = turned.rotation;
            turned.rotation = UnityEngine.Quaternion.LookRotation(-toCamera.normalized, UnityEngine.Vector3.up) * UnityEngine.Quaternion.Euler(0f, extra, 0f);
            note = " faced " + enemy.Definition.name + " head.camera " + UnityEngine.Vector3.Dot(-turned.forward, toCamera.normalized).ToString("0.00");
            break;
        }

        break;
    }
}

var hidden = new System.Collections.Generic.List<UnityEngine.Canvas>();
foreach (var canvas in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
{
    if (canvas.isRootCanvas && canvas.renderMode != UnityEngine.RenderMode.WorldSpace && canvas.enabled) { canvas.enabled = false; hidden.Add(canvas); }
}
var texture = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.DefaultHDR);
float aspect = camera.aspect;
camera.targetTexture = texture;
camera.aspect = 1920f / 1080f;
camera.Render();
camera.targetTexture = null;
camera.aspect = aspect;
foreach (var c in hidden) { c.enabled = true; }
if (turned != null) { turned.rotation = before; }
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
UnityEngine.Object.Destroy(image);
return "wrote " + System.IO.Path.GetFileName(path) + note;
