/// <summary>
/// Round 87 without the full generator (Remco's rule): the beam's role. Writes the combat rules asset with its family
/// table (Beam on Energy x2), every creature's family (the Wisp and the Core Wisp are Energy) and the levels and sweep
/// time of the Laser (smaller pulses at a higher rate than the Gun Turret, 0.88 of it per coin on an ordinary body).
/// The generator writes the same (ProjectGenerator.Damage.cs, ProjectGenerator.World.cs). Safe to run again. The
/// texts are the localisation table's (en.data.txt). Tower numbers are in the calibration's input hash: re-stamp
/// after it (Docs/COMMANDS.md, "Round 86"). Run:
/// bash Tools/PlayTest/stage.sh run ApplyRound87Laser.cs ApplyRound87Laser.Run
/// </summary>
public static class ApplyRound87Laser
{
    public static string Run() => Game.Editor.ProjectGenerator.ApplyBeamRole();
}
