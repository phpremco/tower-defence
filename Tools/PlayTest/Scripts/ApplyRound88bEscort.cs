/// <summary>
/// Round 88b without the full generator (Remco's rule): the escort data. Every level boss that had no Spawn power gets
/// one with a creature of its own kind (Pack Alpha: Prowlers, Core Wisp: Wisps, Acid Empress: Stingers, Crystal Warlord:
/// Shard Runners, Sky Sovereign: Skyrays, Roller King: Rollers), and the four difficulties get their escort worth
/// (BalanceSheet: 0.4 / 1 / 1.1 / 1.2). Safe to run again; the generator writes the same
/// (ProjectGenerator.Escorts.cs). The continent bosses are not touched.
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88bEscort.cs ApplyRound88bEscort.Run
/// </summary>
public static class ApplyRound88bEscort
{
    public static string Run() => Game.Editor.ProjectGenerator.ApplyEscorts();
}
