// Round 50: r49_still.cs (inspection light, off-screen 1920x1080 render of the main camera into look_arg.txt) that can also
// orbit the camera for this one render instead of turning the creature, so effects placed from the animated body (a beam
// leaving a tail bone) stay where they are: Tools/PlayTest/r50_orbit.txt holds "<yaw degrees> <pitch degrees>" (pitch 0
// keeps the game pitch), turned about the point where the camera ray meets the height in its third field (default 1).
var path = System.IO.File.ReadAllText("Tools/PlayTest/look_arg.txt").Trim();
var faceSpec = System.IO.File.Exists("Tools/PlayTest/r45_face.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r45_face.txt").Trim().Split(' ') : new[] { "-" };
var camera = UnityEngine.Camera.main;
var orbitSpec = System.IO.File.Exists("Tools/PlayTest/r50_orbit.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r50_orbit.txt").Trim().Split(' ') : new[] { "0" };
float orbitYaw = float.Parse(orbitSpec[0], System.Globalization.CultureInfo.InvariantCulture);
float orbitPitch = orbitSpec.Length > 1 ? float.Parse(orbitSpec[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
float orbitHeight = orbitSpec.Length > 2 ? float.Parse(orbitSpec[2], System.Globalization.CultureInfo.InvariantCulture) : 1f;
var cameraPosition = camera.transform.position;
var cameraRotation = camera.transform.rotation;
if (orbitYaw != 0f || orbitPitch != 0f)
{
    var forward = camera.transform.forward;
    var pivot = cameraPosition + forward * ((orbitHeight - cameraPosition.y) / forward.y);
    float distance = (pivot - cameraPosition).magnitude;
    var flat = new UnityEngine.Vector3(forward.x, 0f, forward.z).normalized;
    float pitch = orbitPitch != 0f ? orbitPitch : UnityEngine.Mathf.Asin(-forward.y) * UnityEngine.Mathf.Rad2Deg;
    var look = UnityEngine.Quaternion.Euler(pitch, UnityEngine.Quaternion.LookRotation(flat).eulerAngles.y + orbitYaw, 0f);
    camera.transform.SetPositionAndRotation(pivot - look * UnityEngine.Vector3.forward * distance, look);
}
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

float boost = System.IO.File.Exists("Tools/PlayTest/r49_light.txt") ? float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r49_light.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture) : 1f;
var raised = new System.Collections.Generic.List<UnityEngine.Light>();
var ambientSky = UnityEngine.RenderSettings.ambientSkyColor;
var ambientEquator = UnityEngine.RenderSettings.ambientEquatorColor;
var ambientGround = UnityEngine.RenderSettings.ambientGroundColor;
var ambientLight = UnityEngine.RenderSettings.ambientLight;
UnityEngine.GameObject key = null;
if (boost > 1f)
{
    foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None))
    {
        if (l.isActiveAndEnabled && l.type == UnityEngine.LightType.Directional) { l.intensity *= boost; raised.Add(l); }
    }

    UnityEngine.RenderSettings.ambientSkyColor = ambientSky * boost;
    UnityEngine.RenderSettings.ambientEquatorColor = ambientEquator * boost;
    UnityEngine.RenderSettings.ambientGroundColor = ambientGround * boost;
    UnityEngine.RenderSettings.ambientLight = ambientLight * boost;
    key = new UnityEngine.GameObject("InspectionKey");
    var keyLight = key.AddComponent<UnityEngine.Light>();
    keyLight.type = UnityEngine.LightType.Directional;
    keyLight.color = new UnityEngine.Color(1f, 0.97f, 0.92f);
    keyLight.intensity = 0.6f * boost;
    keyLight.shadows = UnityEngine.LightShadows.None;
    key.transform.rotation = UnityEngine.Quaternion.LookRotation(camera.transform.forward + camera.transform.right * 0.5f, UnityEngine.Vector3.up);
    note += " light x" + boost.ToString("0.0");
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
foreach (var l in raised) { l.intensity /= boost; }
if (boost > 1f)
{
    UnityEngine.RenderSettings.ambientSkyColor = ambientSky;
    UnityEngine.RenderSettings.ambientEquatorColor = ambientEquator;
    UnityEngine.RenderSettings.ambientGroundColor = ambientGround;
    UnityEngine.RenderSettings.ambientLight = ambientLight;
    UnityEngine.Object.Destroy(key);
}

if (turned != null) { turned.rotation = before; }
camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
UnityEngine.RenderTexture.active = texture;
var image = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
image.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
image.Apply();
UnityEngine.RenderTexture.active = null;
texture.Release();
System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
UnityEngine.Object.Destroy(image);
return "wrote " + System.IO.Path.GetFileName(path) + note;
