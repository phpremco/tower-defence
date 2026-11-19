/// <summary>
/// Round 85 without the full generator (Remco's rule): every boss creature gets the armour pool of the boss armour rule
/// (BalanceRules.BossArmourShare of its body at least; a boss with more of its own keeps it) and the kill money that
/// follows. The generator writes the same. Safe to run again. Then the solve stage (ApplyRound68.cs), and for the
/// continent bosses the finale stage (Docs/COMMANDS.md, "Round 82"). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound85.cs --entry ApplyRound85.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class ApplyRound85
{
    public static string Run() => Game.Editor.BalanceStages.ApplyBossArmour();
}
