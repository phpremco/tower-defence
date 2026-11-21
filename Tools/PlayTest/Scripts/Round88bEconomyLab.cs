using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Editor;
using Game.Levels;

/// <summary>
/// Round 88b, the economy of the stored waves on paper (assets only, no Play Mode). Per level and difficulty every wave
/// as the game spawns it (the difficulty's count and health multipliers, the wave's own multiplier and boss scale) and
/// pays it (the bounty through the difficulty's income multiplier), the money a player can hold before each wave, and
/// the pressure index "health of the wave over the money available before it" with its trend through the level.
/// Run: bash Tools/PlayTest/stage.sh run Round88bEconomyLab.cs Round88bEconomyLab.Run   (writes Tools/PlayTest/r88b_economy.txt)
///      ... Round88bEconomyLab.Players  (r88b_players.txt: the permanent upgrades the model's player owns per cell, against a real save)
///      ... Round88bEconomyLab.Maps     (r88b_maps.txt: road, gates, rifts, first flyers and first plated wave per cell)
///      ... Round88bEconomyLab.Raids    (r88b_raids.txt: raids and peaks per cell from the scripts, and what candidate peak rules would move)
/// </summary>
public static class Round88bEconomyLab
{
    private const string EconomyPath = "Tools/PlayTest/r88b_economy.txt";
    private const string PlayersPath = "Tools/PlayTest/r88b_players.txt";
    private const string MapsPath = "Tools/PlayTest/r88b_maps.txt";
    private const string RaidsPath = "Tools/PlayTest/r88b_raids.txt";
    private const int CampaignLevels = 6;
    private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

    private sealed class Row
    {
        public int Number;
        public string Kind;
        public WaveComposer.Kind KindValue;
        public bool AfterBoss;
        public bool Boss;
        public bool Raid;
        public int Creatures;
        public float CrowdHealth;
        public float BossHealth;
        public float CrowdMoney;
        public float BossMoney;
        public float Multiplier;
        public float SimpleBefore;
        public float SimpleWithMines;
        public float PurseBefore;
        public float Window;
        public float LedgerPressure;
        public float Rate => Health / Math.Max(1f, Window) / Math.Max(1f, SimpleBefore) * 100f;
        public float Health => CrowdHealth + BossHealth;
        public float Money => CrowdMoney + BossMoney;
    }

    private sealed class Cell
    {
        public string Key;
        public int Difficulty;
        public int Start;
        public bool Harvest;
        public WaveScriptSpec Script;
        public int SecondRoad;
        public List<Row> Rows = new List<Row>();
    }

    public static string Run()
    {
        BalanceStages.Loaded loaded = BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        BalanceRules rules = model.Rules;
        var text = new StringBuilder();
        text.Append(Line($"Round 88b economy lab: the stored waves as spawned and as paid. Data stamp {loaded.Hash}.\n"));
        text.Append("Columns per wave: creatures as spawned | walk-in health of the crowd (body + armour) | of the level boss | coins the crowd pays (after the income multiplier) | the boss pays |\n");
        text.Append("  money before the wave, every earlier wave killed in full: start + kills (no harvester) | the same + the mines as the model books them | the model's KillPurse (kills paid when the creature reaches the first gate, mines in) |\n");
        text.Append("  pressure index = health of the wave / money before it (no harvester) | health per coin the wave itself pays |\n");
        text.Append("  the ledger's window for the wave in seconds (spawn span without the idle part of breathers, plus the time in reach) | rate index = health per second of that window per 100 coins held: the fire a coin must deliver |\n");
        text.Append("  the paper ledger's pressure for its own expected player (health over the capacity of the defence its purse buys; under one the towers alone hold).\n");
        text.Append(Line($"Rewards: Threat.Reward = MoneyPerThreat {rules.MoneyPerThreat:0.##} x threat^{rules.RewardExponent:0.##}, a boss x BossRewardFactor {rules.BossRewardFactor:0.##}; the factor is ALREADY in the stored reward of a boss (the lab recomputes it below), and the reward is a fixed number of coins: it does not move with the boss's bar as spawned.\n"));
        AppendBossRewards(text, model, rules);

        var cells = new List<Cell>();
        int levels = 0;
        foreach (MapNode node in loaded.Catalog.Nodes)
        {
            if (node.Level == null || levels >= CampaignLevels)
            {
                continue;
            }

            levels++;
            string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
            int index = model.IndexOf(key);
            WaveScriptSpec script = WaveScripts.ByKey(key);
            if (index < 0 || script == null)
            {
                continue;
            }

            // Kinds are read with the flyer floors the stored waves were sized with.
            model.BuildWaves(index, script, out _);
            for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
            {
                Cell cell = Measure(model, rules, index, key, d, script, node.Level.GetWaves(loaded.Catalog.Difficulties[d]));
                List<WaveLine> lines = BalanceLedger.Evaluate(model, index, node.Level, loaded.Catalog.Difficulties[d], d);
                for (int i = 0; i < cell.Rows.Count && i < lines.Count; i++)
                {
                    cell.Rows[i].Window = lines[i].Window;
                    cell.Rows[i].LedgerPressure = lines[i].Pressure;
                }

                cells.Add(cell);
                AppendCell(text, cell, model, index, d, script);
            }
        }

        AppendSummary(text, cells, model);
        File.WriteAllText(EconomyPath, text.ToString());
        return "written " + EconomyPath;
    }

