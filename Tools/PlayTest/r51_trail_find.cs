// Round 51: finds a shot in flight, still well short of its target, whose trail has grown (an enabled TrailRenderer with a few
// points) and frames the play
// camera on the trail's middle at the half height in zoom_size.txt, so a still catches the whole trail with its ends.
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
int flying = 0;
foreach (var projectile in UnityEngine.Object.FindObjectsByType<Game.Combat.Projectile>(UnityEngine.FindObjectsSortMode.None))
{
    if (!projectile.gameObject.activeInHierarchy) { continue; }
    flying++;
    var trail = projectile.GetComponent<UnityEngine.TrailRenderer>();
    if (trail == null || !trail.enabled || trail.positionCount < 3) { continue; }
    // Only a shot still well short of its target, so the trail's head is in the open and not inside the creature.
    if (UnityEngine.Vector3.Distance(projectile.transform.position, projectile.Destination) < 1.2f) { continue; }
    var head = trail.GetPosition(trail.positionCount - 1);
    var tail = trail.GetPosition(0);
    var middle = (head + tail) * 0.5f;
    var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
    var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
    if (pan != null) { pan.enabled = false; }
    var camera = UnityEngine.Camera.main;
    var ray = camera.transform.forward;
    rig.SetView(middle - ray * (middle.y / ray.y), zoom);
    camera.orthographicSize = zoom;
    return "found " + projectile.Style + " trail points " + trail.positionCount + " from " + tail.ToString("0.00") + " to " + head.ToString("0.00") + " mat " + trail.sharedMaterial.name + " caps " + trail.numCapVertices + " w0 " + trail.widthCurve.Evaluate(0f).ToString("0.00") + " w1 " + trail.widthCurve.Evaluate(1f).ToString("0.00");
}

return "none (" + flying + " in flight)";
