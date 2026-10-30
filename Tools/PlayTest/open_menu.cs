// Opens one in-level menu named in Tools/PlayTest/menu_arg.txt: "pause", "tower" (menu of the first built,
// non-gate tower), "gate" (menu of the first gate), "hero" (hero panel at post 0) or "win" (finish the level as won).
string which = System.IO.File.ReadAllText("Tools/PlayTest/menu_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

switch (which)
{
    case "pause":
        UnityEngine.Object.FindAnyObjectByType<Game.Levels.TimeController>().TogglePause();
        return "pause toggled, paused " + context.Paused;
    case "tower":
    case "gate":
        foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
        {
            if (tower.enabled && tower.Definition.BlocksGroundEnemies == (which == "gate"))
            {
                context.SelectedPlacedTower = tower;
                context.Mode = Game.Levels.PlacementMode.TowerMenu;
                context.Changed.Raise();
                return "menu on " + tower.name + " at " + tower.Position;
            }
        }

        return "no such tower";
    case "hero":
        context.HeroMenuPost = 0;
        context.Changed.Raise();
        return "hero panel open";
    case "win":
        var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
        var finish = typeof(Game.Levels.LevelController).GetMethod("Finish", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        finish.Invoke(controller, new object[] { Game.Levels.LevelOutcome.Won });
        return "finished as " + context.Outcome;
    default:
        return "unknown menu " + which;
}
