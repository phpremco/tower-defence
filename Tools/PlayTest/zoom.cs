// Zooms the camera onto the hero ("hero"), the first gate ("gate") or the base ("base"), as named in zoom_arg.txt.
string target = System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

UnityEngine.Vector3 focus = context.Hero.transform.position;
if (target == "gate")
{
    foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
    {
        if (tower.Definition.BlocksGroundEnemies)
        {
            focus = tower.Position;
            break;
        }
    }
}
else if (target == "towers")
{
    foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
    {
        if (!tower.Definition.BlocksGroundEnemies)
        {
            focus = tower.Position;
            break;
        }
    }
}
else if (target == "roster")
{
    int count = 0;
    UnityEngine.Vector3 sum = UnityEngine.Vector3.zero;
    foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
    {
        if (!tower.Definition.BlocksGroundEnemies)
        {
            sum += tower.Position;
            count++;
        }
    }

    focus = count > 0 ? sum / count : focus;
}
else if (target == "spawn")
{
    UnityEngine.Vector2Int[] road = context.Level.MainRoad;
    focus = context.Level.Grid.CellToWorld(road[System.Math.Min(3, road.Length - 1)]);
}
else if (target == "enemy")
{
    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        if (enemy.IsAlive)
        {
            focus = enemy.Position;
            break;
        }
    }
}
else if (target == "base")
{
    UnityEngine.Vector2Int[] road = context.Level.MainRoad;
    focus = context.Level.Grid.CellToWorld(road[road.Length - 1]);
}

var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
if (pan != null)
{
    pan.enabled = false;
}

float size = System.IO.File.Exists("Tools/PlayTest/zoom_size.txt") ? float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture) : 3f;
rig.SetView(focus, size);
return "zoomed on " + target + " at " + focus.ToString("0.0");
