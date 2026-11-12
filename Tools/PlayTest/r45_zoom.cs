// Round 45: zooms the play camera onto the first living enemy named in Tools/PlayTest/enemy_arg.txt (first word) at the
// orthographic half height in zoom_size.txt, centred on its drawn body rather than its ground point (a flyer hangs high
// above its shadow), and freezes time for the stills; resume.cs thaws it.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
if (pan != null) { pan.enabled = false; }
var camera = UnityEngine.Camera.main;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    var bounds = new UnityEngine.Bounds(enemy.Position, UnityEngine.Vector3.zero);
    bool any = false;
    foreach (var renderer in enemy.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
    {
        if (!renderer.enabled || !renderer.isVisible && renderer.name.EndsWith("_Far")) { continue; }
        if (!any) { bounds = renderer.bounds; any = true; } else { bounds.Encapsulate(renderer.bounds); }
    }

    // The ground point the camera's ray through the body's centre meets: the rig looks at a point on the ground.
    var centre = bounds.center;
    var ray = camera.transform.forward;
    var focus = centre - ray * (centre.y / ray.y);
    rig.SetView(focus, zoom);
    camera.orthographicSize = zoom;
    UnityEngine.Time.timeScale = 0f;
    return "zoomed on " + wanted + " body " + centre.ToString("0.0") + " size " + bounds.size.ToString("0.0") + " focus " + focus.ToString("0.0");
}

return "no living " + wanted;
