/// <summary>
/// The continent bosses' data again (round 29: health, armour pool, ward and escort powers, Codex text) without the full
/// generator (which syncs the Blender exports). Same code as the generator step "Continent bosses" minus the Codex rows and the
/// arena, which read the bosses by reference and need no rewrite. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBossData.cs --entry ApplyBossData.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class ApplyBossData
{
    public static string Run() => Game.Editor.BalanceStages.ApplyContinentBosses();
}
