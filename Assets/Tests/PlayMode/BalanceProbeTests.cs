using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// A bot that plays like an average player: it holds the line at the first gate every road passes (towers around
    /// it, the hero on its post), judges its own towers with the ledger (<see cref="Game.Balance.Ledger.PressureOf"/>),
    /// keeps the ground holding first, saves for an air gun in time for the first flyer wave, and spends the rest on the
    /// affordable step (a new tower or an upgrade) that lowers the worse pressure most per coin. It builds only the towers a player owns at that point
    /// of the campaign (the starting set plus earlier rewards), uses no drones or gadgets, and writes a per-wave trace
    /// beside the ledger's line to Logs/probe_trace.txt. The smoke run must clear Shard Ridge and Crysium Vein on Medium.
    /// </summary>
    public sealed class BalanceProbeTests
    {
        private const float SimulationSpeed = 20f;
        private const float TimeoutSeconds = 900f;
        // No frame may jump further than this in game time. At twenty times speed a slow editor frame became one to three
        // game seconds: creatures skipped past gates, guns lost shots, and the bot lost to the frame rate, not to the waves.
        // The time scale follows the editor's frame rate so a slow frame only slows the run down.
        private const float MaxFrameSeconds = 0.2f;
        private const float MinFrameSeconds = 0.0001f;
        private const float BuyIntervalSeconds = 1f;
        // The bot judges its towers against the wave being fought and the next one: on a long road the creatures of a
        // wave reach the gate while the next one spawns.
        private const int Horizon = 1;
        // Above this ledger ground pressure on the waves at hand, the ground comes before any saving. One is calibrated as
        // "the bot barely holds", but the bot held Shard Ridge on Medium with its own towers at 1.1 to 1.3 right after a
        // purchase (the model is cautious about gate hold and the hero), and a ceiling of 0.95 kept it from ever saving.
        private const float GroundCeiling = 1.25f;
        // Kill money arrives late and not all of it, so the saving starts when the income up to the raid is this much
        // more than the air gun's price.
        private const float ReserveMargin = 2f;
        // A step that does not move the bottleneck still helps a little if it lifts the other side.
        private const float OtherSideWeight = 0.01f;
        // How much better per coin a step one wave of kills away must be before the bot waits for it.
        private const float WaitRatio = 1.5f;
        private const float NoThreatCover = 1e6f;
        private const float RouteSampleCells = 0.5f;
        // A little inside the Flak's first range (5.5), so the shells reach the route.
        private const float AirReachCells = 5f;
        private const int SecondLineEvery = 4;
        private const float BesideRoadCells = 2f;
        private const string FilterPath = "Tools/PlayTest/probe_arg.txt";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string TracePath = "Logs/probe_trace.txt";

        // The smoke run: Shard Ridge (first flyers) and Crysium Vein (the last level of the slice), on Medium.
        private static readonly int[] SmokeLevels = { 2, 5 };
        private const int SmokeDifficulty = 1;

        // Shard Ridge and Fault Line: the levels with packed air raids.
        private static readonly int[] AirRaidLevels = { 2, 3 };

        // Where the gun-only build must struggle. Fault Line is checked for the Flak bot's win only: its road joins the
        // trunk late, so every gun stands near the base where the flight routes meet, and a gun-only defence downs every
        // flyer, at the raids' own size and at 1.6 or 1.8 times it (2026-11-04), so its raids stay as they were.
        private static readonly int[] StruggleLevels = { 2 };

        // Every run appends its trace, so one test's file holds all of its runs.
        [SetUp]
        public void ClearTrace()
        {
            if (System.IO.File.Exists(TracePath))
            {
                System.IO.File.Delete(TracePath);
            }
        }

        [UnityTest]
        [NUnit.Framework.Timeout(3600000)]
        public IEnumerator Smoke_GreedyBot_ClearsShardRidgeAndCrysiumVein_OnMedium()
        {
            var runs = new List<(int, int)>();
            foreach (int level in ExistingLevels(true))
            {
                runs.Add((level, SmokeDifficulty));
            }

            var results = new List<ProbeResult>();
            yield return Probe(runs, false, results);
            Assert.IsNotEmpty(results);
            foreach (ProbeResult result in results)
            {
                Assert.AreEqual(LevelOutcome.Won, result.Outcome, $"{result.Name}: lost at wave {result.Wave}, see {TracePath}");
            }
        }

        // S4 curator decision 8, narrowed on 2026-11-04: the packed Skyray and bat raids of Shard Ridge and Fault Line are
        // the Flak's job from Medium on. A naive player who builds only the gun that hits both ground and air must struggle
        // there on Medium (more flyers reach the base, or the base takes more damage, than with a Flak; losing is not needed),
        // while the same bot with a Flak before the air waves holds the level. Easy has no struggle requirement.
        [UnityTest]
        [NUnit.Framework.Timeout(3600000)]
        public IEnumerator Medium_GunOnlyBot_StrugglesInThePackedAirRaids()
        {
            foreach (int level in AirRaidLevels)
            {
                var naive = new List<ProbeResult>();
                yield return Probe(new List<(int, int)> { (level, SmokeDifficulty) }, true, naive);
                var flak = new List<ProbeResult>();
                yield return Probe(new List<(int, int)> { (level, SmokeDifficulty) }, false, flak);
                ProbeResult gun = naive[0];
                ProbeResult mixed = flak[0];
                Debug.Log($"[Probe] air check {gun.Name}: gun only {gun.Outcome} at wave {gun.Wave}, flyers down {gun.FlyersKilled}/{gun.FlyersSpawned}, base hits {gun.FlyerBaseHits}, base damage {gun.BaseDamage:0}; with flak {mixed.Outcome}, flyers down {mixed.FlyersKilled}/{mixed.FlyersSpawned}, base hits {mixed.FlyerBaseHits}, base damage {mixed.BaseDamage:0}");
                Assert.AreEqual(LevelOutcome.Won, mixed.Outcome, $"{mixed.Name}: the bot with a Flak must hold Medium");
                if (System.Array.IndexOf(StruggleLevels, level) < 0)
                {
                    continue;
                }

                bool struggles = gun.Outcome == LevelOutcome.Lost || gun.FlyerBaseHits > mixed.FlyerBaseHits || gun.BaseDamage > mixed.BaseDamage;
                Assert.IsTrue(struggles, $"{gun.Name}: a gun-only build does not struggle with the air raids ({gun.FlyerBaseHits} flyer base hits and {gun.BaseDamage:0} base damage against {mixed.FlyerBaseHits} and {mixed.BaseDamage:0} with a Flak)");
            }
        }

        [UnityTest]
        [Category("Full")]
        [Explicit("Every level on every difficulty; run on request with the Full category.")]
        [NUnit.Framework.Timeout(7200000)]
        public IEnumerator Full_GreedyBot_PlaysEveryLevel_WithoutErrors()
        {
            var runs = new List<(int, int)>();
            foreach (int level in ExistingLevels(false))
            {
                for (int difficulty = 0; difficulty < 4; difficulty++)
                {
                    if (Wanted("levels", level) && Wanted("difficulties", difficulty))
                    {
                        runs.Add((level, difficulty));
                    }
                }
            }

            yield return Probe(runs, false, new List<ProbeResult>());
        }

        /// <summary>Where the bot builds: ground guns around the defence gate, air guns under the flight routes.</summary>
        private sealed class BuildSpots
        {
            public BuildSpots(List<Vector2Int> ground, List<Vector2Int> air)
            {
                Ground = ground;
                Air = air;
            }

            public List<Vector2Int> Ground { get; }

            public List<Vector2Int> Air { get; }
        }

        /// <summary>The ledger's view of one run: its lines for the trace, and the wave loads and level input the bot judges its own towers with.</summary>
        private sealed class LedgerView
        {
            public readonly List<Game.Balance.WaveLine> Lines = new List<Game.Balance.WaveLine>();
            public readonly List<Game.Balance.WaveLoad> Loads = new List<Game.Balance.WaveLoad>();
            public readonly HashSet<string> SkyGuns = new HashSet<string>();
            public Game.Balance.LevelInput Input;
            public Game.Balance.BalanceRules Rules;

            public int Count => Loads.Count;

            public string LineFor(int wave) => wave >= 1 && wave <= Lines.Count ? Lines[wave - 1].ToString() : string.Empty;
        }

        /// <summary>How one bot run ended.</summary>
        private sealed class ProbeResult
        {
            public string Name;
            public LevelOutcome Outcome;
            public int Wave;
            public int FlyersSpawned;
            public int FlyersKilled;
            public int FlyerBaseHits;
            public float BaseDamage;
        }

        // Plays each (node, difficulty) run; gunOnly limits the bot to the owned guns that hit both ground and air.
        private IEnumerator Probe(List<(int Level, int Difficulty)> runs, bool gunOnly, List<ProbeResult> results)
        {
            var report = new List<string>();
            var traces = new List<string>();
            // Engine warnings (the graphics ring buffer fills at twenty times speed with a swarm on screen) are not
            // balance findings; only errors and exceptions from the game fail the run.
            LogAssert.ignoreFailingMessages = true;
            int errors = 0;
            Application.LogCallback onLog = (condition, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errors++;
                    Debug.Log("[Probe] error: " + condition);
                }
            };
            Application.logMessageReceived += onLog;
            foreach ((int level, int difficulty) in runs)
            {
                SceneManager.LoadScene(SceneNames.Boot);
                yield return null;
                yield return null;

                var catalog = Services.Get<CampaignCatalog>();
                LevelDefinition definition = catalog.Nodes[level].Level;
                List<string> owned = OwnedTowers(catalog, level);
                if (gunOnly)
                {
                    owned.RemoveAll(id => !HitsBoth(definition, id));
                }

                Services.Register(new LevelSession(definition, catalog.Difficulties[difficulty]));
                SceneManager.LoadScene(SceneNames.Level);
                yield return null;
                yield return null;

                LedgerView ledger = LedgerFor(level, difficulty);
                foreach (TowerDefinition tower in definition.AvailableTowers)
                {
                    if (AirOnly(tower))
                    {
                        ledger.SkyGuns.Add(tower.Id);
                    }
                }

                Time.timeScale = SimulationSpeed;
                float started = Time.realtimeSinceStartup;
                float nextBuy = 0f;
                LevelContext context = null;
                BuildSpots spots = null;
                var towers = new List<Tower>();
                int upgrades = 0;
                ProbeTrace trace = null;
                int notedWave = -1;
                string note = string.Empty;

                while (Time.realtimeSinceStartup - started < TimeoutSeconds)
                {
                    if (context == null && Services.TryGet(out LevelContext found))
                    {
                        context = found;
                        spots = new BuildSpots(RankBuildSpots(definition), RankAirSpots(definition));
                        int post = DefenceGate(definition);
                        if (context.Hero != null && post >= 0)
                        {
                            context.Hero.MoveTo(post);
                        }

                        trace = new ProbeTrace(context);
                    }

                    if (context != null)
                    {
                        if (context.Outcome != LevelOutcome.Running)
                        {
                            break;
                        }

                        if (context.WaveNumber != notedWave)
                        {
                            notedWave = context.WaveNumber;
                            note = ledger.LineFor(notedWave) + BotNote(ledger, towers, notedWave);
                        }

                        trace.Sample(towers, note);
                        nextBuy -= Time.deltaTime;
                        if (nextBuy <= 0f)
                        {
                            nextBuy = BuyIntervalSeconds;
                            Act(context, spots, towers, owned, ledger, ref upgrades);
                        }
                    }

                    if (Time.timeScale > 0f)
                    {
                        Time.timeScale = Mathf.Clamp(MaxFrameSeconds / Mathf.Max(MinFrameSeconds, Time.unscaledDeltaTime), 1f, SimulationSpeed);
                    }

                    yield return null;
                }

                Time.timeScale = 1f;
                Assert.IsNotNull(context);
                trace.Detach();
                traces.Add($"{definition.DisplayName} on difficulty {difficulty}{(gunOnly ? " (gun only)" : string.Empty)}");
                traces.AddRange(trace.Lines);
                traces.Add(ProbeTrace.Layout(definition, towers));
                Assert.AreEqual(0, errors, "the level logged errors");
                results.Add(new ProbeResult
                {
                    Name = definition.DisplayName,
                    Outcome = context.Outcome,
                    Wave = context.WaveNumber,
                    FlyersSpawned = trace.FlyersSpawned,
                    FlyersKilled = trace.FlyersKilled,
                    FlyerBaseHits = trace.FlyerBaseHits,
                    BaseDamage = context.BaseHealth.Max - context.BaseHealth.Current,
                });
                report.Add($"[Probe] {definition.DisplayName} on difficulty {difficulty}: {context.Outcome}, wave {context.WaveNumber}/{context.WaveCount}, base {context.BaseHealth.Current:0}/{context.BaseHealth.Max:0}, towers {towers.Count}, upgrades {upgrades}, money {context.Wallet.Money}");
            }

            Application.logMessageReceived -= onLog;
            LogAssert.ignoreFailingMessages = false;
            System.IO.File.AppendAllLines(TracePath, traces);
            Debug.Log(string.Join(" | ", report));
        }

        // The ledger's view of the same level and difficulty: one line per wave to print beside what the bot really had,
        // and the wave loads the bot measures its own towers against.
        private static LedgerView LedgerFor(int node, int difficulty)
        {
            var view = new LedgerView { Rules = Game.Balance.BalanceRules.Default };
#if UNITY_EDITOR
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>(UpgradesPath);
            var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>(UnlocksPath);
            Game.Balance.CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, view.Rules);
            int index = 0;
            for (int i = 0; i < node; i++)
            {
                index += catalog.Nodes[i].Level != null ? 1 : 0;
            }

            view.Input = model.InputFor(index, difficulty);
            view.Lines.AddRange(BalanceLedger.Evaluate(model, index, catalog.Nodes[node].Level, catalog.Difficulties[difficulty], difficulty, view.Loads));