    /// <summary>
    /// The permanent upgrades the balance model's player owns at each level and difficulty (Progression.StateAt: the
    /// bots play as that player), what they are worth, and next to them a real save: Remco's after clearing level 6
    /// on Easy (read on 2026-11-20: Base_Weapon 1, BlockadeHealth 2, PulseRifle_Damage 2, 112 research unspent, towers
    /// Gun Turret, Cannon, Laser, Flak), and the model's player had it spent only a share of its research.
    /// </summary>
    public static string Players()
    {
        BalanceStages.Loaded loaded = BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        BalanceRules rules = model.Rules;
        var text = new StringBuilder("Round 88b: the player the balance model expects per level and difficulty (the bots play as this player), against a real save.\n");
        text.Append(Line($"Research multipliers per difficulty {rules.ResearchMultipliers[0]:0.#} / {rules.ResearchMultipliers[1]:0.#} / {rules.ResearchMultipliers[2]:0.#} / {rules.ResearchMultipliers[3]:0.#}; paths: Easy = earlier levels on Easy; Medium = on Medium; Hard = on Medium and Hard (+ this level on Medium); Psycho = on all four (+ this level on the three below).\n"));
        text.Append("Per level: research reward (x1), reward tower.\n");
        for (int i = 0; i < model.Levels.Count && i < CampaignLevels; i++)
        {
            text.Append(Line($"  {model.Levels[i].Key}: research {model.Levels[i].Research}, reward tower {model.Levels[i].RewardTowerId}, perk {model.Levels[i].RewardHeroPerkId}, sells {string.Join(",", model.Levels[i].TowerIds)}\n"));
        }

        text.Append("\nPermanent upgrades (research): id | kind | per level | costs\n");
        foreach (UpgradeStats upgrade in model.Upgrades)
        {
            if (!upgrade.PaidInPower && upgrade.Continent == 0)
            {
                text.Append(Line($"  {upgrade.Id} | {upgrade.Kind} | {upgrade.BonusPerLevel:0.##} | {string.Join(" ", upgrade.Costs)}\n"));
            }
        }

        text.Append("\ncell | research earned | spent | upgrade levels | guns owned on the level's bar (damage x rate, range) | mean gun factor | gates x | hero x | base + | base weapon | perks\n");
        for (int i = 0; i < model.Levels.Count && i < CampaignLevels; i++)
        {
            for (int d = 0; d < model.Difficulties.Count; d++)
            {
                text.Append(Describe(model, i, model.Levels[i].Key + " " + DifficultyNames[d], model.PlayerAt(i, d)));
            }

            // The Easy player had it spent half or none of its research: what a cautious real player brings.
            var half = rules.Copy();
            half.ResearchMultipliers = Array.ConvertAll(rules.ResearchMultipliers, m => m * 0.5f);
            text.Append(Describe(model, i, model.Levels[i].Key + " Easy, half the research spent", Progression.StateAt(model.Levels, i, 0, model.StartingTowers, model.Upgrades, half)));
            text.Append(Describe(model, i, model.Levels[i].Key + " Easy, fresh save", model.PlayerAt(i, 0).Plain()));
        }

        text.Append("\nRemco's save after clearing level 6 on Easy: Base_Weapon 1, BlockadeHealth 2, PulseRifle_Damage 2; 112 research unspent; towers PulseRifle, Cannon, Laser, Rocket.\n");
        var save = new Dictionary<string, int> { { "Base_Weapon", 1 }, { "BlockadeHealth", 2 }, { "PulseRifle_Damage", 2 } };
        int spent = 0;
        float gates = 1f;
        float gun = 1f;
        foreach (UpgradeStats upgrade in model.Upgrades)
        {
            if (!save.TryGetValue(upgrade.Id, out int level))
            {
                continue;
            }

            for (int k = 0; k < level && k < upgrade.Costs.Length; k++)
            {
                spent += upgrade.Costs[k];
            }

            gates += upgrade.Kind == UpgradeKind.GateHealth ? level * upgrade.BonusPerLevel : 0f;
            gun += upgrade.Kind == UpgradeKind.TowerDamage ? level * upgrade.BonusPerLevel : 0f;
        }

        text.Append(Line($"  worth: research spent {spent}, Gun Turret damage x{gun:0.00} (Cannon, Laser, Flak x1.00), gates x{gates:0.00}, hero x1.00, base +0, base weapon 1; earned at least {spent + 112}.\n"));
        File.WriteAllText(PlayersPath, text.ToString());
        return "written " + PlayersPath;
    }

    private static string Describe(CampaignModel model, int index, string name, PlayerState player)
    {
        var levels = new List<string>();
        foreach (KeyValuePair<string, int> upgrade in player.UpgradeLevels)
        {
            levels.Add(upgrade.Key + " " + upgrade.Value);
        }

        levels.Sort(StringComparer.Ordinal);
        var guns = new List<string>();
        float sum = 0f;
        int count = 0;
        foreach (string id in model.Levels[index].TowerIds)
        {
            if (!player.OwnedTowers.Contains(id) || !model.Towers.TryGetValue(id, out TowerStats tower) || !tower.IsGun)
            {
                continue;
            }

            TowerBoost boost = player.Boosts.TryGetValue(id, out TowerBoost found) ? found : TowerBoost.None;
            guns.Add(FormattableString.Invariant($"{id} x{boost.Damage * boost.Rate:0.00} r{boost.Range:0.00}"));
            sum += boost.Damage * boost.Rate;
            count++;
        }

        return Line($"{name} | {player.ResearchEarned} | {player.ResearchSpent} | {string.Join(", ", levels)} | {string.Join(", ", guns)} | x{(count > 0 ? sum / count : 1f):0.00} | x{player.GateHealth:0.00} | x{player.HeroDamage:0.00} | +{player.BaseHealthBonus:0} | {player.BaseWeaponLevel} | {player.HeroPerks.Count}\n");
    }

