/// <summary>
/// Round 88b without the full generator (Remco's rule): the tower threat. Writes the reach table into the combat rules
/// asset (Slam 3.2, Sting 4, Bolt 4.5 cells; boss reach 4), hands the rules to every creature, gives the Cragback its
/// slam on structures (with the kill money its threat now prices) and appends the level bosses' volley at the guns
/// (Venom Spit on the Brood Mother, Arc Discharge on the Core Wisp, Spine Volley on the Pack Alpha). The generator
/// writes the same (ProjectGenerator.Round88b.cs, ProjectGenerator.Bestiary.cs). Safe to run again. The Cragback's
/// reward is in the calibration's input hash and its threat in the wave composer: solve and re-stamp after it.
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88bThreat.cs ApplyRound88bThreat.Run
/// </summary>
public static class ApplyRound88bThreat
{
    public static string Run() => Game.Editor.ProjectGenerator.ApplyTowerThreat();
}
