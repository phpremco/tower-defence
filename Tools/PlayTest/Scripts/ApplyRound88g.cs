/// <summary>
/// Round 88g without the full generator: the share of a blow a gun takes per difficulty (Psycho 0.8) on the four
/// difficulty assets, and the health every star adds to a gun (BalanceSheet.GunHealthPerStar of the health it is built
/// with) in TowerLevel.HealthBonus of every gun asset; gates and the base's turret are left alone. Safe to run again;
/// the generator writes the same (ProjectGenerator.Round88g.cs, WriteLevels). The tower levels are in the calibration's
/// input hash: re-stamp after it (bash Tools/PlayTest/r88b_restamp.sh r88g).
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88g.cs ApplyRound88g.Run
/// </summary>
public static class ApplyRound88g
{
    public static string Run() => Game.Editor.ProjectGenerator.ApplyRound88g();
}
