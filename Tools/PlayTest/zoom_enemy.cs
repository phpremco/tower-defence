// Zooms the camera onto the first living enemy whose definition name is in Tools/PlayTest/enemy_arg.txt (first word),
// at the factor in zoom_size.txt, and reports what it is doing.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.name == wanted)
    {
        rig.SetView(enemy.Position, zoom);
        // Frozen, so the screenshot that follows a moment later still has the creature in the middle; resume.cs thaws it.
        UnityEngine.Time.timeScale = 0f;
        return "zoomed on " + wanted + " at " + enemy.Position.ToString("0.0") + " rolling " + enemy.IsRolling + " origin " + enemy.Origin + " hp " + enemy.Health.Current.ToString("0");
    }
}

return "no living " + wanted;
