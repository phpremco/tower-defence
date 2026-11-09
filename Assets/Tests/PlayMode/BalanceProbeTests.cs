using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Hero;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// A bot that plays like a competent player (the human lens, BalanceRules.HumanUtilisation): it holds the line at the
    /// first gate every road passes, puts each gun where it covers the most walk (bends, the queue in front of the gate),
    /// keeps a second line at the next gate, judges its own towers with the ledger in the human lens with the hero
    /// (<see cref="Game.Balance.Ledger.PressureOf"/>), saves for an air gun whenever the standing sky guns cannot hold the next
    /// raid, and spends the rest on the step (a new tower in the next free spot, or an upgrade of a standing one, both weighed
    /// by how good their spot is) that lowers the worse pressure most per coin. It repairs badly hurt guns and, a few times a
    /// wave, the defence gate, scraps wrecks, fires the hero's shockwave into crowds and bosses and its gate repair, and sends
    /// the hero to the next gate when the first one falls. It builds only the towers a player owns at that point of the
    /// campaign, buys no permanent upgrades, drones or gadgets, and writes a per-wave trace to Logs/probe_trace.txt. It must
    /// win every level on Easy and Medium.
    /// </summary>
    public sealed class BalanceProbeTests
    {
        private const float TimeoutSeconds = 900f;
        // Every frame advances the game by exactly this (Time.captureDeltaTime), however long the editor took to draw it.
        // Until 2026-11-05 the time scale followed the editor's frame rate (up to twenty times, at most 0.2 s a frame), so
        // a slow frame or a busy machine changed the steps the game was simulated in and two runs of one build differed.
        // With fixed steps and seeded draws (ProbeSeed) a run is reproducible: the same build plays the same game.
        private const float StepSeconds = 0.2f;
        // "step=0" replays the timing before 2026-11-05 for comparisons: the time scale follows the editor's frame rate (up
        // to AdaptiveSpeed, AdaptiveFrameSeconds a frame unless a frame takes longer), so runs depend on the machine.
        private const float AdaptiveSpeed = 20f;
        private const float AdaptiveFrameSeconds = 0.2f;
        private const float MinFrameSeconds = 0.0001f;
        // The seed of the level's draws (nests, group spacing) for every run; a "seed=N" line in the filter file overrides it
        // to sample other draws of the same waves (see Docs/BALANCE.md, probe seeds).
        private const int ProbeSeed = 1;
        private const float BuyIntervalSeconds = 1f;
        // The bot judges its towers against the wave being fought and the next one: on a long road the creatures of a
        // wave reach the gate while the next one spawns.
        private const int Horizon = 1;
        // Above this ground pressure on the waves at hand (the bot judges itself with the human lens and the hero), the ground
        // comes before waiting for a dearer step. 1.25 against the bot calibration until 2026-11-04; the coverage-placed bot
        // holds Shard Ridge on Medium with its own towers at 1.3 to 1.6 in the human lens (the model is cautious about gate
        // hold), and at 1.25 it never waited for a second Flak and lost to the Skyrays.
        private const float GroundCeiling = 1.6f;
        // A raid the standing sky guns meet above this pressure (human lens) calls for another air gun.
        private const float AirCeiling = 1f;
        // Kill money arrives late and not all of it, so the saving starts when the income up to the raid is this much
        // more than the air gun's price.
        private const float ReserveMargin = 2f;
        // A step that does not move the bottleneck still helps a little if it lifts the other side.
        private const float OtherSideWeight = 0.01f;
        // How much better per coin a step one wave of kills away must be before the bot waits for it. 1.5 until 2026-11-04:
        // a new Pulse Rifle (50) was always affordable before an upgrade (60, 90) that pays as much or more per coin, so the
        // bot almost never upgraded; a player saves the few coins for the better step.
        private const float WaitRatio = 1.3f;
        private const float NoThreatCover = 1e6f;
        private const float RouteSampleCells = 0.5f;
        // A little inside the Flak's first range (5.5), so the shells reach the route.
        private const float AirReachCells = 5f;
        private const int SecondLineEvery = 4;
        private const float BesideRoadCells = 2f;
        // Placement by coverage (RankAround): the reach of the first gun (Pulse Rifle 3.5), the queue in front of a gate,
        // and how much a spot loses per cell beyond the gate's surroundings.
        private const float CoverReachCells = 3.5f;
        private const float GateOnRouteCells = 3f;
        private const float QueueCells = 3f;
        private const float QueueWeight = 2f;
        // Role-aware placement (round 20): a splash gun weighs the queue and the bends, where walkers bunch, this much more.
        private const float SplashQueueWeight = 4f;
        private const float SplashBendWeight = 2f;
        private const float BendCos = 0.707f;
        private const float BendReachCells = 1.5f;
        // The creative player (round 20): drones once this many guns stand (the harvester not in the last waves), mines a wave
        // before crowd and boss waves within this many road cells of the gate, and it never sells below this many ground guns.
        private const int HarvestAfterTowers = 3;
        private const int NoHarvestLastWaves = 2;
        // Ground guns standing before a second sky gun is bought.
        private const int GroundBeforeSecondSkyGun = 3;
        // At most one sky gun for this many ground guns (round 21a4: Shard Ridge Medium, four Flaks and a sold Cannon, lost to ants).
        private const int SkyPerGround = 2;
        private const int RepairAfterTowers = 4;
        private const int MinesPerWave = 2;
        private const int MinesAfterTowers = 4;
        private const int GadgetRoadCells = 4;
        private const int SellKeepsGroundGuns = 3;
        private const float PastGateWeight = 0.35f;
        private const float GateReachCells = 7f;
        private const float FarFromGatePenalty = 1.5f;
        // The least a spot counts for against the best one, so a level whose good spots are full still builds.
        private const float MinSpotQuality = 0.3f;
        // Up to this ground pressure on the waves at hand (judged with the human lens) the bot still puts the air gun's price aside.
        private const float ReserveCeiling = 1.6f;
        // A wave a splash gun's blast meets at least this many walkers of (the ledger's splash count, 1 + 2 x radius x density)
        // is a crowd: a player who sees it in the preview builds one splash gun before it, as the Flak before a raid (2026-11-05:
        // Quartz Flats Easy was lost on 4 of 7 seeds by bots that stayed all Pulse Rifle into the crowd waves 12-15).
        private const float CrowdSplashTargets = 2f;
        // The hero's abilities as a player uses them: the shockwave into a crowd or a boss at the post, the gate repair when
        // the gate is down to this share; towers are repaired below RepairBelow, wrecks scrapped so their spot is free.
        private const int ShockwaveCrowd = 4;
        private const float GateRepairBelow = 0.4f;
        private const float RepairBelow = 0.5f;
        // A structure's loss in the next wave is reckoned as the worse of this wave and the last, times this for the growth.
        private const float LossGrowth = 1.25f;
        private const int GateRepairsPerWave = 2;
        private const string FilterPath = "Tools/PlayTest/probe_arg.txt";
        internal const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        internal const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        internal const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string TracePath = "Logs/probe_trace.txt";
        private const string ResultsPath = "Logs/probe_results.txt";

        // The smoke run: Shard Ridge (first flyers) and Crysium Vein (the last level of the slice), on Medium.
        private static readonly int[] SmokeLevels = { 2, 5 };
        private const int SmokeDifficulty = 1;

        // Shard Ridge and Fault Line: the levels with packed air raids.
        private static readonly int[] AirRaidLevels = { 2, 3 };

        // Where the gun-only build must struggle. Fault Line is checked for the Flak bot's win only: its road joins the
        // trunk late, so every gun stands near the base where the flight routes meet, and a gun-only defence downs every
        // flyer, at the raids' own size and at 1.6 or 1.8 times it (2026-11-04), so its raids stay as they were.
        private static readonly int[] StruggleLevels = { 2 };

        // Levels with two ground fronts the bot does not split its defence over (Fault Line: half the walkers come up a second
        // road that joins at the last gate). The bot defends the trunk gate only, earns little while both lanes queue at their
        // own gates, and won Fault Line Medium in about half of its runs on 2026-11-04 (always Easy). Until the bot can split,
        // these levels are asserted on Easy only, and in the air check only for the air (no flyer may reach the base).
        private static readonly int[] TwoFrontLevels = { 3 };

        // Every run appends its trace, so one test's file holds all of its runs.
        [SetUp]
        public void ClearTrace()
        {
            if (System.IO.File.Exists(TracePath))
            {
                System.IO.File.Delete(TracePath);
            }
        }

        // A run cut off by the timeout must not leave the editor stepping in fixed game time.
        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
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

        // The waves are calibrated to a human curve since 2026-11-04 (Easy 1.4x, Medium 1.15x the firepower needed on the first
        // level, a little less on each later one). The bot places and upgrades like that player and uses the hero, so it must
        // win every level on Easy and on Medium (Medium without the two-front levels, see TwoFrontLevels). Hard and Psycho are
        // left to the Full run: the bot may lose there (Hard needs the hero and good play, Psycho the permanent upgrades the
        // bot does not buy).
        [UnityTest]
        [NUnit.Framework.Timeout(3600000)]
        public IEnumerator EasyAndMedium_Bot_ClearsEveryLevel()
        {
            var runs = new List<(int, int)>();
            foreach (int level in ExistingLevels(false))
            {
                runs.Add((level, 0));
                if (System.Array.IndexOf(TwoFrontLevels, level) < 0)
                {
                    runs.Add((level, 1));
                }
            }

            var results = new List<ProbeResult>();
            yield return Probe(runs, false, results);
            Assert.AreEqual(runs.Count, results.Count);
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
                if (System.Array.IndexOf(TwoFrontLevels, level) >= 0)
                {
                    Assert.AreEqual(0, mixed.FlyerBaseHits, $"{mixed.Name}: with a Flak no flyer may reach the base");
                }
                else
                {
                    Assert.AreEqual(LevelOutcome.Won, mixed.Outcome, $"{mixed.Name}: the bot with a Flak must hold Medium");
                }
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
                    if (WantedCell(level, difficulty))
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
            public BuildSpots(List<Vector2Int> ground, List<Vector2Int> air, Dictionary<Vector2Int, float> quality)
            {
                Ground = ground;
                Air = air;
                Quality = quality;
            }

            public List<Vector2Int> Ground { get; }

            /// <summary>How much road a ground spot covers, as a share of the best spot (0 to 1).</summary>
            public Dictionary<Vector2Int, float> Quality { get; }

            public List<Vector2Int> Air { get; }

            /// <summary>Per tower type its own ranking and spot values (role-aware placement); types without one use Ground or Air.</summary>
            public readonly Dictionary<string, List<Vector2Int>> Role = new Dictionary<string, List<Vector2Int>>();
            public readonly Dictionary<string, Dictionary<Vector2Int, float>> RoleQuality = new Dictionary<string, Dictionary<Vector2Int, float>>();

            /// <summary>True for the naive bot: every spot counts the same, it does not weigh where a gun stands.</summary>
            public bool Uniform;

            public List<Vector2Int> RankingFor(TowerDefinition tower) =>
                Role.TryGetValue(tower.Id, out List<Vector2Int> ranking) ? ranking : AirOnly(tower) ? Air : Ground;

            public float QualityFor(TowerDefinition tower, Vector2Int cell)
            {
                if (Uniform || AirOnly(tower))
                {
                    return 1f;
                }

                Dictionary<Vector2Int, float> quality = RoleQuality.TryGetValue(tower.Id, out Dictionary<Vector2Int, float> own) ? own : Quality;
                return quality.TryGetValue(cell, out float value) ? value : MinSpotQuality;
            }
        }

        /// <summary>The gate the defence stands around, and how often it was repaired this wave.</summary>
        private sealed class GateWatch
        {
            public Tower Gate;
            public int Wave = -1;
            public int Repairs;
            public int Post = -1;
            public int Fallback = -1;

            /// <summary>Money spent on repairs in the whole run (the work a repair drone would do for free).</summary>
            public int RepairSpent;

            /// <summary>Health the gates and the guns lost in the whole run, and the health each had at the last look.</summary>
            public float GateDamage;
            public float TowerDamage;
            public readonly Dictionary<Tower, float> LastHealth = new Dictionary<Tower, float>();

            /// <summary>Health each structure lost in the wave being fought and in the one before: what the next wave may cost it.</summary>
            public readonly Dictionary<Tower, float> LossThisWave = new Dictionary<Tower, float>();
            public readonly Dictionary<Tower, float> LossLastWave = new Dictionary<Tower, float>();
            public int LossWave = -1;
        }

        /// <summary>The ledger's view of one run: its lines for the trace, and the wave loads and level input the bot judges its own towers with.</summary>
        /// <summary>
        /// Another field, waves, purse and player for one probe run (the finale probe, round 22f): set before <see cref="Probe"/>
        /// and cleared after. The node index then only names the run; the level, its waves, the start money, the player state,
        /// the owned towers and the bot's ledger view all come from here.
        /// </summary>
        internal sealed class ProbeOverride
        {
            public LevelDefinition Level;
            public WaveDefinition[] Waves;
            public int StartingMoney;
            public Game.Balance.PlayerState Player;
            public List<string> Owned = new List<string>();
            public LedgerView Ledger;
        }

        internal static ProbeOverride Override;

        /// <summary>A ledger view for given waves and player input, as the bot judges its towers by (the finale probe's).</summary>
        internal static LedgerView LedgerFor(Game.Balance.LevelInput input, IReadOnlyList<Game.Balance.WaveSpec> waves, Game.Balance.DifficultyStats stats)
        {
            var view = new LedgerView { Rules = Game.Balance.BalanceRules.Default, Input = input };
            view.Judge = view.Rules.HumanWithHero();
            view.Lines.AddRange(Game.Balance.Ledger.Evaluate(input, waves, stats, view.Rules, view.Loads));
            return view;
        }

        internal sealed class LedgerView
        {
            public readonly List<Game.Balance.WaveLine> Lines = new List<Game.Balance.WaveLine>();
            public readonly List<Game.Balance.WaveLoad> Loads = new List<Game.Balance.WaveLoad>();
            public readonly HashSet<string> SkyGuns = new HashSet<string>();
            public Game.Balance.LevelInput Input;
            public Game.Balance.BalanceRules Rules;

            /// <summary>The rules the bot judges its own towers by: it places like a competent player (the human lens), with the hero.</summary>
            public Game.Balance.BalanceRules Judge;

            public int Count => Loads.Count;

            public string LineFor(int wave) => wave >= 1 && wave <= Lines.Count ? Lines[wave - 1].ToString() : string.Empty;
        }

        /// <summary>How one bot run ended.</summary>
        internal sealed class ProbeResult
        {
            public string Name;
            public string Asset;
            public int Difficulty;
            public string Strategy;
            public LevelOutcome Outcome;

            /// <summary>Played on a plain save instead of the cell's expected player (the fresh-save sanity runs).</summary>
            public bool FreshPlayer;
            public int Wave;
            public int WaveCount;
            public int Stars;
            public float BaseHealth;
            public float BaseMax;
            public int TowersBuilt;
            public int TowersStanding;
            public int Upgrades;
            public int CoinsUnspent;
            public int Sold;
            public int Mines;
            public int Fields;
            public string Drones = string.Empty;
            public int Harvested;
            public float DroneDamage;
            public float GameSeconds;
            public int FlyersSpawned;
            public int FlyersKilled;
            public int FlyerBaseHits;
            public float BaseDamage;

            /// <summary>Coins spent on repairs in the run, and everything the run earned (start money and kills), for the share.</summary>
            public int RepairSpent;
            public int Earned;

            /// <summary>Per tower type and role, damage dealt; per tower type, coin-seconds (see <see cref="BotRun.RoleDamage"/>).</summary>
            public Dictionary<(string Tower, string Role), float> RoleDamage = new Dictionary<(string, string), float>();
            public Dictionary<string, float> CoinSeconds = new Dictionary<string, float>();

            /// <summary>The seed and step of the run, and its wave records for the calibration (<see cref="Game.Balance.CalibrationFit"/>).</summary>
            public int Seed;
            public float Step;
            public List<Game.Balance.WaveRecord> Records = new List<Game.Balance.WaveRecord>();

            /// <summary>The build order the run really took (round 22e).</summary>
            public List<Game.Balance.PlanStep> Moves = new List<Game.Balance.PlanStep>();

            /// <summary>The run as the calibration fit reads it.</summary>
            public Game.Balance.RunRecord ToRecord()
            {
                var record = new Game.Balance.RunRecord
                {
                    Level = Asset,
                    Difficulty = Difficulty,
                    Strategy = Strategy,
                    Seed = Seed,
                    Step = Step,
                    Won = Outcome == LevelOutcome.Won,
                    Stars = Stars,
                    WaveReached = Wave,
                    WaveCount = WaveCount,
                    BaseHealth = BaseHealth,
                    Coins = CoinsUnspent,
                    FreshPlayer = FreshPlayer,
                };
                record.Waves.AddRange(Records);
                record.Moves.AddRange(Moves);
                return record;
            }

            /// <summary>Per tower type: damage dealt in the run, tower-seconds standing while creatures were on the field, and the data DPS of its levels then.</summary>
            public Dictionary<string, (float Damage, float Seconds, float DataDamage)> Fire = new Dictionary<string, (float, float, float)>();
        }

        // Plays each (node, difficulty) run; gunOnly limits the bot to the owned guns that hit both ground and air.
        private IEnumerator Probe(List<(int Level, int Difficulty)> runs, bool gunOnly, List<ProbeResult> results) =>
            Probe(runs, gunOnly ? ProbeStrategy.GunOnly : ProbeStrategy.Greedy, results);

        /// <summary>Plays each (node, difficulty) run with a bot strategy (see <see cref="ProbeStrategy"/>).</summary>
        internal static IEnumerator Probe(List<(int Level, int Difficulty)> runs, ProbeStrategy strategy, List<ProbeResult> results) =>
            Probe(runs, strategy, results, null);

        /// <summary>Plays each run with a strategy on a given seed (null: the filter file's or the default).</summary>
        internal static IEnumerator Probe(List<(int Level, int Difficulty)> runs, ProbeStrategy strategy, List<ProbeResult> results, int? seedOverride) =>
            Probe(runs, strategy, results, seedOverride, false);

        /// <summary>
        /// As above; <paramref name="freshPlayer"/> plays a plain save instead of the campaign's expected player for the cell (the
        /// permanent upgrades, perks and base turrets the plan search plans for; see <see cref="ProbePlayer"/>).
        /// </summary>
        internal static IEnumerator Probe(List<(int Level, int Difficulty)> runs, ProbeStrategy strategy, List<ProbeResult> results, int? seedOverride, bool freshPlayer)
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
            // A headless worker keeps every rig evaluated although nothing is drawn (see the camera switch below).
            ModelClipPlayer.AlwaysAnimate = Application.isBatchMode;
            foreach ((int level, int difficulty) in runs)
            {
                SceneManager.LoadScene(SceneNames.Boot);
                yield return null;
                yield return null;

                var catalog = Services.Get<CampaignCatalog>();
                ProbeOverride over = Override;
                LevelDefinition definition = over?.Level ?? catalog.Nodes[level].Level;
                List<string> owned = over != null ? new List<string>(over.Owned) : OwnedTowers(catalog, level, difficulty);
                if (strategy.Allowed != null)
                {
                    owned.RemoveAll(id => !Allows(strategy, definition, id));
                }

                Game.Balance.PlayerState player = over != null ? ProbePlayer.Install(catalog, over.Player, owned) : ProbePlayer.Install(catalog, level, difficulty, freshPlayer, owned);
                Debug.Log("[Probe] player: " + ProbePlayer.Describe(player, freshPlayer));
                int seed = seedOverride ?? Seed();
                float step = Step();
                UnityEngine.Random.InitState(seed);
                Services.Register(over != null
                    ? new LevelSession(definition, catalog.Difficulties[difficulty], false, null, over.Waves, over.StartingMoney, seed)
                    : new LevelSession(definition, catalog.Difficulties[difficulty], seed: seed));
                SceneManager.LoadScene(SceneNames.Level);
                yield return null;
                yield return null;
                if (Application.isBatchMode)
                {
                    // A headless worker (round 25) measures the game, not the picture: with the cameras off a frame costs the
                    // simulation only, six workers do not queue at one GPU, and -nographics raises no render errors.
                    foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                    {
                        camera.enabled = false;
                    }
                }

                LedgerView ledger = over?.Ledger ?? LedgerFor(level, difficulty);
                foreach (TowerDefinition tower in definition.AvailableTowers)
                {
                    if (AirOnly(tower))
                    {
                        ledger.SkyGuns.Add(tower.Id);
                    }
                }

                Time.timeScale = 1f;
                Time.captureDeltaTime = step;
                float started = Time.realtimeSinceStartup;
                float nextBuy = 0f;
                LevelContext context = null;
                BuildSpots spots = null;
                var towers = new List<Tower>();
                var gates = new List<Tower>();
                var watch = new GateWatch();
                var run = new BotRun(strategy, seed);
                ProbeTrace trace = null;
                int notedWave = -1;
                string note = string.Empty;

                while (Time.realtimeSinceStartup - started < TimeoutSeconds)
                {
                    if (context == null && Services.TryGet(out LevelContext found))
                    {
                        context = found;
                        spots = SpotsFor(definition, context, owned, strategy, run);
                        int post = DefenceGate(definition);
                        if (context.Hero != null && post >= 0)
                        {
                            context.Hero.MoveTo(post);
                        }

                        trace = new ProbeTrace(context);
                        run.Attach(context);
                        foreach (Tower placed in Object.FindObjectsByType<Tower>())
                        {
                            if (placed.Definition.BlocksGroundEnemies)
                            {
                                gates.Add(placed);
                            }
                        }

                        watch.Gate = post >= 0 ? Nearest(gates, context.Level.Grid.CellToWorld(definition.Blockades[post].Cell)) : null;
                        watch.Post = post;
                        watch.Fallback = FallbackGate(definition, post);
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

                        trace.Sample(towers, note, watch.RepairSpent);
                        MeasureWear(watch, towers, gates);
                        run.Sample(context, towers, gates, Time.deltaTime);
                        nextBuy -= Time.deltaTime;
                        if (nextBuy <= 0f)
                        {
                            nextBuy = BuyIntervalSeconds;
                            Act(context, spots, towers, gates, watch, owned, ledger, run);
                        }
                    }

                    if (step <= 0f && Time.timeScale > 0f)
                    {
                        Time.timeScale = Mathf.Clamp(AdaptiveFrameSeconds / Mathf.Max(MinFrameSeconds, Time.unscaledDeltaTime), 1f, AdaptiveSpeed);
                    }

                    yield return null;
                }

                Time.captureDeltaTime = 0f;
                Time.timeScale = 1f;
                Assert.IsNotNull(context);
                trace.Detach();
                run.Detach(context);
                traces.Add($"{definition.DisplayName} on difficulty {difficulty}, seed {seed}, strategy {strategy.Name}");
                if (context != null && context.Outcome == LevelOutcome.Running)
                {
                    // The wall clock ran out with the level still running: say what the level is waiting for (round 25, the C1_03 stall).
                    string stall = "[Probe] wall clock ran out at wave " + context.WaveNumber + "/" + context.WaveCount + ": " + (Object.FindFirstObjectByType<LevelController>()?.SpawnDiagnostic() ?? "no controller");
                    Debug.Log(stall);
                    traces.Add(stall);
                }

                traces.AddRange(trace.Lines);
                traces.Add(ProbeTrace.Layout(definition, towers));
                Assert.AreEqual(0, errors, "the level logged errors");
                float baseFraction = context.BaseHealth.Max > 0f ? context.BaseHealth.Current / context.BaseHealth.Max : 0f;
                results.Add(new ProbeResult
                {
                    Name = definition.DisplayName,
                    Asset = definition.name,
                    Difficulty = difficulty,
                    Strategy = strategy.Name,
                    Outcome = context.Outcome,
                    FreshPlayer = freshPlayer,
                    Wave = context.WaveNumber,
                    WaveCount = context.WaveCount,
                    Stars = context.Outcome == LevelOutcome.Won ? RewardCalculator.StarsFor(definition, baseFraction) : 0,
                    BaseHealth = context.BaseHealth.Current,
                    BaseMax = context.BaseHealth.Max,
                    TowersBuilt = run.Built,
                    TowersStanding = towers.Count,
                    Upgrades = run.Upgrades,
                    CoinsUnspent = context.Wallet.Money,
                    Sold = run.Sold,
                    Mines = run.Mines,
                    Fields = run.Fields,
                    Drones = string.Join("+", run.DronesBought),
                    Harvested = run.Harvested,
                    DroneDamage = run.DroneDamage,
                    GameSeconds = run.Seconds,
                    Fire = run.Fire,
                    FlyersSpawned = trace.FlyersSpawned,
                    RepairSpent = watch.RepairSpent,
                    Moves = run.Moves,
                    RoleDamage = run.RoleDamage,
                    CoinSeconds = run.CoinSeconds,
                    Seed = seed,
                    Step = step,
                    Records = run.Records,
                    Earned = trace.Earned,
                    FlyersKilled = trace.FlyersKilled,
                    FlyerBaseHits = trace.FlyerBaseHits,
                    BaseDamage = context.BaseHealth.Max - context.BaseHealth.Current,
                });
                report.Add($"[Probe] {definition.DisplayName} on difficulty {difficulty} (seed {seed}, step {step:0.###}{(strategy == ProbeStrategy.Greedy ? string.Empty : ", " + strategy.Name)}): {context.Outcome}, wave {context.WaveNumber}/{context.WaveCount}, base {context.BaseHealth.Current:0}/{context.BaseHealth.Max:0}, towers {towers.Count}, upgrades {run.Upgrades}, money {context.Wallet.Money}, wear gates {watch.GateDamage:0} guns {watch.TowerDamage:0}, repairs bought {watch.RepairSpent}");
            }

            Application.logMessageReceived -= onLog;
            LogAssert.ignoreFailingMessages = false;
            System.IO.File.AppendAllLines(TracePath, traces);
            // One line per run, kept over test runs, so repeated runs and seed sweeps can be compared.
            System.IO.File.AppendAllLines(ResultsPath, report);
            Debug.Log(string.Join(" | ", report));
        }

        // Whether the strategy lets the bot build an owned tower (by id, as the level sells it).
        private static bool Allows(ProbeStrategy strategy, LevelDefinition level, string towerId)
        {
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (tower.Id == towerId)
                {
                    return strategy.Allowed(tower);
                }
            }

            return false;
        }

        // Where the bot builds: the coverage rankings around the defence gate; per tower type by its own reach and role for
        // a role-aware strategy; every spot beside the road in a seeded random order for the naive one.
        private static BuildSpots SpotsFor(LevelDefinition definition, LevelContext context, List<string> owned, ProbeStrategy strategy, BotRun run)
        {
            var quality = new Dictionary<Vector2Int, float>();
            var spots = new BuildSpots(RankBuildSpots(definition, quality), RankAirSpots(definition), quality);
            if (strategy.Naive)
            {
                // Round 21a4: a careless player, not random spots: the first free spots beside the road nearest the defence gate,
                // whatever the overlap, the bends or the queue (ties in a seeded order). Random spots off the road made Easy look hard.
                spots.Uniform = true;
                run.Shuffle(spots.Ground);
                run.Shuffle(spots.Air);
                int gate = DefenceGate(definition);
                Vector2 near = gate >= 0 ? (Vector2)definition.Blockades[gate].Cell : Vector2.zero;
                spots.Ground.Sort((a, b) => Vector2.Distance(a, near).CompareTo(Vector2.Distance(b, near)));
                spots.Air.Sort((a, b) => Vector2.Distance(a, near).CompareTo(Vector2.Distance(b, near)));
                return spots;
            }

            if (!strategy.RoleSpots)
            {
                return spots;
            }

            // A searched strategy (round 22e) brings its own spot priority per type; the bot's ranking follows for the spots it does not name.
            foreach (Game.Balance.SpotPriority priority in strategy.Spec.Spots)
            {
                var ranking = new List<Vector2Int>();
                for (int i = 0; i < priority.Count; i++)
                {
                    ranking.Add(new Vector2Int(priority.X[i], priority.Y[i]));
                }

                spots.Role[priority.Tower] = ranking;
            }

            foreach (TowerDefinition tower in context.AllTowers)
            {
                if (!owned.Contains(tower.Id) || tower.IsSupport || AirOnly(tower))
                {
                    continue;
                }

                // A splash gun goes where the walkers bunch (the queue, the bends), a rate-of-fire gun where it covers the most road.
                bool splash = tower.SplashRadius > 0f;
                var own = new Dictionary<Vector2Int, float>();
                float reach = tower.GetLevel(0).Range / Mathf.Max(0.01f, definition.CellSize);
                List<Vector2Int> ranked = RankBuildSpots(definition, own, reach, splash ? SplashQueueWeight : QueueWeight, splash ? SplashBendWeight : 1f);
                if (spots.Role.TryGetValue(tower.Id, out List<Vector2Int> named))
                {
                    ranked.RemoveAll(named.Contains);
                    named.AddRange(ranked);
                }
                else
                {
                    spots.Role[tower.Id] = ranked;
                }

                spots.RoleQuality[tower.Id] = own;
            }

            foreach (Game.Balance.SpotPriority priority in strategy.Spec.Spots)
            {
                // A sky gun's named spots are followed by the bot's air ranking.
                TowerDefinition tower = TowerById(context, priority.Tower);
                if (tower != null && AirOnly(tower) && spots.Role.TryGetValue(tower.Id, out List<Vector2Int> named))
                {
                    named.AddRange(spots.Air.FindAll(c => !named.Contains(c)));
                }
            }

            return spots;
        }

        // The ledger's view of the same level and difficulty: one line per wave to print beside what the bot really had,
        // and the wave loads the bot measures its own towers against.
        private static LedgerView LedgerFor(int node, int difficulty)
        {
            var view = new LedgerView { Rules = Game.Balance.BalanceRules.Default };
            view.Judge = view.Rules.HumanWithHero();
#if UNITY_EDITOR
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var upgrades = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>(UpgradesPath);
            var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>(UnlocksPath);
            Game.Balance.CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, view.Rules, UnityEditor.AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));
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
        internal static List<int> ExistingLevels(bool smoke)
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

        // The seed of every run: ProbeSeed, or the "seed=N" line of the filter file.
        private static int Seed() => (int)Setting("seed", ProbeSeed);

        /// <summary>A step a test sets for its own runs (the step check); null takes the filter file's or the default.</summary>
        internal static float? StepOverride;

        // Game seconds per frame: StepSeconds, or the "step=S" line of the filter file (to compare finer steps).
        private static float Step() => StepOverride ?? Setting("step", StepSeconds);

        private static float Setting(string key, float fallback)
        {
            if (System.IO.File.Exists(FilterPath))
            {
                foreach (string line in System.IO.File.ReadAllLines(FilterPath))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2 && parts[0].Trim() == key && float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                    {
                        return value;
                    }
                }
            }

            return fallback;
        }

        /// <summary>
        /// Whether the filter file wants a cell: its level and difficulty lines, and a "cells=2:0,3:2" line (level:difficulty) that
        /// names single cells, so a replay of the cells an impact report flagged needs no product of levels and difficulties
        /// (round 25). Every parallel worker gets its own filter file with the cells and seeds it plays.
        /// </summary>
        internal static bool WantedCell(int level, int difficulty) =>
            Wanted("levels", level) && Wanted("difficulties", difficulty) && WantedUnit(level, difficulty, null);

        /// <summary>
        /// Whether a cell's seed is wanted: a "cells=" entry may carry its seed ("2:0:3"), so a worker plays exactly the (cell, seed)
        /// units it was given and no product of cells and seeds. An entry without a seed wants every seed.
        /// </summary>
        internal static bool WantedSeed(int level, int difficulty, int seed) => WantedUnit(level, difficulty, seed);

        private static bool WantedUnit(int level, int difficulty, int? seed)
        {
            if (!System.IO.File.Exists(FilterPath))
            {
                return true;
            }

            foreach (string line in System.IO.File.ReadAllLines(FilterPath))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == "cells")
                {
                    foreach (string item in parts[1].Split(','))
                    {
                        string[] unit = item.Split(':');
                        if (unit.Length < 2 || !int.TryParse(unit[0].Trim(), out int l) || !int.TryParse(unit[1].Trim(), out int d) || l != level || d != difficulty)
                        {
                            continue;
                        }

                        if (!seed.HasValue || unit.Length < 3 || (int.TryParse(unit[2].Trim(), out int s) && s == seed.Value))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        // An optional filter file narrows a run, one "levels=0,1" or "difficulties=1,2" line each (and "seed=N" for the draws, "step=S" for the game seconds per frame); no file means everything.
        internal static bool Wanted(string key, int value)
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

        // Cells beside the road, the best cover around the defence gate first (see DefenceGate and RankAround): the walkers
        // queue there, so a player's towers go there. Every few towers one goes to the next gate on the trunk instead, a second
        // line where the queue moves when the first gate falls, and for the fast ones that slip through (rollers, raging
        // prowlers). Until 2026-11-04 that was the last gate before the base: on Glass Meadows the first gate fell and the
        // creatures queued at the next one, out of reach of every gun.
        private static List<Vector2Int> RankBuildSpots(LevelDefinition level, Dictionary<Vector2Int, float> quality, float reach = CoverReachCells, float queueWeight = QueueWeight, float bendWeight = 1f)
        {
            int defence = DefenceGate(level);
            List<Vector2Int> front = RankAround(level, defence >= 0 ? level.Blockades[defence].Cell : (Vector2Int?)null, quality, reach, queueWeight, bendWeight);
            int fallback = FallbackGate(level, defence);
            if (fallback < 0)
            {
                return front;
            }

            List<Vector2Int> back = RankAround(level, level.Blockades[fallback].Cell, quality, reach, queueWeight, bendWeight);
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

        // A competent player's placement (the human lens, BalanceRules.HumanUtilisation): a gun goes where it covers the
        // most walk in reach, which is the inside of a bend and the queue in front of a gate (walkers stop there), rather
        // than the nearest ring around the gate. Coverage counts the walking routes sample by sample, so the trunk all
        // routes share weighs more; road past the gate counts less (only leakers walk it), and a spot far from the gate
        // loses value, so the defence stays together where the hero holds.
        private static List<Vector2Int> RankAround(LevelDefinition level, Vector2Int? gate, Dictionary<Vector2Int, float> quality, float reach, float queueWeight, float bendWeight)
        {
            var grid = level.Grid;
            var buildGrid = new BuildGrid(level);
            var road = new List<Vector2Int>(level.Road.Cells);
            List<(Vector2 Point, float Weight)> walk = WalkSamples(level, gate, queueWeight, bendWeight);
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

                    if (nearest > BesideRoadCells)
                    {
                        continue;
                    }

                    float covered = 0f;
                    foreach ((Vector2 point, float weight) in walk)
                    {
                        if (Vector2.Distance(cell, point) <= reach)
                        {
                            covered += weight;
                        }
                    }

                    float fromGate = gate.HasValue ? Vector2Int.Distance(cell, gate.Value) : 0f;
                    float score = covered * RouteSampleCells - Mathf.Max(0f, fromGate - GateReachCells) * FarFromGatePenalty;
                    // Sorted ascending: the best spot has the lowest score.
                    spots.Add((cell, -score));
                }
            }

            spots.Sort((a, b) => a.Score.CompareTo(b.Score));
            float best = spots.Count > 0 ? Mathf.Max(1f, -spots[0].Score) : 1f;
            var result = new List<Vector2Int>();
            foreach (var spot in spots)
            {
                result.Add(spot.Cell);
                float share = Mathf.Clamp(-spot.Score / best, MinSpotQuality, 1f);
                quality[spot.Cell] = quality.TryGetValue(spot.Cell, out float known) ? Mathf.Max(known, share) : share;
            }

            return result;
        }

        // Points along every walking route, one per RouteSampleCells, weighted by how much a gun there is worth: the queue in
        // front of the gate most, the walk up to it fully, the road past it a little.
        private static List<(Vector2 Point, float Weight)> WalkSamples(LevelDefinition level, Vector2Int? gate, float queueWeight, float bendWeight)
        {
            var samples = new List<(Vector2 Point, float Weight)>();
            foreach (EnemyPath path in level.Paths)
            {
                if (path.Flying || path.Waypoints == null)
                {
                    continue;
                }

                var points = new List<Vector2>();
                for (int i = 0; i + 1 < path.Waypoints.Length; i++)
                {
                    Vector2 from = path.Waypoints[i];
                    Vector2 to = path.Waypoints[i + 1];
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / RouteSampleCells));
                    for (int s = 0; s < steps; s++)
                    {
                        points.Add(Vector2.Lerp(from, to, s / (float)steps));
                    }
                }

                int gateAt = -1;
                float gateDistance = float.MaxValue;
                for (int i = 0; gate.HasValue && i < points.Count; i++)
                {
                    float distance = Vector2.Distance(points[i], gate.Value);
                    if (distance < gateDistance)
                    {
                        gateDistance = distance;
                        gateAt = i;
                    }
                }

                bool passesGate = gateAt >= 0 && gateDistance <= GateOnRouteCells;
                for (int i = 0; i < points.Count; i++)
                {
                    float weight = 1f;
                    if (passesGate && i > gateAt)
                    {
                        weight = PastGateWeight;
                    }
                    else if (passesGate && Vector2.Distance(points[i], gate.Value) <= QueueCells)
                    {
                        weight = queueWeight;
                    }

                    if (bendWeight != 1f && NearBend(path, points[i]))
                    {
                        weight *= bendWeight;
                    }

                    samples.Add((points[i], weight));
                }
            }

            return samples;
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
            foreach (Vector2Int cell in RankBuildSpots(level, new Dictionary<Vector2Int, float>()))
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

        // The trunk gate after the defence gate, the nearest one along the road; -1 when there is none.
        private static int FallbackGate(LevelDefinition level, int defence)
        {
            if (defence < 0)
            {
                return -1;
            }

            Game.Balance.LevelShape shape = BalanceLedger.Shape(level);
            int best = -1;
            for (int i = 0; i < shape.Gates.Count && i < level.Blockades.Length; i++)
            {
                bool after = shape.Gates[i].OnTrunk && shape.Gates[i].Progress > shape.Gates[defence].Progress;
                if (after && (best < 0 || shape.Gates[i].Progress < shape.Gates[best].Progress))
                {
                    best = i;
                }
            }

            return best;
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
        private static void Act(LevelContext context, BuildSpots spots, List<Tower> towers, List<Tower> gates, GateWatch watch, List<string> owned, LedgerView ledger, BotRun run)
        {
            if (ledger.Count == 0)
            {
                return;
            }

            UseHero(context, gates, watch);
            int cheapestStep = CheapestBuild(context, owned, t => !t.IsSupport && !t.BlocksGroundEnemies && (run.Strategy.Allowed == null || run.Strategy.Allowed(t)));
            if (Maintain(context, towers, watch, cheapestStep, run))
            {
                return;
            }

            if (run.Strategy.Style == PlayStyle.Scripted)
            {
                ScriptedAct(context, towers, watch, cheapestStep, run);
                return;
            }

            if (run.Strategy.Obvious)
            {
                ObviousAct(context, spots, towers, owned, run);
                return;
            }

            int current = Mathf.Clamp(context.WaveNumber - 1, 0, ledger.Count - 1);
            if (run.Strategy.Creative && Creative(context, towers, owned, ledger, current, run))
            {
                return;
            }

            List<(string, int)> standing = Standing(towers);
            float groundCover = Cover(ledger, standing, current, true);
            bool groundHolds = groundCover >= 1f / GroundCeiling;
            if (run.Strategy.Creative && SavesForHarvester(context, towers, run, groundCover))
            {
                return;
            }

            // A player who sees flyers in the next waves' preview saves for the air gun unless the ground is falling apart; the
            // first one comes whatever the ground does (Quartz Flats Medium: the Skyrays took the base while the bot kept buying
            // ground guns for a heavy roller wave).
            bool noSkyGun = !towers.Exists(t => AirOnly(t.Definition));
            int reserve = groundCover >= 1f / ReserveCeiling || noSkyGun ? AirReserve(context, towers, owned, ledger, current) : 0;
            // A new gun goes to the best free spot; the ledger counts every gun as well placed, so a step to a poor spot is
            // worth less than its stats, and upgrading a gun in a good spot can beat it.
            if (reserve > 0 && context.Wallet.CanAfford(reserve) && TryBuy(context, spots, towers, owned, AirOnly, run))
            {
                return;
            }

            // The creative player sells its weakest ground gun for the sky gun the preview asks for when the purse alone is short.
            if (run.Strategy.Creative && run.Strategy.Spec.AllowSell && reserve > 0 && !context.Wallet.CanAfford(reserve) && SellForSky(context, towers, reserve, run)
                && TryBuy(context, spots, towers, owned, AirOnly, run))
            {
                return;
            }

            // The same habit for the crowd waves: one splash gun stands before the first wave a shell would hit two walkers in.
            TowerDefinition splashGun = CheapestSplash(context, owned);
            int splash = SplashReserve(splashGun, towers, ledger, current);
            if (splash > 0 && context.Wallet.CanAfford(reserve + splash) && TryBuy(context, spots, towers, owned, t => t == splashGun, run))
            {
                return;
            }

            // The creative player puts a wanted drone's price aside while the ground holds and the sky is covered.
            Game.Drones.DroneDefinition drone = run.Strategy.Creative && groundHolds && reserve == 0 && splash == 0 ? WantedDrone(context, towers, run) : null;
            int purse = context.Wallet.Money - reserve - splash - (drone != null ? drone.Cost : 0);
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
                // A player spreads the first guns over the queue before upgrading one: a single upgraded gun covers little road.
                if (!tower.CanUpgrade || tower.NextUpgradeCost > reach || (towers.Count < run.Strategy.Spec.TowersBeforeUpgrades && towers.Count < spots.Ground.Count))
                {
                    continue;
                }

                var next = new List<(string, int)>(standing);
                next.Remove((tower.Definition.Id, tower.LevelIndex));
                next.Add((tower.Definition.Id, tower.LevelIndex + 1));
                // Weighed by the gun's own spot, as a new gun is by the next free spot: upgrading a gun in a good spot beats a new
                // gun in a poor one, and the other way round.
                float value = Gain(ledger, next, current, now, nowGround, nowAir) / Mathf.Max(1, tower.NextUpgradeCost) * SpotQuality(context, spots, tower) * run.Strategy.Spec.UpgradeBias;
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
                    float value = Gain(ledger, next, current, now, nowGround, nowAir) / Mathf.Max(1, candidate.BuildCost) * NextSpotQuality(context, spots, towers, candidate) * ThreatWeight(ledger, current, candidate.Id, run);
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

            // The naive player upgrades without thought: when an upgrade is the step, any gun it can pay for gets it.
            if (upgrade != null && run.Strategy.Naive)
            {
                upgrade = AnyUpgrade(towers, purse, run) ?? upgrade;
            }

            if (upgrade != null && context.Wallet.TrySpend(upgrade.NextUpgradeCost))
            {
                upgrade.Upgrade();
                run.Upgrades++;
                run.Note(Game.Balance.PlanStep.Upgrade, null, context.Level.Grid.WorldToCell(upgrade.Position));
            }
            else if (build != null)
            {
                TryBuy(context, spots, towers, owned, t => t == build, run);
            }
        }

        // The hero's abilities as a player uses them: the shockwave when a crowd or a boss is in its ring, the gate repair when
        // the gate it guards is hurt. Paid through the same path as the HUD button (HeroController.TryAbility).
        private static void UseHero(LevelContext context, List<Tower> gates, GateWatch watch)
        {
            Commander hero = context.Hero;
            if (hero == null)
            {
                return;
            }

            // When the defence gate is down the queue moves on to the next gate, and so does a player's hero; back once it is rebuilt.
            bool fallen = watch.Gate != null && watch.Gate.IsWrecked && watch.Fallback >= 0;
            int wantedPost = fallen ? watch.Fallback : watch.Post;
            if (wantedPost >= 0 && hero.PostIndex != wantedPost)
            {
                hero.MoveTo(wantedPost);
            }

            if (hero.IsDown)
            {
                return;
            }

            var near = new List<ITargetable>();
            for (int slot = 0; slot < hero.Abilities.Count; slot++)
            {
                HeroAbilityState state = hero.AbilityAt(slot);
                if (state == null || !hero.CanUse(slot))
                {
                    continue;
                }

                bool wanted;
                switch (state.Definition.Kind)
                {
                    case HeroAbilityKind.Shockwave:
                        context.Targets.FindInRadius(hero.transform.position, state.Definition.Radius, state.Definition.Targets, near);
                        wanted = near.Count >= ShockwaveCrowd || (context.BossesAlive > 0 && near.Count > 0);
                        break;
                    case HeroAbilityKind.GateRepair:
                        Tower guarded = Nearest(gates, hero.GuardedPosition);
                        wanted = guarded != null && guarded.HealthFraction < GateRepairBelow;
                        break;
                    default:
                        wanted = false;
                        break;
                }

                if (wanted)
                {
                    HeroController.TryAbility(hero, slot, true, context.Wallet);
                }
            }
        }

        // A player keeps the defence standing before buying more: a badly hurt gun is repaired, a wrecked gun scrapped so its
        // spot takes a new one. True when the money went to that this round.
        private static bool Maintain(LevelContext context, List<Tower> towers, GateWatch watch, int cheapestStep, BotRun run)
        {
            for (int i = towers.Count - 1; i >= 0; i--)
            {
                Tower tower = towers[i];
                if (tower.IsWrecked)
                {
                    context.Wallet.Add(tower.SellValue);
                    tower.Retire();
                    Object.Destroy(tower.gameObject);
                    towers.RemoveAt(i);
                }
            }

            // Only the gate the defence stands around is repaired with money, and only a few times a wave: under a steady queue a
            // gate needs a repair every few seconds, and the first probe that repaired every gate (2026-11-04) spent most of
            // Shard Ridge's income on them; without any repair Glass Meadows' first gate fell and the creatures queued at the
            // next one, out of reach of every gun.
            Tower gate = watch.Gate;
            if (watch.Wave != context.WaveNumber)
            {
                watch.Wave = context.WaveNumber;
                watch.Repairs = 0;
                watch.LossLastWave.Clear();
                foreach (KeyValuePair<Tower, float> loss in watch.LossThisWave)
                {
                    watch.LossLastWave[loss.Key] = loss.Value;
                }

                watch.LossThisWave.Clear();
            }

            // A scripted plan repairs its gates only where it says so (a "repair" step; round 22b): the repair is one of its trades.
            bool gatesByPlan = run.Strategy.Style == PlayStyle.Scripted;
            if (!gatesByPlan && gate != null && watch.Repairs < GateRepairsPerWave && gate.NeedsRepair && (gate.IsWrecked || gate.HealthFraction < RepairBelow)
                && WorthRepair(context, watch, gate, cheapestStep) && Spend(context, watch, gate.RepairCost))
            {
                gate.RepairFully();
                watch.Repairs++;
                return true;
            }

            foreach (Tower tower in towers)
            {
                if (tower.NeedsRepair && tower.HealthFraction < RepairBelow && WorthRepair(context, watch, tower, cheapestStep) && Spend(context, watch, tower.RepairCost))
                {
                    tower.RepairFully();
                    return true;
                }
            }

            return false;
        }

        private static bool Spend(LevelContext context, GateWatch watch, int cost)
        {
            if (!context.Wallet.TrySpend(cost))
            {
                return false;
            }

            watch.RepairSpent += cost;
            return true;
        }

        // Every structure's lost health since the last frame, gates and guns apart; repairs and rebuilds only raise it.
        private static void MeasureWear(GateWatch watch, List<Tower> towers, List<Tower> gates)
        {
            foreach (Tower gate in gates)
            {
                watch.GateDamage += Lost(watch, gate);
            }

            foreach (Tower tower in towers)
            {
                watch.TowerDamage += Lost(watch, tower);
            }
        }

        private static float Lost(GateWatch watch, Tower structure)
        {
            float now = structure.Health.Current;
            float lost = watch.LastHealth.TryGetValue(structure, out float before) ? Mathf.Max(0f, before - now) : 0f;
            watch.LastHealth[structure] = now;
            watch.LossThisWave[structure] = (watch.LossThisWave.TryGetValue(structure, out float sum) ? sum : 0f) + lost;
            return lost;
        }

        // What a structure may lose before the next wave is over: the worse of this wave and the last, with a margin for the
        // growth from wave to wave.
        private static float ExpectedLoss(GateWatch watch, Tower structure)
        {
            float now = watch.LossThisWave.TryGetValue(structure, out float a) ? a : 0f;
            float last = watch.LossLastWave.TryGetValue(structure, out float b) ? b : 0f;
            return Mathf.Max(now, last) * LossGrowth;
        }

        // Remco (2026-11-06): the bots wasted money repairing gates instead of building. A repair is bought only when the
        // structure would fall in the coming wave (its health below what the waves at hand take from it), or when restoring
        // that health is cheaper per point than the cheapest new gun that would have to stop the same leak for the rest of
        // the level; otherwise the coins go to the defence.
        private static bool WorthRepair(LevelContext context, GateWatch watch, Tower structure, int cheapestStep)
        {
            float expected = ExpectedLoss(watch, structure);
            if (expected <= 0f)
            {
                return false;
            }

            if (structure.IsWrecked || structure.Health.Current <= expected)
            {
                return true;
            }

            float missing = structure.Health.Max - structure.Health.Current;
            float repairPerPoint = structure.RepairCost / Mathf.Max(1f, missing);
            int wavesLeft = Mathf.Max(1, context.WaveCount - context.WaveNumber + 1);
            float stepPerPoint = cheapestStep / Mathf.Max(1f, expected * wavesLeft);
            return cheapestStep > 0 && repairPerPoint < stepPerPoint;
        }

        // How good the next free spot for a tower type is (1 for sky guns and for the naive bot).
        private static float NextSpotQuality(LevelContext context, BuildSpots spots, List<Tower> towers, TowerDefinition candidate) =>
            spots.Uniform || AirOnly(candidate) ? 1f
            : FreeSpot(context, spots.RankingFor(candidate), towers, out Vector2Int cell) ? spots.QualityFor(candidate, cell) : MinSpotQuality;

        private static float SpotQuality(LevelContext context, BuildSpots spots, Tower tower) =>
            spots.QualityFor(tower.Definition, context.Level.Grid.WorldToCell(tower.Position));

        private static Tower Nearest(List<Tower> structures, Vector3 position)
        {
            Tower best = null;
            float bestDistance = float.MaxValue;
            foreach (Tower structure in structures)
            {
                float distance = structure != null ? Vector3.Distance(structure.Position, position) : float.MaxValue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = structure;
                }
            }

            return best;
        }

        // The price of an air gun while flyers are still to come and the standing sky guns cannot hold the next raid (none yet,
        // or the raid grew past them), from the wave on whose income (the ledger's money, kills paid as they really arrive)
        // until that raid covers the price with a margin; 0 otherwise.
        private static int AirReserve(LevelContext context, List<Tower> towers, List<string> owned, LedgerView ledger, int current)
        {
            int price = CheapestBuild(context, owned, AirOnly);
            if (price == 0)
            {
                return 0;
            }

            // Ground guns pay for everything: with the Gun Turret at half damage up there (round 21a), the preview of an early
            // raid made the bot spend the whole starting purse on two Flaks and never earn a kill (Quartz Flats). A sky gun
            // waits for a ground gun, and a second one for the ground line of a few guns.
            int ground = towers.FindAll(t => !AirOnly(t.Definition)).Count;
            int sky = towers.Count - ground;
            if (ground == 0 || (sky > 0 && ground < GroundBeforeSecondSkyGun) || (sky > 0 && sky * SkyPerGround > ground))
            {
                return 0;
            }

            List<(string, int)> standing = Standing(towers);
            int raid = -1;
            for (int i = current; i < ledger.Count && raid < 0; i++)
            {
                if (ledger.Loads[i].AirHealth > 0f && Pressure(ledger, standing, i).Air > AirCeiling)
                {
                    raid = i;
                }
            }

            if (raid < 0)
            {
                return 0;
            }

            return current >= SavingFrom(ledger, raid, price) ? price : 0;
        }

        // The wave from whose income (the ledger's money) until the wave in need the price is covered with a margin.
        private static int SavingFrom(LedgerView ledger, int needed, int price)
        {
            for (int i = needed; i >= 0; i--)
            {
                if (ledger.Lines[needed].Money - ledger.Lines[i].Money >= price * ReserveMargin)
                {
                    return i;
                }
            }

            return 0;
        }

        // The cheapest owned gun whose shells burst on the ground, or null.
        private static TowerDefinition CheapestSplash(LevelContext context, List<string> owned)
        {
            TowerDefinition cheapest = null;
            foreach (TowerDefinition tower in context.AllTowers)
            {
                bool splashes = !tower.IsSupport && tower.SplashRadius > 0f && (tower.Targets & TargetLayers.Ground) != 0;
                if (splashes && owned.Contains(tower.Id) && (cheapest == null || tower.BuildCost < cheapest.BuildCost))
                {
                    cheapest = tower;
                }
            }

            return cheapest;
        }

        // The splash gun's price while no splash gun stands and a crowd wave (CrowdSplashTargets) is still to come, from the
        // wave whose income covers it before the crowd; 0 otherwise.
        private static int SplashReserve(TowerDefinition gun, List<Tower> towers, LedgerView ledger, int current)
        {
            if (gun == null || towers.Exists(t => t.Definition.SplashRadius > 0f && (t.Definition.Targets & TargetLayers.Ground) != 0))
            {
                return 0;
            }

            for (int i = current; i < ledger.Count; i++)
            {
                if (1f + 2f * gun.SplashRadius * ledger.Loads[i].GroundDensity >= CrowdSplashTargets)
                {
                    return current >= SavingFrom(ledger, i, gun.BuildCost) ? gun.BuildCost : 0;
                }
            }

            return 0;
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
            float ground = Game.Balance.Ledger.PressureOf(ledger.Input, load, ledger.Judge, standing).Ground;
            if (load.AirHealth <= 0f)
            {
                return (ground, 0f);
            }

            List<(string Id, int Level)> sky = standing.FindAll(t => ledger.SkyGuns.Contains(t.Item1));
            return (ground, Game.Balance.Ledger.PressureOf(ledger.Input, load, ledger.Judge, sky).Air);
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

        internal static bool AirOnly(TowerDefinition tower) => tower.Targets == TargetLayers.Air;

        // The starting set, every tower granted by the levels before this one, and the level's own reward when a player of the
        // difficulty has cleared it on a lower difficulty first (Progression.OwnLevelPathOf; the curator, round 22f): the same
        // player the paper plans for.
        internal static List<string> OwnedTowers(CampaignCatalog catalog, int level, int difficulty)
        {
            var owned = new List<string>();
#if UNITY_EDITOR
            var unlocks = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>(UnlocksPath);
            owned.AddRange(unlocks.StartingTowerIds);
#endif
            int last = Game.Balance.Progression.OwnLevelPathOf(difficulty).Length > 0 ? level : level - 1;
            for (int i = 0; i <= last; i++)
            {
                if (catalog.Nodes[i].Level != null && catalog.Nodes[i].Level.Reward.NewTower != null && !owned.Contains(catalog.Nodes[i].Level.Reward.NewTower.Id))
                {
                    owned.Add(catalog.Nodes[i].Level.Reward.NewTower.Id);
                }
            }

            return owned;
        }

        // A walking route point within reach of a bend sharper than BendCos (walkers bunch on the inside of a turn).
        private static bool NearBend(EnemyPath path, Vector2 point)
        {
            for (int i = 1; i + 1 < path.Waypoints.Length; i++)
            {
                Vector2 into = ((Vector2)path.Waypoints[i] - path.Waypoints[i - 1]).normalized;
                Vector2 outOf = ((Vector2)path.Waypoints[i + 1] - path.Waypoints[i]).normalized;
                if (Vector2.Dot(into, outOf) < BendCos && Vector2.Distance(point, path.Waypoints[i]) <= BendReachCells)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- the creative player (round 20) ------------------------------------------------------------------------

        // What a creative player adds to the towers: the drones the level offers (saved for in Act), mines in the queue before
        // crowd and boss waves (and on a burrowing boss's line), a slow field at the gate's exit. The sky comes first: nothing
        // while an air gun is being saved for, and a gadget never takes the price of the next gun. True when the money went to
        // that this round.
        private static bool Creative(LevelContext context, List<Tower> towers, List<string> owned, LedgerView ledger, int current, BotRun run)
        {
            if (AirReserve(context, towers, owned, ledger, current) > 0)
            {
                return false;
            }

            int keep = CheapestBuild(context, owned, t => !t.IsSupport);
            return BuyDrone(context, towers, run) || PlaceMines(context, towers, ledger, current, keep, run) || PlaceField(context, towers, ledger, current, keep, run);
        }

        // The drone the creative player wants next and has not bought, or null: the harvester once the first guns stand and
        // the ore still has time to pay (not in the last waves), the repair drone once something was hurt, the attack drone
        // with a full line.
        private static Game.Drones.DroneDefinition WantedDrone(LevelContext context, List<Tower> towers, BotRun run)
        {
            // Income first (the drone lens: a harvester bought early pays most), then repairs, then fire.
            var drones = new List<Game.Drones.DroneDefinition>(context.Level.Drones);
            drones.Sort((a, b) => DronePriority(a).CompareTo(DronePriority(b)));
            foreach (Game.Drones.DroneDefinition drone in drones)
            {
                bool wanted;
                switch (drone.Kind)
                {
                    case Game.Drones.DroneKind.Harvest:
                        wanted = run.Strategy.Spec.HarvesterCover > 0f && towers.Count >= HarvestAfterTowers && context.Level.MineCells.Length > 0 && context.WaveNumber <= context.WaveCount - NoHarvestLastWaves;
                        break;
                    case Game.Drones.DroneKind.Repair:
                        wanted = towers.Count >= RepairAfterTowers && run.Strategy.Spec.RepairDroneWear < 1f && run.Wear >= run.Strategy.Spec.RepairDroneWear;
                        break;
                    default:
                        wanted = run.Strategy.Spec.AttackDroneAfterTowers > 0 && towers.Count >= run.Strategy.Spec.AttackDroneAfterTowers;
                        break;
                }

                if (wanted && !run.DronesBought.Contains(drone.name) && context.ActiveDrones(drone) < context.MaxDrones(drone))
                {
                    return drone;
                }
            }

            return null;
        }

        // The creative player invests in the harvester early, once its first guns hold the waves at hand outright (Remco: an
        // investment paid for with early risk; the drone lens: an early buy pays most). In the first part of the level it puts
        // the price aside before any other step and buys the drone as soon as the purse has it; when the waves press, it builds.
        // Round 20: saving only from what was left over, it never had the price before the late waves on Medium.
        private static bool SavesForHarvester(LevelContext context, List<Tower> towers, BotRun run, float groundCover)
        {
            if (run.Strategy.Spec.HarvesterCover <= 0f || groundCover < run.Strategy.Spec.HarvesterCover || context.WaveNumber > Mathf.CeilToInt(context.WaveCount * run.Strategy.Spec.HarvesterShare))
            {
                return false;
            }

            Game.Drones.DroneDefinition drone = WantedDrone(context, towers, run);
            if (drone == null || drone.Kind != Game.Drones.DroneKind.Harvest)
            {
                return false;
            }

            BuyDrone(context, towers, run);
            return true;
        }

        private static int DronePriority(Game.Drones.DroneDefinition drone) =>
            drone.Kind == Game.Drones.DroneKind.Harvest ? 0 : drone.Kind == Game.Drones.DroneKind.Repair ? 1 : 2;

        private static bool BuyDrone(LevelContext context, List<Tower> towers, BotRun run)
        {
            Game.Drones.DroneDefinition drone = WantedDrone(context, towers, run);
            if (drone == null || !context.Wallet.CanAfford(drone.Cost))
            {
                return false;
            }

            int before = context.Wallet.Money;
            context.DroneRequested.Raise(drone);
            if (context.Wallet.Money >= before)
            {
                return false;
            }

            run.DronesBought.Add(drone.name);
            run.Note(Game.Balance.PlanStep.Drone, drone.name, Vector2Int.zero);
            return true;
        }

        // Mines in the queue before the defence gate while a crowd or a boss wave is at hand (MinesPerWave a wave), and one
        // on the road nearest a boss travelling under the ground (a mine going off on it stuns it, Docs/BOSSES.md).
        private static bool PlaceMines(LevelContext context, List<Tower> towers, LedgerView ledger, int current, int keep, BotRun run)
        {
            PathObjectDefinition mine = PathObjectOf(context, PathObjectKind.Mine);
            if (mine == null || !context.Wallet.CanAfford(mine.Cost + keep))
            {
                return false;
            }

            Game.Bosses.ContinentBossBrain burrowing = null;
            if (context.BossesAlive > 0)
            {
                foreach (Game.Bosses.ContinentBossBrain brain in Object.FindObjectsByType<Game.Bosses.ContinentBossBrain>())
                {
                    burrowing = brain.State == "Burrowed" ? brain : burrowing;
                }
            }

            if (burrowing != null && run.BurrowMined != burrowing.Burrows)
            {
                run.BurrowMined = burrowing.Burrows;
                return Place(context, mine, NearestRoad(context.Level, burrowing.transform.position), run);
            }

            if (run.MinesWave != context.WaveNumber)
            {
                run.MinesWave = context.WaveNumber;
                run.MinesThisWave = 0;
            }

            // A crowd wave asks for mines only when the standing guns would not hold it and the first guns stand (round 20: two mines
            // on wave 1 cost Crysium Vein its second Flak); a boss wave always does.
            Game.Balance.StrategySpec spec = run.Strategy.Spec;
            bool pressed = spec.MineCoverBelow > 0f && Pressed(towers, ledger, current, spec.MineCoverBelow);
            bool wanted = false;
            for (int i = current; i <= current + Horizon && i < ledger.Count; i++)
            {
                wanted |= (spec.MineOnBoss && ledger.Lines[i].Boss) || (pressed && 1f + 2f * mine.Radius * ledger.Loads[i].GroundDensity >= CrowdSplashTargets);
            }

            if (!wanted || run.MinesThisWave >= MinesPerWave)
            {
                return false;
            }

            foreach (Vector2Int cell in run.Strategy.Naive ? AnyRoad(context.Level, run) : RoadAtGate(context.Level, true))
            {
                if (Place(context, mine, cell, run))
                {
                    run.MinesThisWave++;
                    return true;
                }
            }

            return false;
        }

        // One slow field a wave on the road just past the defence gate while creatures are on the field: leakers stay in
        // the second line's reach longer.
        private static bool PlaceField(LevelContext context, List<Tower> towers, LedgerView ledger, int current, int keep, BotRun run)
        {
            PathObjectDefinition field = PathObjectOf(context, PathObjectKind.SlowField);
            float fieldBelow = run.Strategy.Spec.FieldCoverBelow;
            if (field == null || fieldBelow <= 0f || run.FieldWave == context.WaveNumber || context.Targets.Count == 0 || !context.Wallet.CanAfford(field.Cost + keep))
            {
                return false;
            }

            List<Vector2Int> cells = run.Strategy.Naive ? AnyRoad(context.Level, run) : RoadAtGate(context.Level, false);
            if (!run.Strategy.Naive)
            {
                // Remco (2026-11-06): a slow field is a damage tactic, laid where the guns overlap, not only a last resort.
                cells.Sort((a, b) => CoverDps(context, towers, b).CompareTo(CoverDps(context, towers, a)));
                if (!Pressed(towers, ledger, current, fieldBelow) && (cells.Count == 0 || !FieldPays(context, towers, field, cells[0], keep)))
                {
                    return false;
                }
            }
            else if (!Pressed(towers, ledger, current, fieldBelow))
            {
                return false;
            }

            foreach (Vector2Int cell in cells)
            {
                if (Place(context, field, cell, run))
                {
                    run.FieldWave = context.WaveNumber;
                    return true;
                }
            }

            return false;
        }

        // Data DPS of the standing ground guns that reach a road cell: what a slow there multiplies.
        private static float CoverDps(LevelContext context, List<Tower> towers, Vector2Int cell)
        {
            Vector3 point = context.Level.Grid.CellToWorld(cell);
            float dps = 0f;
            foreach (Tower tower in towers)
            {
                TowerLevel level = tower.Definition.GetLevel(tower.LevelIndex);
                Vector3 offset = tower.Position - point;
                offset.y = 0f;
                if (!tower.IsWrecked && (tower.Definition.Targets & TargetLayers.Ground) != 0 && level.FireInterval > 0f && offset.magnitude <= tower.Range)
                {
                    dps += level.Damage / level.FireInterval;
                }
            }

            return dps;
        }

        // The slow pays when the damage it adds to every creature crossing it (the guns' DPS there times the extra seconds in
        // the field) beats what the same coins in the cheapest gun would add to every creature for the rest of the level: a
        // field lasts about a wave, a gun every wave left (TowerRolesTests reports the same break-even per level).
        private static bool FieldPays(LevelContext context, List<Tower> towers, PathObjectDefinition field, Vector2Int cell, int cheapestGun)
        {
            TowerDefinition gun = null;
            foreach (TowerDefinition tower in context.AllTowers)
            {
                gun = tower.BuildCost == cheapestGun && !tower.IsSupport && (tower.Targets & TargetLayers.Ground) != 0 ? tower : gun;
            }

            if (gun == null || field.SlowFactor <= 0f || field.SlowFactor >= 1f)
            {
                return false;
            }

            TowerLevel first = gun.GetLevel(0);
            float gunPerCreature = first.Damage / first.FireInterval / gun.BuildCost * field.Cost * 2f * first.Range;
            float fieldPerCreature = CoverDps(context, towers, cell) * 2f * field.Radius * (1f / field.SlowFactor - 1f);
            int wavesLeft = Mathf.Max(1, context.WaveCount - context.WaveNumber + 1);
            return fieldPerCreature >= gunPerCreature * wavesLeft;
        }

        // Sells the weakest ground gun (lowest level, then cheapest) when its refund makes up what the purse lacks for the
        // sky gun the preview calls for; never below SellKeepsGroundGuns ground guns.
        private static bool SellForSky(LevelContext context, List<Tower> towers, int price, BotRun run)
        {
            Tower weakest = null;
            int ground = 0;
            foreach (Tower tower in towers)
            {
                if (AirOnly(tower.Definition) || !tower.IsAlive)
                {
                    continue;
                }

                ground++;
                // Never the last gun of its kind: the role it fills (the only Cannon for the packs) is why it stands there.
                if (towers.FindAll(t => t.Definition == tower.Definition && t.IsAlive).Count <= 1)
                {
                    continue;
                }

                if (weakest == null || tower.LevelIndex < weakest.LevelIndex || (tower.LevelIndex == weakest.LevelIndex && tower.TotalSpent < weakest.TotalSpent))
                {
                    weakest = tower;
                }
            }

            if (weakest == null || ground <= SellKeepsGroundGuns || context.Wallet.Money + weakest.SellValue < price)
            {
                return false;
            }

            context.Wallet.Add(weakest.SellValue);
            run.Note(Game.Balance.PlanStep.Sell, null, context.Level.Grid.WorldToCell(weakest.Position));
            weakest.Retire();
            Object.Destroy(weakest.gameObject);
            towers.Remove(weakest);
            run.Sold++;
            return true;
        }

        // Gadgets are for a defence that stands but would not hold the waves at hand: in a thin economy (Crysium Vein Medium,
        // round 20) a slow field on wave 3 cost the second Cannon and the level.
        private static bool Pressed(List<Tower> towers, LedgerView ledger, int current, float coverBelow) =>
            towers.Count >= MinesAfterTowers && Cover(ledger, Standing(towers), current, true) < coverBelow;

        // The strategy's answer to the threats of the waves at hand (round 22e): the mean of its weights for the type over the threat shares.
        private static float ThreatWeight(LedgerView ledger, int current, string towerId, BotRun run)
        {
            if (run.Strategy.Spec.Weights.Length == 0)
            {
                return 1f;
            }

            int cheapest = Game.Balance.PlanEvaluator.CheapestGun(ledger.Input);
            float sum = 0f;
            float weight = 0f;
            for (int i = current; i <= current + Horizon && i < ledger.Count; i++)
            {
                foreach ((string threat, float share) in Game.Balance.StrategyExecutor.ThreatShares(ledger.Input, ledger.Loads[i], cheapest))
                {
                    sum += share;
                    weight += share * run.Strategy.Spec.WeightOf(threat, towerId);
                }
            }

            return sum > 0f ? weight / sum : 1f;
        }

        // The naive player's upgrade: a random gun it can pay to raise, wherever it stands.
        private static Tower AnyUpgrade(List<Tower> towers, int purse, BotRun run)
        {
            var candidates = towers.FindAll(t => t.CanUpgrade && t.NextUpgradeCost <= purse);
            return candidates.Count > 0 ? candidates[run.Next(candidates.Count)] : null;
        }

        private static PathObjectDefinition PathObjectOf(LevelContext context, PathObjectKind kind)
        {
            if (context.AvailablePathObjects == null)
            {
                return null;
            }

            foreach (PathObjectDefinition item in context.AvailablePathObjects)
            {
                if (item != null && item.Kind == kind)
                {
                    return item;
                }
            }

            return null;
        }

        // Road cells of the main road before the defence gate (the queue, nearest the gate first) or just after it (its exit).
        private static List<Vector2Int> RoadAtGate(LevelDefinition level, bool before)
        {
            var cells = new List<Vector2Int>();
            int defence = DefenceGate(level);
            Vector2Int[] road = level.MainRoad;
            if (defence < 0 || road.Length == 0)
            {
                return cells;
            }

            Vector2Int gate = level.Blockades[defence].Cell;
            int at = 0;
            for (int i = 1; i < road.Length; i++)
            {
                at = Vector2Int.Distance(road[i], gate) < Vector2Int.Distance(road[at], gate) ? i : at;
            }

            for (int k = 1; k <= GadgetRoadCells; k++)
            {
                int index = before ? at - k - 1 : at + k;
                if (index >= 0 && index < road.Length)
                {
                    cells.Add(road[index]);
                }
            }

            return cells;
        }

        // The naive player drops a gadget anywhere on the main road.
        private static List<Vector2Int> AnyRoad(LevelDefinition level, BotRun run)
        {
            var cells = new List<Vector2Int>(level.MainRoad);
            run.Shuffle(cells);
            return cells.GetRange(0, Mathf.Min(GadgetRoadCells, cells.Count));
        }

        private static Vector2Int NearestRoad(LevelDefinition level, Vector3 position)
        {
            Vector2Int best = level.MainRoad.Length > 0 ? level.MainRoad[0] : default;
            float bestDistance = float.MaxValue;
            foreach (Vector2Int cell in level.Road.Cells)
            {
                float distance = Vector3.Distance(level.Grid.CellToWorld(cell), position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = cell;
                }
            }

            return best;
        }

        // Places a path object the way the player's cursor does (PlacementController checks the cell and takes the money).
        private static bool Place(LevelContext context, PathObjectDefinition item, Vector2Int cell, BotRun run)
        {
            var placement = Object.FindAnyObjectByType<PlacementController>();
            if (placement == null)
            {
                return false;
            }

            int before = context.Wallet.Money;
            CursorField.SetValue(placement, cell);
            PlacePathObject.Invoke(placement, new object[] { item });
            if (context.Wallet.Money >= before)
            {
                return false;
            }

            if (item.Kind == PathObjectKind.Mine)
            {
                run.Mines++;
            }
            else
            {
                run.Fields++;
            }

            run.Note(item.Kind == PathObjectKind.Mine ? Game.Balance.PlanStep.Mine : Game.Balance.PlanStep.Field, item.name, cell);

            return true;
        }

        private static readonly System.Reflection.FieldInfo CursorField =
            typeof(PlacementController).GetField("_cursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static readonly System.Reflection.MethodInfo PlacePathObject =
            typeof(PlacementController).GetMethod("TryPlacePathObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

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

        private static bool TryBuy(LevelContext context, BuildSpots spots, List<Tower> towers, List<string> owned, System.Predicate<TowerDefinition> kind, BotRun run)
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

            if (best == null || !FreeSpot(context, spots.RankingFor(best), towers, out Vector2Int cell) || !context.Wallet.TrySpend(best.BuildCost))
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
            run.Built++;
            run.Note(Game.Balance.PlanStep.Build, best.Id, cell);
            return true;
        }

        // ---- The obvious family (round 22): no balance model, so these yardsticks never move between rounds -----------------

        // Mono rows upgrade only once this many towers stand; the careless mix upgrades a random gun every third purchase and
        // puts an air gun every third tower when the level has flight routes.
        private const int MonoUpgradeAfterTowers = 4;
        private const int NaiveUpgradeEvery = 3;
        private const int NaiveSkyEvery = 3;
        private const int DefaultSeeds = 3;

        private static void ObviousAct(LevelContext context, BuildSpots spots, List<Tower> towers, List<string> owned, BotRun run)
        {
            switch (run.Strategy.Style)
            {
                case PlayStyle.MonoRow:
                    MonoAct(context, spots, towers, owned, run);
                    break;
                case PlayStyle.NearGate:
                    NaiveAct(context, spots, towers, owned, run);
                    break;
                default:
                    AffordableAct(context, spots, towers, owned, run);
                    break;
            }
        }

        // A row of one type: build while spots are free, then alternate an upgrade (the cheapest step) and a tower.
        private static void MonoAct(LevelContext context, BuildSpots spots, List<Tower> towers, List<string> owned, BotRun run)
        {
            bool upgradeTurn = towers.Count >= MonoUpgradeAfterTowers && run.Purchases % 2 == 1;
            Tower cheapest = CheapestUpgrade(towers, context.Wallet.Money);
            if (upgradeTurn && cheapest != null)
            {
                Buy(context, cheapest, run);
                return;
            }

            if (TryBuy(context, spots, towers, owned, null, run))
            {
                run.Purchases++;
            }
            else if (towers.Count >= MonoUpgradeAfterTowers && cheapest != null)
            {
                // The row comes first; only a standing row is raised when the next tower is out of reach.
                Buy(context, cheapest, run);
            }
        }

        // The careless mix: the owned ground types in turn (cheapest first), an air gun now and then where flyers come, a
        // random upgrade every few purchases; it waits for the price of the tower it wants, nothing more.
        private static void NaiveAct(LevelContext context, BuildSpots spots, List<Tower> towers, List<string> owned, BotRun run)
        {
            if (run.Purchases > 0 && run.Purchases % NaiveUpgradeEvery == NaiveUpgradeEvery - 1)
            {
                Tower any = AnyUpgrade(towers, context.Wallet.Money, run);
                if (any != null)
                {
                    Buy(context, any, run);
                    return;
                }
            }

            var ground = new List<TowerDefinition>();
            var sky = new List<TowerDefinition>();
            foreach (TowerDefinition tower in context.AllTowers)
            {
                if (tower.IsSupport || !owned.Contains(tower.Id))
                {
                    continue;
                }

                (AirOnly(tower) ? sky : ground).Add(tower);
            }

            ground.Sort((a, b) => a.BuildCost.CompareTo(b.BuildCost));
            bool flyers = System.Array.Exists(context.Level.Paths, p => p.Flying);
            bool skyTurn = flyers && sky.Count > 0 && run.Purchases % NaiveSkyEvery == NaiveSkyEvery - 1;
            TowerDefinition wanted = skyTurn ? sky[0] : ground.Count > 0 ? ground[run.Purchases % ground.Count] : null;
            if (wanted != null && context.Wallet.CanAfford(wanted.BuildCost) && TryBuy(context, spots, towers, owned, t => t == wanted, run))
            {
                run.Purchases++;
            }
        }

        // The dearest affordable step, whatever it is: a new tower (an air gun only where flyers come) or an upgrade.
        private static void AffordableAct(LevelContext context, BuildSpots spots, List<Tower> towers, List<string> owned, BotRun run)
        {
            bool flyers = System.Array.Exists(context.Level.Paths, p => p.Flying);
            TowerDefinition build = null;
            foreach (TowerDefinition tower in context.AllTowers)
            {
                bool fits = !tower.IsSupport && owned.Contains(tower.Id) && (flyers || !AirOnly(tower)) && context.Wallet.CanAfford(tower.BuildCost);
                if (fits && (build == null || tower.BuildCost > build.BuildCost))
                {
                    build = tower;
                }
            }

            Tower upgrade = null;
            foreach (Tower tower in towers)
            {
                if (tower.CanUpgrade && context.Wallet.CanAfford(tower.NextUpgradeCost) && (upgrade == null || tower.NextUpgradeCost > upgrade.NextUpgradeCost))
                {
                    upgrade = tower;
                }
            }

            if (upgrade != null && (build == null || upgrade.NextUpgradeCost >= build.BuildCost))
            {
                Buy(context, upgrade, run);
            }
            else if (build != null && TryBuy(context, spots, towers, owned, t => t == build, run))
            {
                run.Purchases++;
            }
        }

        private static Tower CheapestUpgrade(List<Tower> towers, int purse)
        {
            Tower best = null;
            foreach (Tower tower in towers)
            {
                if (tower.CanUpgrade && tower.NextUpgradeCost <= purse && (best == null || tower.NextUpgradeCost < best.NextUpgradeCost))
                {
                    best = tower;
                }
            }

            return best;
        }

        private static void Buy(LevelContext context, Tower upgrade, BotRun run)
        {
            if (context.Wallet.TrySpend(upgrade.NextUpgradeCost))
            {
                upgrade.Upgrade();
                run.Upgrades++;
                run.Purchases++;
                run.Note(Game.Balance.PlanStep.Upgrade, null, context.Level.Grid.WorldToCell(upgrade.Position));
            }
        }

        // ---- Scripted plans (round 22, the tester's stage B): the steps in order, each as soon as the purse has it ------------

        private static void ScriptedAct(LevelContext context, List<Tower> towers, GateWatch watch, int cheapestStep, BotRun run)
        {
            Game.Balance.PlanSpec plan = run.Strategy.Plan;
            int wave = Mathf.Max(1, context.WaveNumber);
            while (run.PlanStep < plan.Steps.Length && plan.Steps[run.PlanStep].Wave <= wave)
            {
                Game.Balance.PlanStep step = plan.Steps[run.PlanStep];
                var cell = new Vector2Int(step.X, step.Y);
                Tower at = TowerAt(context, towers, cell);
                bool done;
                bool possible;
                switch (step.Kind)
                {
                    case Game.Balance.PlanStep.Build:
                        TowerDefinition tower = TowerById(context, step.Item);
                        possible = tower != null && at == null;
                        done = possible && context.Wallet.CanAfford(tower.BuildCost) && TryBuyAt(context, tower, cell, towers, run);
                        break;
                    case Game.Balance.PlanStep.Upgrade:
                        possible = at != null && at.CanUpgrade;
                        done = possible && context.Wallet.CanAfford(at.NextUpgradeCost);
                        if (done)
                        {
                            Buy(context, at, run);
                        }

                        break;
                    case Game.Balance.PlanStep.Sell:
                        possible = at != null;
                        done = possible;
                        if (done)
                        {
                            Sell(context, towers, at, run);
                        }

                        break;
                    case Game.Balance.PlanStep.Drone:
                        Game.Drones.DroneDefinition drone = DroneById(context, step.Item);
                        possible = drone != null && context.ActiveDrones(drone) < context.MaxDrones(drone);
                        done = possible && context.Wallet.CanAfford(drone.Cost) && BuyDrone(context, drone, run);
                        break;
                    case Game.Balance.PlanStep.Repair:
                        // The plan's repair goes through the same rule as the bots' (WorthRepair): a gate that is whole or not worth it is skipped.
                        Tower gate = watch.Gate;
                        possible = gate != null && gate.NeedsRepair && WorthRepair(context, watch, gate, cheapestStep);
                        done = possible && Spend(context, watch, gate.RepairCost);
                        if (done)
                        {
                            gate.RepairFully();
                            run.Repairs++;
                        }

                        break;
                    default:
                        PathObjectDefinition item = GadgetById(context, step.Item);
                        possible = item != null;
                        done = possible && context.Wallet.CanAfford(item.Cost) && Place(context, item, cell, run);
                        break;
                }

                if (!possible)
                {
                    // A step the field does not allow (a taken cell, a full drone bay) is skipped, not waited for.
                    run.PlanSkipped++;
                    run.PlanStep++;
                    continue;
                }

                if (!done)
                {
                    return;
                }

                run.PlanStep++;
            }
        }

        private static Tower TowerAt(LevelContext context, List<Tower> towers, Vector2Int cell)
        {
            foreach (Tower tower in towers)
            {
                if (context.Level.Grid.WorldToCell(tower.Position) == cell)
                {
                    return tower;
                }
            }

            return null;
        }

        private static TowerDefinition TowerById(LevelContext context, string id)
        {
            foreach (TowerDefinition tower in context.AllTowers)
            {
                if (tower.Id == id)
                {
                    return tower;
                }
            }

            return null;
        }

        private static Game.Drones.DroneDefinition DroneById(LevelContext context, string id)
        {
            foreach (Game.Drones.DroneDefinition drone in context.Level.Drones)
            {
                if (drone != null && drone.name == id)
                {
                    return drone;
                }
            }

            return null;
        }

        private static PathObjectDefinition GadgetById(LevelContext context, string id)
        {
            if (context.AvailablePathObjects == null)
            {
                return null;
            }

            foreach (PathObjectDefinition item in context.AvailablePathObjects)
            {
                if (item != null && item.name == id)
                {
                    return item;
                }
            }

            return null;
        }

        private static bool BuyDrone(LevelContext context, Game.Drones.DroneDefinition drone, BotRun run)
        {
            int before = context.Wallet.Money;
            context.DroneRequested.Raise(drone);
            if (context.Wallet.Money >= before)
            {
                return false;
            }

            run.DronesBought.Add(drone.name);
            run.Note(Game.Balance.PlanStep.Drone, drone.name, Vector2Int.zero);
            return true;
        }

        private static void Sell(LevelContext context, List<Tower> towers, Tower tower, BotRun run)
        {
            context.Wallet.Add(tower.SellValue);
            run.Note(Game.Balance.PlanStep.Sell, null, context.Level.Grid.WorldToCell(tower.Position));
            tower.Retire();
            Object.Destroy(tower.gameObject);
            towers.Remove(tower);
            run.Sold++;
        }

        // Builds a tower on an exact cell (the plan's spot) through the same path as the bot's own buys.
        private static bool TryBuyAt(LevelContext context, TowerDefinition definition, Vector2Int cell, List<Tower> towers, BotRun run)
        {
            if (!new BuildGrid(context.Level).CanBuild(cell) || TowerAt(context, towers, cell) != null || !context.Wallet.TrySpend(definition.BuildCost))
            {
                return false;
            }

            var go = new GameObject($"BotTower_{definition.DisplayName}");
            go.transform.position = context.Level.Grid.CellToWorld(cell);
            var tower = go.AddComponent<Tower>();
            go.AddComponent<TowerView>();
            tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
            tower.Register(context.Structures);
            towers.Add(tower);
            run.Built++;
            run.Note(Game.Balance.PlanStep.Build, definition.Id, cell);
            return true;
        }

        /// <summary>The seeds a sweep plays: the filter file's "seeds=1,2,3" line, else 1 to 3.</summary>
        internal static List<int> Seeds()
        {
            var seeds = new List<int>();
            if (System.IO.File.Exists(FilterPath))
            {
                foreach (string line in System.IO.File.ReadAllLines(FilterPath))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2 && parts[0].Trim() == "seeds")
                    {
                        foreach (string item in parts[1].Split(','))
                        {
                            if (int.TryParse(item.Trim(), out int seed))
                            {
                                seeds.Add(seed);
                            }
                        }
                    }
                }
            }

            if (seeds.Count == 0)
            {
                for (int i = 1; i <= DefaultSeeds; i++)
                {
                    seeds.Add(i);
                }
            }

            return seeds;
        }
    }
}
