/// <summary>
/// Round 88a without the full generator (Remco's rule). Run fills the radar ping of the sound library from its approved
/// recording, then measures the blow of every filled sound set and writes it beside the gain (the hero's power pulse and
/// the pulses of the bosses swell before they hit; the players start them ahead). Safe to run again; the generator
/// writes the same (ProjectGenerator.Audio.cs).
/// Run: bash Tools/PlayTest/stage.sh run ApplyRound88.cs ApplyRound88.Run
/// </summary>
public static class ApplyRound88
{
    public static string Run() => Game.Editor.ProjectGenerator.ApplyLibrarySets("_radarPings") + Game.Editor.ProjectGenerator.ApplySoundImpacts();
}