#endif
            return view;
        }

        // Node indices of the levels that exist; the smoke run keeps Shard Ridge and Crysium Vein.
        private static List<int> ExistingLevels(bool smoke)
        {
            var levels = new List<int>();
#if UNITY_EDITOR
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            for (int i = 0; catalog != null && i < catalog.Nodes.Count; i++)
            {
                if (catalog.Nodes[i].Level != null)
                {
                    levels.Add(i);
                }
            }
#endif
            if (smoke)
            {
                levels = levels.FindAll(i => System.Array.IndexOf(SmokeLevels, i) >= 0);
            }

            return levels;
        }

        // An optional filter file narrows a run, one "levels=0,1" or "difficulties=1,2" line each; no file means everything.
        private static bool Wanted(string key, int value)
        {
            if (!System.IO.File.Exists(FilterPath))
            {
                return true;
            }

            foreach (string line in System.IO.File.ReadAllLines(FilterPath))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    foreach (string item in parts[1].Split(','))
                    {
                        if (int.TryParse(item.Trim(), out int wanted) && wanted == value)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        // Cells beside the road, closest to the defence gate first (see DefenceGate): the walkers queue there, so a player's
        // towers go there. Every few towers one goes to the last gate before the base instead, a second line for the fast
        // ones that slip through (rollers, raging prowlers) and for what the late nests bring.
        private static List<Vector2Int> RankBuildSpots(LevelDefinition level)
        {
            int defence = DefenceGate(level);
            List<Vector2Int> front = RankAround(level, defence >= 0 ? level.Blockades[defence].Cell : (Vector2Int?)null);
            int last = level.Blockades.Length - 1;
            if (last <= defence)
            {
                return front;
            }

            List<Vector2Int> back = RankAround(level, level.Blockades[last].Cell);
            var result = new List<Vector2Int>();
            var taken = new HashSet<Vector2Int>();
            int f = 0;
            int b = 0;
            while (f < front.Count || b < back.Count)
            {
                bool fromBack = result.Count % SecondLineEvery == SecondLineEvery - 1 && b < back.Count;
                Vector2Int cell = fromBack || f >= front.Count ? back[b++] : front[f++];
                if (taken.Add(cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        private static List<Vector2Int> RankAround(LevelDefinition level, Vector2Int? gate)
        {
            var grid = level.Grid;
            var buildGrid = new BuildGrid(level);
            var road = new List<Vector2Int>(level.Road.Cells);
            var spots = new List<(Vector2Int Cell, float Score)>();

            for (int x = 0; x < grid.Size.x; x++)
            {
                for (int y = 0; y < grid.Size.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!buildGrid.CanBuild(cell))
                    {
                        continue;
                    }

                    float nearest = float.MaxValue;
                    foreach (Vector2Int roadCell in road)
                    {
                        nearest = Mathf.Min(nearest, Vector2Int.Distance(cell, roadCell));
                    }

                    // Prefer cells next to many road cells: they cover long stretches of the path.
                    int covered = 0;
                    foreach (Vector2Int roadCell in road)
                    {
                        if (Vector2Int.Distance(cell, roadCell) <= 2.5f)
                        {
                            covered++;
                        }
                    }

                    if (nearest > BesideRoadCells)
                    {
                        continue;
                    }

                    float fromGate = gate.HasValue ? Vector2Int.Distance(cell, gate.Value) : 0f;
                    spots.Add((cell, fromGate + nearest - covered * 0.2f));
                }
            }

            spots.Sort((a, b) => a.Score.CompareTo(b.Score));
            var result = new List<Vector2Int>();
            foreach (var spot in spots)
            {
                result.Add(spot.Cell);
            }

            return result;
        }

        // Buildable cells beside the road, most flight route in reach first: flyers ignore the road, so the air gun goes where
        // the routes run (they meet towards the base), not at the gate.
        private static List<Vector2Int> RankAirSpots(LevelDefinition level)
        {
            var route = new List<Vector2>();
            foreach (EnemyPath path in level.Paths)
            {
                for (int i = 0; path.Flying && i + 1 < path.Waypoints.Length; i++)
                {
                    Vector2 from = path.Waypoints[i];
                    Vector2 to = path.Waypoints[i + 1];
                    int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / RouteSampleCells);
                    for (int s = 0; s <= steps; s++)
                    {
                        route.Add(Vector2.Lerp(from, to, steps > 0 ? s / (float)steps : 0f));
                    }
                }
            }

            var spots = new List<(Vector2Int Cell, int Covered)>();
            foreach (Vector2Int cell in RankBuildSpots(level))
            {
                int covered = 0;
                foreach (Vector2 point in route)
                {
                    covered += Vector2.Distance(cell, point) <= AirReachCells ? 1 : 0;
                }

                spots.Add((cell, covered));
            }

            // A stable sort keeps the gate order among cells that cover as much route.
            var result = new List<Vector2Int>();
            foreach (var spot in System.Linq.Enumerable.OrderByDescending(spots, s => s.Covered))
            {
                result.Add(spot.Cell);
            }

            return result;
        }

        // The first gate every road passes (on the trunk the roads share), else the first gate: where a player who has
        // watched a wave or two holds the line with the towers and the hero. Same rule as the ledger's kill point
        // (KillPurse.DefenceRemaining). -1 when the level has no gate.
        private static int DefenceGate(LevelDefinition level)
        {
            Game.Balance.LevelShape shape = BalanceLedger.Shape(level);
            int best = -1;
            for (int i = 0; i < shape.Gates.Count && i < level.Blockades.Length; i++)
            {
                if (shape.Gates[i].OnTrunk && (best < 0 || shape.Gates[i].Progress < shape.Gates[best].Progress))
                {
                    best = i;
                }
            }

            return best >= 0 ? best : level.Blockades.Length > 0 ? 0 : -1;
        }

        // An average player's habit, judged with the ledger on the bot's own towers: first the ground holds the waves at
        // hand (the one being fought and the next), then the price of an air gun is put aside in time for the first flyer
        // wave the standing guns cannot meet, starting from the wave whose kill income still pays for it before the raid;
        // everything else goes to the affordable step (a new tower or an upgrade) that lowers the worse pressure of the
        // waves at hand most per coin: money in the bank kills nothing.
        private static void Act(LevelContext context, BuildSpots spots, List<Tower> towers, List<string> owned, LedgerView ledger, ref int upgrades)
        {
            if (ledger.Count == 0)
            {
                return;
            }

            int current = Mathf.Clamp(context.WaveNumber - 1, 0, ledger.Count - 1);
            List<(string, int)> standing = Standing(towers);
            bool groundHolds = Cover(ledger, standing, current, true) >= 1f / GroundCeiling;
            int reserve = groundHolds ? AirReserve(context, towers, owned, ledger, current) : 0;
            if (reserve > 0 && context.Wallet.CanAfford(reserve) && TryBuy(context, spots, towers, owned, AirOnly))
            {
                return;
            }

            int purse = context.Wallet.Money - reserve;
            // A step a wave's kills away is worth waiting for while the ground holds, if it is clearly better per coin
            // (an air gun before a raid enters the waves at hand, a big upgrade) than what the purse buys now.
            int reach = groundHolds ? purse + ledger.Lines[current].KillMoney : purse;
            float now = Cover(ledger, standing, current);
            float nowGround = Cover(ledger, standing, current, true);
            float nowAir = Cover(ledger, standing, current, false);
            float bestValue = 0f;
            float waitValue = 0f;
            Tower upgrade = null;
            TowerDefinition build = null;
            for (int i = 0; i < towers.Count; i++)
            {
                Tower tower = towers[i];
                if (!tower.CanUpgrade || tower.NextUpgradeCost > reach)
                {
                    continue;
                }

                var next = new List<(string, int)>(standing);
                next.Remove((tower.Definition.Id, tower.LevelIndex));
                next.Add((tower.Definition.Id, tower.LevelIndex + 1));
                float value = Gain(ledger, next, current, now, nowGround, nowAir) / Mathf.Max(1, tower.NextUpgradeCost);
                if (tower.NextUpgradeCost > purse)
                {
                    waitValue = Mathf.Max(waitValue, value);
                }
                else if (value > bestValue)
                {
                    bestValue = value;
                    upgrade = tower;
                    build = null;
                }
            }

            if (towers.Count < spots.Ground.Count)
            {
                foreach (TowerDefinition candidate in context.AllTowers)
                {
                    if (candidate.IsSupport || !owned.Contains(candidate.Id) || candidate.BuildCost > reach)
                    {
                        continue;
                    }

                    var next = new List<(string, int)>(standing) { (candidate.Id, 0) };
                    float value = Gain(ledger, next, current, now, nowGround, nowAir) / Mathf.Max(1, candidate.BuildCost);
                    if (candidate.BuildCost > purse)
                    {
                        waitValue = Mathf.Max(waitValue, value);
                    }
                    else if (value > bestValue)
                    {
                        bestValue = value;
                        build = candidate;
                        upgrade = null;
                    }
                }
            }

            if (waitValue > bestValue * WaitRatio)
            {
                return;
            }

            if (upgrade != null && context.Wallet.TrySpend(upgrade.NextUpgradeCost))
            {
                upgrade.Upgrade();
                upgrades++;
            }
            else if (build != null)
            {
                TryBuy(context, spots, towers, owned, t => t == build);
            }
        }

        // The price of the first air gun while it is still to be built and flyers are still to come, from the wave on whose income
        // (the ledger's money, kills paid as they really arrive) until the first flyer wave covers the price with a margin; 0 otherwise.
        private static int AirReserve(LevelContext context, List<Tower> towers, List<string> owned, LedgerView ledger, int current)
        {
            int price = CheapestBuild(context, owned, AirOnly);
            if (price == 0 || Has(towers, AirOnly))
            {
                return 0;
            }

            int raid = -1;
            for (int i = current; i < ledger.Count && raid < 0; i++)
            {
                if (ledger.Loads[i].AirHealth > 0f)
                {
                    raid = i;
                }
            }

            if (raid < 0)
            {
                return 0;
            }

            int from = 0;
            for (int i = raid; i >= 0; i--)
            {
                if (ledger.Lines[raid].Money - ledger.Lines[i].Money >= price * ReserveMargin)
                {
                    from = i;
                    break;
                }
            }

            return current >= from ? price : 0;
        }

        // What a step adds: the rise of the worse side's cover over the waves at hand; when the bottleneck does not move
        // (the other side is worse), a little for lifting a side.
        private static float Gain(LedgerView ledger, List<(string, int)> next, int current, float now, float nowGround, float nowAir)
        {
            float gain = Cover(ledger, next, current) - now;
            if (gain > 0f)
            {
                return gain;
            }

            float side = Mathf.Max(Lift(Cover(ledger, next, current, true), nowGround), Lift(Cover(ledger, next, current, false), nowAir));
            return side * OtherSideWeight;
        }

        private static float Lift(float after, float before) => after >= NoThreatCover || before >= NoThreatCover ? 0f : Mathf.Max(0f, after - before);

        // The worse of ground and air cover (one over the ledger pressure) over the wave being fought and the next.
        private static float Cover(LedgerView ledger, List<(string, int)> standing, int current) =>
            Mathf.Min(Cover(ledger, standing, current, true), Cover(ledger, standing, current, false));

        private static float Cover(LedgerView ledger, List<(string, int)> standing, int current, bool ground)
        {
            float cover = NoThreatCover;
            for (int i = current; i <= current + Horizon && i < ledger.Count; i++)
            {
                (float g, float a) = Pressure(ledger, standing, i);
                float pressure = ground ? g : a;
                if (pressure > 0f)
                {
                    cover = Mathf.Min(cover, float.IsInfinity(pressure) ? 0f : 1f / pressure);
                }
            }

            return cover;
        }

        // Ground from every standing tower; air only from the sky guns. The ledger lets every gun that aims up count for
        // the air, but the bot's guns stand around the gate while the flyers cross elsewhere (Shard Ridge on Medium: 18
        // guns, the ledger's air pressure below 0.5, 12 of 18 Skyrays through to the base), so a raid is the sky guns' job.
        private static (float Ground, float Air) Pressure(LedgerView ledger, List<(string, int)> standing, int wave)
        {
            Game.Balance.WaveLoad load = ledger.Loads[wave];
            float ground = Game.Balance.Ledger.PressureOf(ledger.Input, load, ledger.Rules, standing).Ground;
            if (load.AirHealth <= 0f)
            {
                return (ground, 0f);
            }

            List<(string Id, int Level)> sky = standing.FindAll(t => ledger.SkyGuns.Contains(t.Item1));
            return (ground, Game.Balance.Ledger.PressureOf(ledger.Input, load, ledger.Rules, sky).Air);
        }

        // The ledger's pressure of the wave against the bot's own towers (air from its sky guns only), beside the line.
        private static string BotNote(LedgerView ledger, List<Tower> towers, int wave)
        {
            if (wave < 1 || wave > ledger.Count)
            {
                return string.Empty;
            }

            (float ground, float air) = Pressure(ledger, Standing(towers), wave - 1);
            return $" | bot pressure {ground:0.00}/{air:0.00}";
        }

        // The bot's towers that still fire, as the ledger names them.
        private static List<(string, int)> Standing(List<Tower> towers)
        {
            var standing = new List<(string, int)>(towers.Count);
            foreach (Tower tower in towers)
            {
                if (tower.IsAlive)
                {
                    standing.Add((tower.Definition.Id, tower.LevelIndex));
                }
            }

            return standing;
        }

        // Build price of the cheapest owned tower of a kind, or 0 when the player owns none.
        private static int CheapestBuild(LevelContext context, List<string> owned, System.Predicate<TowerDefinition> kind)
        {
            int cheapest = 0;
            foreach (TowerDefinition tower in context.AllTowers)
            {
                if (owned.Contains(tower.Id) && kind(tower) && (cheapest == 0 || tower.BuildCost < cheapest))
                {
                    cheapest = tower.BuildCost;
                }
            }

            return cheapest;
        }

        // The plain gun of the naive check: it aims at ground and air alike.
        private static bool HitsBoth(LevelDefinition level, string towerId)
        {
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (tower.Id == towerId)
                {
                    return tower.Targets == (TargetLayers.Ground | TargetLayers.Air);
                }
            }

            return false;
        }

        private static bool AirOnly(TowerDefinition tower) => tower.Targets == TargetLayers.Air;

        private static bool Has(List<Tower> towers, System.Predicate<TowerDefinition> kind) => towers.Exists(t => kind(t.Definition));

        // The starting set plus every tower granted by the levels before this one.
        private static List<string> OwnedTowers(CampaignCatalog catalog, int level)
        {
            var owned = new List<string>();
#if UNITY_EDITOR
            var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>(UnlocksPath);
            owned.AddRange(unlocks.StartingTowerIds);
#endif
            for (int i = 0; i < level; i++)
            {
                if (catalog.Nodes[i].Level != null && catalog.Nodes[i].Level.Reward.NewTower != null)
                {
                    owned.Add(catalog.Nodes[i].Level.Reward.NewTower.Id);
                }
            }

            return owned;
        }

        // The first cell of a ranking that no tower of the bot stands on yet.
        private static bool FreeSpot(LevelContext context, List<Vector2Int> ranking, List<Tower> towers, out Vector2Int cell)
        {
            foreach (Vector2Int candidate in ranking)
            {
                if (!towers.Exists(t => context.Level.Grid.WorldToCell(t.Position) == candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private static bool TryBuy(LevelContext context, BuildSpots spots, List<Tower> towers, List<string> owned, System.Predicate<TowerDefinition> kind)
        {
            // The dearest affordable tower of the wanted kind.
            TowerDefinition best = null;
            for (int i = 0; i < context.AllTowers.Count; i++)
            {
                TowerDefinition candidate = context.AllTowers[i];
                if (candidate.IsSupport || !owned.Contains(candidate.Id) || (kind != null && !kind(candidate)) || !context.Wallet.CanAfford(candidate.BuildCost))
                {
                    continue;
                }

                if (best == null || candidate.BuildCost > best.BuildCost)
                {
                    best = candidate;
                }
            }

            if (best == null || !FreeSpot(context, AirOnly(best) ? spots.Air : spots.Ground, towers, out Vector2Int cell) || !context.Wallet.TrySpend(best.BuildCost))
            {
                return false;
            }

            var go = new GameObject($"BotTower_{best.DisplayName}");
            go.transform.position = context.Level.Grid.CellToWorld(cell);
            var tower = go.AddComponent<Tower>();
            go.AddComponent<TowerView>();
            tower.Initialize(best, context.Targets, context.Projectiles, context.ModifiersFor(best));
            tower.Register(context.Structures);
            towers.Add(tower);
            return true;
        }
    }
}