    /// <summary>
    /// The map and the script of every cell side by side: the road, the gates and the rifts on it, the build spots
    /// that reach the approach to the first gate, the base, and per difficulty the first flyers against the purse a
    /// sky gun needs, the first plated wave, the raids and the boss.
    /// </summary>
    public static string Maps()
    {
        BalanceStages.Loaded loaded = BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        BalanceRules rules = model.Rules;
        var text = new StringBuilder("Round 88b: maps and scripts side by side.\n");
        int levels = 0;
        foreach (MapNode node in loaded.Catalog.Nodes)
        {
            if (node.Level == null || levels >= CampaignLevels)
            {
                continue;
            }

            levels++;
            LevelDefinition level = node.Level;
            string key = level.name.StartsWith("Level_") ? level.name.Substring(6) : level.name;
            int index = model.IndexOf(key);
            LevelFacts facts = model.Facts[index];
            LevelGeometry geometry = facts.Geometry;
            LevelShape shape = facts.Shape;
            LevelGeometry.Route main = geometry.GroundRoutes[0];
            text.Append(Line($"\n{key}: main road {main.Cells.Count} cells, defence gate at cell {main.GateIndex} ({(main.Cells.Count > 0 ? main.GateIndex / (float)main.Cells.Count : 0f):P0} of the road), ground routes {geometry.GroundRoutes.Count}, air routes {geometry.AirRoutes.Count}; base {facts.BaseHealth:0}, gate {facts.GateHealth:0} each, start money {facts.StartingMoney}, health scale {facts.HealthScale:0.##}\n"));
            text.Append("  roads:");
            foreach (LevelShape.Lane lane in shape.Lanes)
            {
                text.Append(Line($" path {lane.PathIndex} {lane.Length:0} cells (trunk share {lane.TrunkShare:0.00})"));
            }

            text.Append("\n  gates (progress along their road, cells from the spawn on the main road):");
            foreach (LevelShape.Gate gate in shape.Gates)
            {
                text.Append(Line($" path {gate.PathIndex} at {gate.Progress:0.00}{(gate.OnTrunk ? " trunk" : string.Empty)} ({gate.Progress * shape.MainLength:0})"));
            }

            text.Append("\n  rifts (join the main road at cell, share of the road left):");
            for (int n = 0; n < shape.NestRemaining.Count; n++)
            {
                text.Append(Line($" #{n + 1} at {(1f - shape.NestRemaining[n]) * shape.MainLength:0} ({shape.NestRemaining[n]:0.00} left)"));
            }

            TowerStats rifle = model.Towers.TryGetValue("PulseRifle", out TowerStats found) ? found : null;
            float reach = rifle != null && rifle.Levels.Length > 0 ? rifle.Levels[0].Range : 3f;
            int approach = 0;
            int early = 0;
            foreach (Game.Balance.Cell spot in geometry.Spots)
            {
                int[] hits = geometry.InReach(spot, reach, main);
                bool before = false;
                bool opening = false;
                foreach (int hit in hits)
                {
                    before |= hit <= main.GateIndex;
                    opening |= hit <= Math.Min(main.GateIndex, 12);
                }

                approach += before ? 1 : 0;
                early += opening ? 1 : 0;
            }

            text.Append(Line($"\n  build spots beside the roads {geometry.Spots.Count}; a Gun Turret (reach {reach:0.#}) on {approach} of them reaches the road before the defence gate, on {early} of them its first 12 cells\n"));
            WaveScriptSpec script = WaveScripts.ByKey(key);
            model.BuildWaves(index, script, out _);
            for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
            {
                LevelInput input = model.InputFor(index, d);
                DifficultyStats difficulty = model.Difficulties[d];
                int sky = -1;
                foreach (TowerStats tower in input.Towers)
                {
                    if (tower.Targets == Layer.Air && tower.Levels.Length > 0)
                    {
                        sky = sky < 0 ? tower.Levels[0].Cost : Math.Min(sky, tower.Levels[0].Cost);
                    }
                }

                WaveDefinition[] waves = level.GetWaves(loaded.Catalog.Difficulties[d]);
                float paid = 0f;
                int firstFlyer = 0, firstPlated = 0;
                float flyerPurse = 0f, flyerHealth = 0f, total = 0f, platedShare = 0f;
                var raids = new List<int>();
                var bosses = new List<string>();
                for (int i = 0; i < waves.Length; i++)
                {
                    WaveSpec wave = BalanceLedger.Spec(waves[i], model.Enemies);
                    float scale = difficulty.Health * input.HealthScale * wave.HealthMultiplier;
                    float money = 0f, air = 0f, armour = 0f, health = 0f;
                    foreach (GroupSpec group in wave.Groups)
                    {
                        EnemyStats enemy = group.Enemy;
                        int spawned = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.Count));
                        money += enemy.Reward * spawned * input.Income;
                        if (enemy.IsBoss)
                        {
                            float bossScale = wave.BossHealthScale > 0f ? wave.BossHealthScale : 1f;
                            bosses.Add(FormattableString.Invariant($"w{i + 1} {enemy.Id} {enemy.EffectiveHealth * scale * bossScale:0}"));
                            continue;
                        }

                        health += enemy.EffectiveHealth * scale * spawned;
                        air += enemy.Flies ? enemy.EffectiveHealth * scale * spawned : 0f;
                        armour += enemy.Armor * scale * spawned;
                    }

                    if (air > 0f && firstFlyer == 0)
                    {
                        firstFlyer = i + 1;
                        flyerPurse = input.StartingMoney + paid;
                        flyerHealth = air;
                    }

                    if (armour > 0f && firstPlated == 0)
                    {
                        firstPlated = i + 1;
                        platedShare = armour / Math.Max(1f, health);
                    }

                    if (wave.AirRaid)
                    {
                        raids.Add(i + 1);
                    }

                    total += health;
                    paid += money;
                }

                text.Append(Line($"  {DifficultyNames[d]}: guns {string.Join(",", input.Towers.ConvertAll(t => t.Id))}; crowd health {total:0}, kills {paid:0}; first flyers wave {firstFlyer} ({flyerHealth:0} hp in the air, money earned by then {flyerPurse:0}, sky gun {sky}); raids {string.Join(",", raids)}; first plated wave {firstPlated} (armour {platedShare:P0} of the wave); bosses {string.Join("; ", bosses)}\n"));
            }
        }

        File.WriteAllText(MapsPath, text.ToString());
        return "written " + MapsPath;
    }

    /// <summary>
    /// The raids and the peaks of every cell as the scripts give them (no stored wave is read): which waves are raids,
    /// their share of the level, raids in two waves running, every peak with its kind, how far it landed from its
    /// phase's place and whether both its neighbours are raids; and what three candidate peak rules would move.
    /// </summary>
    public static string Raids()
    {
        BalanceStages.Loaded loaded = BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        BalanceRules rules = model.Rules;
        var text = new StringBuilder("Round 88b: raids and peaks per cell, from the scripts.\n");
        text.Append("Row: one letter per wave (P plain, R rush, H heavy, A air raid; b boss wave, s surprise, * peak).\n");
        int levels = 0;
        foreach (MapNode node in loaded.Catalog.Nodes)
        {
            if (node.Level == null || levels >= CampaignLevels)
            {
                continue;
            }

            levels++;
            string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
            int index = model.IndexOf(key);
            WaveScriptSpec script = WaveScripts.ByKey(key);
            model.BuildWaves(index, script, out _);
            int secondRoad = model.Facts[index].SecondRoad;
            for (int d = 0; d < DifficultyNames.Length; d++)
            {
                int waves = script.Waves;
                bool Raid(int n) => n >= 1 && n <= waves && WaveComposer.KindOf(script, d, n) == WaveComposer.Kind.Air;
                bool Boss(int n) => n >= 1 && n <= waves && WaveComposer.BossOn(script, d, n) != null;
                bool Open(int n) => !Raid(n) && !Boss(n) && !Boss(n - 1);
                bool Marked(int n) => WaveComposer.KindOf(script, d, n) == WaveComposer.Kind.Heavy || WaveGrammar.SurpriseOn(script, model.Enemies, d, n, secondRoad, rules) != SurpriseKind.None;
                bool Between(int n) => Raid(n - 1) && Raid(n + 1);
                bool Rush(int n) => WaveComposer.KindOf(script, d, n) == WaveComposer.Kind.Rush;
                List<int> now = WaveSolver.PeaksOf(script, model.Enemies, d, secondRoad, rules);
                List<int> old = WaveFormula.Peaks(rules, waves, Open, Marked);
                List<int> noBetween = WaveFormula.Peaks(rules, waves, n => Open(n) && !Between(n), Marked);
                List<int> noRushBetween = WaveFormula.Peaks(rules, waves, n => Open(n) && !(Between(n) && Rush(n)), Marked);
                List<int> dropped = old.FindAll(n => !(Between(n) && Rush(n)));
                var row = new StringBuilder();
                var raids = new List<int>();
                var running = new List<int>();
                int ground = 0;
                for (int n = 1; n <= waves; n++)
                {
                    WaveComposer.Kind kind = WaveComposer.KindOf(script, d, n);
                    row.Append(kind == WaveComposer.Kind.Air ? 'A' : kind == WaveComposer.Kind.Rush ? 'R' : kind == WaveComposer.Kind.Heavy ? 'H' : 'P');
                    row.Append(Boss(n) ? "b" : string.Empty).Append(Marked(n) && kind != WaveComposer.Kind.Heavy ? "s" : string.Empty).Append(old.Contains(n) ? "*" : string.Empty).Append(' ');
                    if (Raid(n))
                    {
                        raids.Add(n);
                        if (Raid(n + 1))
                        {
                            running.Add(n);
                        }
                    }

                    ground += Open(n) ? 1 : 0;
                }

                var places = new List<string>();
                List<float> phases = WaveFormula.PeakPhases(rules, waves);
                foreach (int peak in old)
                {
                    float nearest = float.MaxValue;
                    foreach (float phase in phases)
                    {
                        nearest = Math.Min(nearest, Math.Abs(peak - (1f + phase * (waves - 1))));
                    }

                    places.Add(FormattableString.Invariant($"{peak} {WaveComposer.KindOf(script, d, peak)}{(Between(peak) ? " BETWEEN RAIDS" : string.Empty)} (nearest place {nearest:0.0} waves off)"));
                }

                text.Append(Line($"\n{key} {DifficultyNames[d]}: {row}\n"));
                text.Append(Line($"  raids {string.Join(",", raids)} ({raids.Count} of {waves} = {raids.Count / (float)waves:P0}), two running from {string.Join(",", running)}; open ground waves {ground}; peaks: {string.Join("; ", places)}\n"));
                text.Append(Line($"  the rule in code now: {string.Join(",", now)}{(Same(now, old) ? " (as round 87)" : " (round 87: " + string.Join(",", old) + ")")}\n"));
                if (!Same(old, noBetween) || !Same(old, noRushBetween) || !Same(old, dropped))
                {
                    text.Append(Line($"  candidates: no peak between two raids, placed anew {string.Join(",", noBetween)} | no peak on a rush between two raids, placed anew {string.Join(",", noRushBetween)} | a rush between two raids carries no peak (not placed anew) {string.Join(",", dropped)}\n"));
                }
            }
        }

        File.WriteAllText(RaidsPath, text.ToString());
        return "written " + RaidsPath;
    }

    private static bool Same(List<int> a, List<int> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void AppendBossRewards(StringBuilder text, CampaignModel model, BalanceRules rules)
    {
        text.Append("Boss bounties (book): id | health | armour | stored reward | Threat.Reward now | the same boss without armour | with a fifth of the body as armour (round 86)\n");
        foreach (KeyValuePair<string, EnemyStats> pair in model.Enemies)
        {
            EnemyStats boss = pair.Value;
            if (!boss.IsBoss || boss.IsContinentBoss)
            {
                continue;
            }

            float armour = boss.Armor;
            int now = Threat.Reward(boss, rules);
            boss.Armor = 0f;
            int bare = Threat.Reward(boss, rules);
            boss.Armor = (float)Math.Round(boss.Health * 0.2f);
            int fifth = Threat.Reward(boss, rules);
            boss.Armor = armour;
            text.Append(Line($"  {boss.Id} | {boss.Health:0} | {boss.Armor:0} | {boss.Reward} | {now} | {bare} | {fifth}\n"));
        }
    }

    private static Cell Measure(CampaignModel model, BalanceRules rules, int index, string key, int d, WaveScriptSpec script, WaveDefinition[] definitions)
    {
        LevelInput input = model.InputFor(index, d);
        DifficultyStats difficulty = model.Difficulties[d];
        var cell = new Cell { Key = key, Difficulty = d, Start = input.StartingMoney, Harvest = input.Harvest && input.MineCount > 0, Script = script, SecondRoad = model.Facts[index].SecondRoad };
        var purse = new KillPurse(input, difficulty, rules);
        float paid = 0f;
        int count = definitions.Length;
        for (int i = 0; i < count; i++)
        {
            WaveSpec wave = BalanceLedger.Spec(definitions[i], model.Enemies);
            var row = new Row
            {
                Number = i + 1,
                Kind = WaveComposer.KindOf(script, d, i + 1).ToString(),
                KindValue = WaveComposer.KindOf(script, d, i + 1),
                AfterBoss = WaveComposer.BossOn(script, d, i) != null,
                Boss = WaveComposer.BossOn(script, d, i + 1) != null,
                Raid = wave.AirRaid,
                Multiplier = wave.HealthMultiplier,
            };
            float scale = difficulty.Health * input.HealthScale * wave.HealthMultiplier;
            float bossScale = wave.BossHealthScale > 0f ? wave.BossHealthScale : 1f;
            foreach (GroupSpec group in wave.Groups)
            {
                EnemyStats enemy = group.Enemy;
                if (enemy == null)
                {
                    continue;
                }

                int spawned = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.Count));
                float own = enemy.IsBoss && !enemy.IsContinentBoss ? bossScale : 1f;
                float health = enemy.EffectiveHealth * scale * own * difficulty.ShareFor(enemy) * spawned;
                float money = enemy.Reward * spawned * input.Income;
                row.Creatures += spawned;
                if (enemy.IsBoss)
                {
                    row.BossHealth += health;
                    row.BossMoney += money;
                }
                else
                {
                    row.CrowdHealth += health;
                    row.CrowdMoney += money;
                }
            }

            int mines = Ledger.MoneyAt(input, i, count, 0f, rules) - input.StartingMoney;
            row.SimpleBefore = input.StartingMoney + paid;
            row.SimpleWithMines = row.SimpleBefore + mines;
            purse.Begin(wave);
            row.PurseBefore = purse.Money(i, count);
            purse.Add(wave, default);
            paid += row.Money;
            cell.Rows.Add(row);
        }

        return cell;
    }

    private static void AppendCell(StringBuilder text, Cell cell, CampaignModel model, int index, int d, WaveScriptSpec script)
    {
        LevelInput input = model.InputFor(index, d);
        DifficultyStats difficulty = model.Difficulties[d];
        text.Append(Line($"\n{cell.Key} {DifficultyNames[d]}: start {cell.Start} (x{difficulty.StartMoney:0.##}), income x{difficulty.Income:0.##}, count x{difficulty.Count:0.##}, health x{difficulty.Health:0.##}, script budget {script.Budget:0.#} -> {script.FinalBudget:0.#} over {script.Waves} waves; mines {input.MineCount} x {input.MineStock}, harvester {(cell.Harvest ? "offered (" + input.HarvesterCost + " coins)" : "not offered: the ore pays nothing")}\n"));
        text.Append("  wave kind         mult  creat   crowdHP   bossHP  crowd$  boss$ | $before  +mines   purse | index  HP/coin | window   rate  ledger\n");
        foreach (Row row in cell.Rows)
        {
            string kind = row.Kind + (row.Boss ? "+boss" : string.Empty) + (row.Raid ? " raid" : string.Empty);
            text.Append(Line($"  {row.Number,4} {kind,-12} {row.Multiplier,5:0.00} {row.Creatures,6} {row.CrowdHealth,9:0} {row.BossHealth,8:0} {row.CrowdMoney,7:0} {row.BossMoney,6:0} | {row.SimpleBefore,7:0} {row.SimpleWithMines,7:0} {row.PurseBefore,7:0} | {row.Health / Math.Max(1f, row.SimpleBefore),5:0.00} {row.Health / Math.Max(1f, row.Money),7:0.0} | {row.Window,6:0} {row.Rate,6:0.00} {row.LedgerPressure,7:0.00}\n"));
        }

        Trend all = TrendOf(cell.Rows, r => true, r => r.Health / Math.Max(1f, r.SimpleBefore));
        Trend plain = TrendOf(cell.Rows, r => !r.Boss, r => r.Health / Math.Max(1f, r.SimpleBefore));
        Trend ground = TrendOf(cell.Rows, r => !r.Boss && !r.Raid, r => r.Health / Math.Max(1f, r.SimpleBefore));
        Trend mined = TrendOf(cell.Rows, r => !r.Boss, r => r.Health / Math.Max(1f, r.SimpleWithMines));
        Trend purse = TrendOf(cell.Rows, r => !r.Boss, r => r.Health / Math.Max(1f, r.PurseBefore));
        Trend rate = TrendOf(cell.Rows, r => !r.Boss && !r.Raid, r => r.Rate);
        Trend ledger = TrendOf(cell.Rows, r => !r.Boss && !r.Raid, r => r.LedgerPressure);
        text.Append(Line($"  pressure index, log-linear fit over the phase (x from the first to the last wave) and last third over first third (geometric means):\n"));
        text.Append(Line($"    all waves {all}; without boss waves {plain}; ground waves without a boss {ground}; without boss waves, mines in {mined}; without boss waves, KillPurse {purse}\n"));
        text.Append(Line($"    ground waves without a boss: rate index {rate}; the ledger's pressure {ledger}\n"));
        Totals totals = TotalsOf(cell);
        text.Append(Line($"  money: start {cell.Start} + kills {totals.Kills:0} = {cell.Start + totals.Kills:0}; in the last quarter of the waves {totals.LastQuarter / totals.Kills:P0} of the kills, the LAST wave alone {totals.LastWave / totals.Kills:P0} (nobody can spend it); bosses {totals.Bosses / totals.Kills:P0}, of which bosses before the last wave {totals.MidBosses:0} coins ({totals.MidBosses / totals.Kills:P0}); start is {cell.Start / (cell.Start + totals.Kills):P0} of all the money; health per coin over the level {totals.Health / (cell.Start + totals.Kills):0.0}\n"));
    }

    private readonly struct Trend
    {
        public Trend(float slope, float thirds)
        {
            Slope = slope;
            Thirds = thirds;
        }

        public float Slope { get; }

        public float Thirds { get; }

        public override string ToString() => FormattableString.Invariant($"x{Slope:0.00} (thirds x{Thirds:0.00})");
    }

    // The log-linear fit of an index over the wave's phase, as the factor from the first wave to the last, and the
    // geometric mean of the last third of the level over that of the first third.
    private static Trend TrendOf(List<Row> rows, Func<Row, bool> include, Func<Row, float> index)
    {
        int waves = rows.Count;
        double sx = 0, sy = 0, sxx = 0, sxy = 0;
        int n = 0;
        double first = 0, last = 0;
        int firstCount = 0, lastCount = 0;
        foreach (Row row in rows)
        {
            float value = index(row);
            if (!include(row) || value <= 0f)
            {
                continue;
            }

            double x = WaveFormula.Phase(row.Number, waves);
            double y = Math.Log(value);
            sx += x;
            sy += y;
            sxx += x * x;
            sxy += x * y;
            n++;
            if (x < 1.0 / 3.0)
            {
                first += y;
                firstCount++;
            }
            else if (x > 2.0 / 3.0)
            {
                last += y;
                lastCount++;
            }
        }

        double denominator = n * sxx - sx * sx;
        double slope = n > 1 && denominator > 0 ? (n * sxy - sx * sy) / denominator : 0;
        double thirds = firstCount > 0 && lastCount > 0 ? Math.Exp(last / lastCount - first / firstCount) : 1;
        return new Trend((float)Math.Exp(slope), (float)thirds);
    }

    private sealed class Totals
    {
        public float Kills;
        public float LastQuarter;
        public float LastWave;
        public float Bosses;
        public float MidBosses;
        public float Health;
    }

    private static Totals TotalsOf(Cell cell)
    {
        var totals = new Totals();
        int waves = cell.Rows.Count;
        foreach (Row row in cell.Rows)
        {
            totals.Kills += row.Money;
            totals.Health += row.Health;
            totals.Bosses += row.BossMoney;
            if (row.Number > waves - Math.Max(1, (int)Math.Round(waves / 4f)))
            {
                totals.LastQuarter += row.Money;
            }

            if (row.Number == waves)
            {
                totals.LastWave += row.Money;
            }
            else
            {
                totals.MidBosses += row.BossMoney;
            }
        }

        return totals;
    }

    private static void AppendSummary(StringBuilder text, List<Cell> cells, CampaignModel model)
    {
        text.Append("\n==== Summary per cell ====\n");
        text.Append("cell | start | kills | total | last quarter | last wave | bosses | mid bosses (coins) | index trend without boss waves: fit, thirds | ground only: fit, thirds | index at first / middle / last third (geometric mean, no boss waves) | HP per coin of the wave itself: first third / last third | rate index (ground, no boss): fit, thirds; first / middle / last third | ledger pressure (ground, no boss): fit, thirds; first / middle / last third\n");
        foreach (Cell cell in cells)
        {
            Totals totals = TotalsOf(cell);
            Trend plain = TrendOf(cell.Rows, r => !r.Boss, r => r.Health / Math.Max(1f, r.SimpleBefore));
            Trend ground = TrendOf(cell.Rows, r => !r.Boss && !r.Raid, r => r.Health / Math.Max(1f, r.SimpleBefore));
            float[] thirds = Thirds(cell.Rows, r => !r.Boss, r => r.Health / Math.Max(1f, r.SimpleBefore));
            float[] own = Thirds(cell.Rows, r => !r.Boss, r => r.Health / Math.Max(1f, r.Money));
            Trend rate = TrendOf(cell.Rows, r => !r.Boss && !r.Raid, r => r.Rate);
            float[] rates = Thirds(cell.Rows, r => !r.Boss && !r.Raid, r => r.Rate);
            Trend ledger = TrendOf(cell.Rows, r => !r.Boss && !r.Raid, r => r.LedgerPressure);
            float[] ledgers = Thirds(cell.Rows, r => !r.Boss && !r.Raid, r => r.LedgerPressure);
            text.Append(Line($"{cell.Key} {DifficultyNames[cell.Difficulty]} | {cell.Start} | {totals.Kills:0} | {cell.Start + totals.Kills:0} | {totals.LastQuarter / totals.Kills:P0} | {totals.LastWave / totals.Kills:P0} | {totals.Bosses / totals.Kills:P0} | {totals.MidBosses:0} | {plain} | {ground} | {thirds[0]:0.00} / {thirds[1]:0.00} / {thirds[2]:0.00} | {own[0]:0.0} / {own[2]:0.0} | {rate}; {rates[0]:0.00} / {rates[1]:0.00} / {rates[2]:0.00} | {ledger}; {ledgers[0]:0.00} / {ledgers[1]:0.00} / {ledgers[2]:0.00}\n"));
        }

        text.Append("\n==== What other constants would do (paper: the formula's factors on the same bodies, the bounties re-summed) ====\n");
        AppendScenarios(text, cells, model);
    }

    private static float[] Thirds(List<Row> rows, Func<Row, bool> include, Func<Row, float> index)
    {
        var sums = new double[3];
        var counts = new int[3];
        foreach (Row row in rows)
        {
            float value = index(row);
            if (!include(row) || value <= 0f)
            {
                continue;
            }

            float phase = WaveFormula.Phase(row.Number, rows.Count);
            int third = phase < 1f / 3f ? 0 : phase > 2f / 3f ? 2 : 1;
            sums[third] += Math.Log(value);
            counts[third]++;
        }

        var result = new float[3];
        for (int i = 0; i < 3; i++)
        {
            result[i] = counts[i] > 0 ? (float)Math.Exp(sums[i] / counts[i]) : 0f;
        }

        return result;
    }

    /// <summary>One "what if" of the paper: other formula constants for the health, other shares for the money.</summary>
    private sealed class Scenario
    {
        public string Name;
        public BalanceRules Rules;
        public float[] Income = { 1f, 1f, 1f, 1f };
        public float[] StartShare = { 1f, 1f, 1f, 1f };
        public float MidBossBounty = 1f;
        public float LastBossBounty = 1f;
    }

    private static List<Scenario> Scenarios(BalanceRules rules)
    {
        BalanceRules Ramp(float start, float end)
        {
            BalanceRules trial = rules.Copy();
            trial.RampStart = start;
            trial.RampEnd = end;
            return trial;
        }

        BalanceRules afterBoss = rules.Copy();
        afterBoss.AfterBossFactor = 1f;
        BalanceRules latePeaks = rules.Copy();
        latePeaks.PeakPhases = new[] { 0.5f, 0.85f };
        latePeaks.LongLevelPeakPhase = 0.68f;
        BalanceRules combo = Ramp(0.6f, 1.68f);
        combo.AfterBossFactor = 0.9f;
        combo.PeakPhases = new[] { 0.5f, 0.85f };
        combo.LongLevelPeakPhase = 0.68f;
        float third = 1f / Math.Max(1f, rules.BossRewardFactor);
        return new List<Scenario>
        {
            new Scenario { Name = "as stored", Rules = rules },
            new Scenario { Name = "ramp 0.65..1.55", Rules = Ramp(0.65f, 1.55f) },
            new Scenario { Name = "ramp 0.6..1.68", Rules = Ramp(0.6f, 1.68f) },
            new Scenario { Name = "ramp 0.55..1.82", Rules = Ramp(0.55f, 1.82f) },
            new Scenario { Name = "ramp 0.5..2", Rules = Ramp(0.5f, 2f) },
            new Scenario { Name = "no breather after a boss (AfterBossFactor 1)", Rules = afterBoss },
            new Scenario { Name = "peaks later (PeakPhases 0.5 / 0.85, long level 0.68)", Rules = latePeaks },
            new Scenario { Name = "a boss before the last wave pays the ordinary rule (factor 1, not 3)", Rules = rules, MidBossBounty = third },
            new Scenario { Name = "a boss before the last wave pays factor 1.5", Rules = rules, MidBossBounty = 1.5f * third },
            new Scenario { Name = "every boss pays factor 1", Rules = rules, MidBossBounty = third, LastBossBounty = third },
            new Scenario { Name = "income Hard and Psycho x1.0 (now x1.1)", Rules = rules, Income = new[] { 1f, 1f, 1f / 1.1f, 1f / 1.1f } },
            new Scenario { Name = "income Hard x1.0, Psycho x0.9", Rules = rules, Income = new[] { 1f, 1f, 1f / 1.1f, 0.9f / 1.1f } },
            new Scenario { Name = "Psycho starts with x2 (now x3)", Rules = rules, StartShare = new[] { 1f, 1f, 1f, 2f / 3f } },
            new Scenario { Name = "Psycho starts with x1.5", Rules = rules, StartShare = new[] { 1f, 1f, 1f, 0.5f } },
            new Scenario { Name = "together: ramp 0.6..1.68, breather 0.9, peaks later, mid boss factor 1, income Hard and Psycho x1.0", Rules = combo, MidBossBounty = third, Income = new[] { 1f, 1f, 1f / 1.1f, 1f / 1.1f } },
        };
    }

    // The rate index of every ground wave without a boss under a scenario: the wave's health re-read on the trial
    // formula (the same bodies, the level's scalar and the difficulty's factor cancel), the money before it re-summed.
    private static List<(float Phase, float Rate)> Rates(Cell cell, Scenario scenario, BalanceRules rules, CampaignModel model, out float total, out float beforeLast)
    {
        int d = cell.Difficulty;
        int waves = cell.Rows.Count;
        List<int> stored = WaveSolver.PeaksOf(cell.Script, model.Enemies, d, cell.SecondRoad, rules);
        List<int> trial = ReferenceEquals(scenario.Rules, rules) ? stored : WaveSolver.PeaksOf(cell.Script, model.Enemies, d, cell.SecondRoad, scenario.Rules);
        var rates = new List<(float, float)>();
        float money = (float)Math.Round(cell.Start * scenario.StartShare[d]);
        beforeLast = money;
        foreach (Row row in cell.Rows)
        {
            float phase = WaveFormula.Phase(row.Number, waves);
            float was = WaveFormula.WalkIn(rules, d, 1f, phase, row.KindValue, row.Boss, row.AfterBoss, stored.Contains(row.Number));
            float now = WaveFormula.WalkIn(scenario.Rules, d, 1f, phase, row.KindValue, row.Boss, row.AfterBoss, trial.Contains(row.Number));
            float health = row.CrowdHealth * now / was + row.BossHealth;
            if (!row.Boss && !row.Raid)
            {
                rates.Add((phase, health / Math.Max(1f, row.Window) / Math.Max(1f, money) * 100f));
            }

            beforeLast = money;
            money += (row.CrowdMoney + row.BossMoney * (row.Number == waves ? scenario.LastBossBounty : scenario.MidBossBounty)) * scenario.Income[d];
        }

        total = money;
        return rates;
    }

    private static void AppendScenarios(StringBuilder text, List<Cell> cells, CampaignModel model)
    {
        BalanceRules rules = model.Rules;
        text.Append("Rate index = health per second of the wave's window per 100 coins held before it; ground waves without a boss; thirds of the level by phase (geometric means over the waves, then over the six levels).\n");
        text.Append("scenario | difficulty | rate index first / middle / last third | last over first | last over middle | cells of 6 whose last third is under the middle | money over the level (mean of the six levels) | held before the last wave\n");
        var perCell = new StringBuilder("\nPer cell, last third over middle third of the rate index, per scenario (in the order above):\n");
        var lines = new Dictionary<string, List<string>>();
        foreach (Scenario scenario in Scenarios(rules))
        {
            for (int d = 0; d < DifficultyNames.Length; d++)
            {
                var sums = new double[3];
                int count = 0, falling = 0;
                float totals = 0f, held = 0f;
                foreach (Cell cell in cells)
                {
                    if (cell.Difficulty != d)
                    {
                        continue;
                    }

                    List<(float Phase, float Rate)> rates = Rates(cell, scenario, rules, model, out float total, out float beforeLast);
                    var logs = new double[3];
                    var counts = new int[3];
                    foreach ((float phase, float rate) in rates)
                    {
                        int third = phase < 1f / 3f ? 0 : phase > 2f / 3f ? 2 : 1;
                        logs[third] += Math.Log(Math.Max(1e-6f, rate));
                        counts[third]++;
                    }

                    for (int t = 0; t < 3; t++)
                    {
                        logs[t] = counts[t] > 0 ? logs[t] / counts[t] : 0;
                        sums[t] += logs[t];
                    }

                    count++;
                    falling += logs[2] < logs[1] ? 1 : 0;
                    totals += total;
                    held += beforeLast;
                    string name = cell.Key + " " + DifficultyNames[d];
                    if (!lines.TryGetValue(name, out List<string> list))
                    {
                        lines[name] = list = new List<string>();
                    }

                    list.Add(FormattableString.Invariant($"x{Math.Exp(logs[2] - logs[1]):0.00}"));
                }

                if (count == 0)
                {
                    continue;
                }

                text.Append(Line($"{scenario.Name} | {DifficultyNames[d]} | {Math.Exp(sums[0] / count):0.00} / {Math.Exp(sums[1] / count):0.00} / {Math.Exp(sums[2] / count):0.00} | x{Math.Exp((sums[2] - sums[0]) / count):0.00} | x{Math.Exp((sums[2] - sums[1]) / count):0.00} | {falling} | {totals / count:0} | {held / count:0}\n"));
            }
        }

        foreach (Cell cell in cells)
        {
            string name = cell.Key + " " + DifficultyNames[cell.Difficulty];
            perCell.Append(name).Append(": ").Append(string.Join(" ", lines[name])).Append('\n');
        }

        text.Append(perCell);
    }

    private static string Line(FormattableString text) => FormattableString.Invariant(text);
}
