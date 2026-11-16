using System.IO;
using Game.Economy;
using Game.Platform;
using UnityEngine;

/// <summary>
/// Round 69: the settings split on a COPY of the real save. Copies progress.json from the game's save folder into a
/// scratch folder (the original is only read), boots the settings and progress services on the copy, saves the progress
/// once (as the first purchase or story flag would), loads both again and reports every field. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/R69MigrationCheck.cs --entry R69MigrationCheck.Run --timeout_ms 60000
/// </summary>
public static class R69MigrationCheck
{
    public static string Run()
    {
        string source = Path.Combine(Application.persistentDataPath, "Saves", "progress.json");
        string scratch = Path.Combine(Path.GetTempPath(), "td_r69_migration");
        if (Directory.Exists(scratch))
        {
            Directory.Delete(scratch, true);
        }

        Directory.CreateDirectory(scratch);
        File.Copy(source, Path.Combine(scratch, "progress.json"));
        string before = File.ReadAllText(Path.Combine(scratch, "progress.json"));

        var save = new FileSaveService(scratch);
        var settings = new SettingsService(save);
        var progress = new ProgressService(save, new string[0]);
        string loadedOnly = File.ReadAllText(Path.Combine(scratch, "progress.json"));
        progress.Save();
        var settingsAgain = new SettingsService(save);
        var progressAgain = new ProgressService(save, new string[0]);

        ProgressData p = progressAgain.Data;
        SettingsData s = settingsAgain.Data;
        string after = File.ReadAllText(Path.Combine(scratch, "progress.json"));
        return $"untouched by load: {before == loadedOnly}; settings: {JsonUtility.ToJson(s)}; " +
               $"progress v{p.Version} research {p.ResearchPoints} power {p.PowerCurrency} intro {p.IntroSeen} story {p.SeenStoryIds.Count} " +
               $"enemies {p.SeenEnemyIds.Count} towers {string.Join(",", p.UnlockedTowerIds)} completed {p.CompletedLevels.Count} stars {p.Stars.Count} " +
               $"upgrades {p.Upgrades.Count} perks {string.Join(",", p.UnlockedHeroPerkIds)}; " +
               $"progress after save drops the options: {!after.Contains("MasterVolumePercent")}; bak exists: {File.Exists(Path.Combine(scratch, "progress.json.bak"))}; " +
               $"BEFORE {before.Length} chars, AFTER {after.Length} chars: {after}";
    }
}
