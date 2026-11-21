/// <summary>
/// Round 88c without the full generator (Remco's rule): the first-raid purse on the four difficulties (0 / 0 / 1 / 1 sky
/// guns' worth), a brood call sound for the five level bosses that had none (existing approved recordings), and the combat
/// rules asset rewritten (the mob fight's reason). Safe to run again; the generator writes the same
/// (ProjectGenerator.Round88c.cs). After it: stage.sh run ApplyRound88.cs ApplyRound88.Run measures the blow of the new
/// sound sets, and the calibration is re-stamped (the difficulty profiles are in its hash only through the rules).
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88c.cs ApplyRound88c.Run
/// </summary>
public static class ApplyRound88c
{
    public static string Run() => Game.Editor.ProjectGenerator.ApplyRound88c();
}
