// Round 14: knocks a fifth off the first base turret, so a repair drone has a short job beside the bays.
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
{
    if (tower.Definition.IsBaseDefence)
    {
        tower.TakeDamage(tower.Health.Max * 0.2f);
        return "hurt turret at " + tower.Position.ToString("0.0") + " to " + tower.Health.Fraction.ToString("0.00");
    }
}

return "no base turret";
