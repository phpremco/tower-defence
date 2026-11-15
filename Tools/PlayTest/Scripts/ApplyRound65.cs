/// <summary>
/// Round 65 without the full generator (Remco's rule): writes what the changed generator now would, and nothing else.
///  1. The terrain rules asset (reach on high ground, the sight rule) and every level's link to it.
///  2. Every level's plateaus by the plateau rules (ProjectGenerator.ApplyPlateauSkirt): raised cells beside the road, a
///     liquid, a mine or a nest taken down, the ring written as slope cells, the raised cells behind the lip closed.
///  3. The ground of every level painted again (the painter draws the slope). "nobake" as the first line of
///     Tools/PlayTest/r65_apply_arg.txt skips that step; a line "only=C1_01" does one level alone.
///  4. The story's high-ground hint and the loading tips (ProjectGenerator.ApplyRound65Story).
/// The waves follow with the solve stage (ApplyWaves.cs). Safe to run again. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound65.cs --entry ApplyRound65.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyRound65
{
    public static string Run()
    {
        const string arg = "Tools/PlayTest/r65_apply_arg.txt";
        string text = System.IO.File.Exists(arg) ? System.IO.File.ReadAllText(arg) : string.Empty;
        string only = null;
        foreach (string line in text.Split((char)10))
        {
            if (line.Trim().StartsWith("only="))
            {
                only = line.Trim().Substring("only=".Length);
            }
        }

        return Game.Editor.ProjectGenerator.ApplyPlateausToEveryLevel(!text.Contains("nobake"), only) + Game.Editor.ProjectGenerator.ApplyRound65Story();
    }
}
