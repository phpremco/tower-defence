// Lists the base turrets standing in the level: their cells, levels and whether they can be sold.
var sb = new System.Text.StringBuilder();
int count = 0;
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
{
    if (tower.Definition.IsBaseDefence)
    {
        count++;
        sb.Append(" turret@" + tower.Position.ToString("0.0") + " level=" + (tower.LevelIndex + 1) + " sellable=" + tower.CanBeSold + " alive=" + tower.IsAlive);
    }
}

var context = Game.Core.Services.Get<Game.Levels.LevelContext>();
return "baseWeaponLevel=" + context.GlobalModifiers.BaseWeaponLevel + " turrets=" + count + sb + " base@" + context.Base.Position.ToString("0.0");
