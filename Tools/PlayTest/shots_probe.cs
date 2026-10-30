// Reports the shots in flight (style, beam state) and centres the camera on the first one, so a screenshot catches it.
var sb = new System.Text.StringBuilder();
int count = 0;
foreach (var projectile in UnityEngine.Object.FindObjectsByType<Game.Combat.Projectile>())
{
    if (!projectile.gameObject.activeInHierarchy)
    {
        continue;
    }

    if (count == 0)
    {
        var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
        var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
        if (pan != null)
        {
            pan.enabled = false;
        }

        rig.SetView(projectile.Origin, 2.4f);
    }

    count++;
    var beam = projectile.transform.Find("Beam");
    sb.Append(projectile.Style).Append(beam != null && beam.GetComponent<UnityEngine.LineRenderer>().enabled ? "(beam) " : " ");
}

return count + ": " + sb;
