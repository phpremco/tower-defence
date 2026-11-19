using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 86, why the paper and the game disagree on a boss wave: one stored wave against a defence that really stood in
/// a recorded run, taken apart into the ledger's own terms (the crowd against the stream capacity, the boss against the
/// single-target capacity, per gun the seconds the boss is in its reach and what the ledger lets it deal in them).
/// Arguments in Tools/PlayTest/boss_paper_arg.txt, one per line: "cell=C1_01:0", "wave=12", "multiple=1.76" (the
/// probe's health multiple), "stand=Cannon:0" (tower id, level index; one line per gun), "label=name".
/// Writes Tools/PlayTest/boss_paper_&lt;label&gt;.txt; no asset is written.
/// </summary>
public static class BossPaperLab
{
    private const string ArgPath = "Tools/PlayTest/boss_paper_arg.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        var calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
        string key = "C1_01";
        int d = 0;
        int waveNumber = 12;
        float multiple = 1f;
        string label = "run";
        var standing = new List<(string TowerId, int Level)>();
        foreach (string raw in File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0])
        {
            string[] kv = raw.Trim().Split('=');
            if (kv.Length != 2)
            {
                continue;
            }

            string value = kv[1].Trim();
            switch (kv[0].Trim())
            {
                case "cell":
                    key = value.Split(':')[0];
                    d = int.Parse(value.Split(':')[1]);
                    break;
                case "wave":
                    waveNumber = int.Parse(value);
                    break;
                case "multiple":
                    multiple = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "label":
                    label = value;
                    break;
                case "stand":
                    standing.Add((value.Split(':')[0], int.Parse(value.Split(':')[1])));
                    break;
            }
        }

        BalanceRules rules = BalanceRules.Default;
        CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, calibration);
        rules = model.Rules;
        int index = model.IndexOf(key);
        LevelDefinition level = null;
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level != null && node.Level.name == "Level_" + key)
            {
                level = node.Level;
            }
        }

        var waves = new List<WaveSpec>();
        foreach (WaveDefinition stored in level.GetWaves(catalog.Difficulties[d]))
        {
            WaveSpec spec = BalanceLedger.Spec(stored, model.Enemies);
            spec.HealthMultiplier *= multiple;
            waves.Add(spec);
        }

        LevelInput input = model.InputFor(index, d);
        DifficultyStats difficulty = model.Difficulties[d];
        var text = new StringBuilder();
        text.Append(Inv($"Boss paper lab: {key} difficulty {d} wave {waveNumber} at x{multiple:0.###}\n"));
        foreach ((string name, BalanceRules lens) in new[] { ("towers alone", rules.HumanTowersOnly()), ("with the hero", rules.HumanWithHero()) })
        {
            var loads = new List<WaveLoad>();
            List<WaveLine> lines = Ledger.Evaluate(input, waves, difficulty, lens, loads);
            WaveLoad load = loads[waveNumber - 1];
            WaveLine line = lines[waveNumber - 1];
            text.Append(Inv($"\n[{name}] utilisation {lens.Utilisation:0.###}, hero share {lens.HeroShare:0.###}\n"));
            text.Append(Inv($"  ledger's own purchase: {line.Purchase} with {line.Money} coins -> pressure {line.Pressure:0.00}\n"));
            text.Append(Inv($"  stream health {load.StreamHealth:0} (carried {load.CarriedHealth:0}), boss health {load.BossHealth:0} (body {load.BossArmour.Health:0}, armour {load.BossArmour.Armour:0})\n"));
            text.Append(Inv($"  span {load.Span:0.0} s, gate seconds {load.GateSeconds:0.0}, window {load.Window:0.0}; boss speed {load.BossSpeed:0.00}, boss fight {load.BossFight:0.0} s, boss window (gate hold share) {load.BossWindow:0.0} s\n"));
            text.Append(Inv($"  stream reach {load.StreamReach:0.00}, boss reach {load.BossReach:0.00}\n"));
            text.Append("  per gun (typical spot): cost | data dps | boss in reach s | ledger Single | Single at full data dps in reach (armour share kept) | Stream\n");
            float stream = 0f;
            float single = 0f;
            float full = 0f;
            for (int t = 0; t < input.Towers.Count; t++)
            {
                TowerStats tower = input.Towers[t];
                if (!tower.IsGun)
                {
                    continue;
                }

                for (int l = 0; l < tower.Levels.Length; l++)
                {
                    TowerBoost boost = input.BoostOf(tower.Id);
                    float range = tower.Levels[l].Range * boost.Range;
                    bool airOnly = Layer.Has(tower.Targets, Layer.Air) && !Layer.Has(tower.Targets, Layer.Ground);
                    SpotCover cover = input.Geometry != null ? input.Geometry.TypicalCover(range, airOnly, tower.Splash > 0f, Ledger.TypicalSpots) : SpotCover.Ideal(range);
                    Ledger.Option option = Ledger.Capacity(input, load, lens, t, l, cover);
                    float dps = tower.Levels[l].Interval > 0f ? tower.Levels[l].Damage * boost.Damage * boost.Rate / tower.Levels[l].Interval : 0f;
                    float inReach = load.BossSeconds((cover.Gate ? load.BossWindow : 0f) + cover.GroundPass / Math.Max(0.01f, load.BossSpeed), lens);
                    float atFull = Layer.Has(tower.Targets, Layer.Ground) ? dps * tower.GroundFactor * load.BossArmour.Share(tower.ArmourFactor) * inReach : 0f;
                    int count = standing.FindAll(s => s.TowerId == tower.Id && s.Level == l).Count;
                    text.Append(Inv($"    {tower.Id} L{l + 1}{(count > 0 ? " x" + count : "")}: {option.Cost} | {dps:0.0} | {inReach:0.0} (pass {cover.GroundPass:0.0} cells, gate {cover.Gate}) | {option.Single:0} | {atFull:0} | {option.Stream:0}\n"));
                    stream += count * option.Stream;
                    single += count * option.Single;
                    full += count * atFull;
                }
            }

            if (standing.Count > 0)
            {
                (float ground, float _) = Ledger.PressureOf(input, load, lens, standing);
                float crowd = load.StreamHealth > 0f && stream > 0f ? load.StreamHealth / (stream * load.StreamReach) : 0f;
                float boss = load.BossHealth > 0f && single > 0f ? load.BossHealth / (single * load.BossReach) : 0f;
                float bossFull = load.BossHealth > 0f && full > 0f ? load.BossHealth / (full * load.BossReach) : 0f;
                text.Append(Inv($"  the standing defence ({standing.Count} guns), towers only: crowd {crowd:0.00} + boss {boss:0.00}; the boss at full data dps in reach {bossFull:0.00}; the ledger's pressure with this lens (hero as the lens has him) {ground:0.00}\n"));
            }
        }

        string path = $"Tools/PlayTest/boss_paper_{label}.txt";
        File.WriteAllText(path, text.ToString());
        return "written " + path;
    }

    private static string Inv(FormattableString text) => FormattableString.Invariant(text);
}
