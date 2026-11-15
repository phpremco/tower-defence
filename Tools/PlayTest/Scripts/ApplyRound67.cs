using System.Text;

/// <summary>
/// Round 67 without the full generator (Remco's rule): writes what the changed generator now would, and nothing else.
///  1. The combat rules (the generator's "Combat rules" step): Data/Balance/CombatRules.asset from the generator's
///     tables (damage type against armour and air, the named exceptions, the creature trait thresholds, the reference
///     fight and margin per trait, the grace windows), and every weapon stamped with its damage type and a reference
///     to the rules (towers, the hero's weapons and shockwave, the attack drone, the path objects). No weapon keeps a
///     factor of its own.
///  2. The towers (the generator's "Towers" and "Roster" steps): every level as a named step that improves one thing,
///     each tower's roles and its role line. The damage per second and the price of every level are what they were.
///  3. The tower art: the level-3 path-B looks taken out of the data (the A/B paths are cut).
///  4. Spencer's blade: its looks per blade damage tier on the gear art (orange from tier 5, red at tier 10).
/// The waves follow with the pipeline (measure, fit, apply, solve). Safe to run again. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound67.cs --entry ApplyRound67.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class ApplyRound67
{
    public static string Run()
    {
        var report = new StringBuilder();
        report.Append(Game.Editor.ProjectGenerator.ApplyCombatRules());
        report.Append(Game.Editor.ProjectGenerator.ApplyTowerSteps());
        report.Append(Game.Editor.BlenderTowerBuilder.DropCutPathLooks());
        report.Append(Game.Editor.ProjectGenerator.ApplyBladeStages());
        return report.ToString();
    }
}
