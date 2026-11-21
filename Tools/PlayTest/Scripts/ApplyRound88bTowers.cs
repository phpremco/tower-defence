/// <summary>
/// Round 88b without the full generator (Remco's rule): the Cannon's blast hurts at most two creatures as built, three
/// at level 2 and four at level 3 (TowerLevel.SplashTargets, read through TowerDefinition.SplashTargetsAt by the game,
/// the balance model and the texts). The Flak keeps four at every level, the hybrids keep what they had. The generator
/// writes the same (ProjectGenerator.Towers.cs, CannonSplashTargets). Safe to run again. The caps are in the
/// calibration's input hash: re-stamp after it (the curator's step). Run:
/// bash Tools/PlayTest/stage.sh run ApplyRound88bTowers.cs ApplyRound88bTowers.Run
/// </summary>
public static class ApplyRound88bTowers
{
    public static string Run() => Game.Editor.ProjectGenerator.ApplyBlastCaps();
}
