using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>A level's own numbers that do not change with the campaign position: map, money, mines, gates.</summary>
    public sealed class LevelFacts
    {
        public string Key = string.Empty;
        public int StartingMoney;
        public float HealthScale = 1f;
        public LevelShape Shape = new LevelShape();
        public int MineCount;
        public int MineStock;

        /// <summary>False when the level offers no harvest drone (yet): its ore pays nothing.</summary>
        public bool Harvest = true;
        public int SecondRoad = -1;
        public float GateHealth;
        public float GateRepairCostPerHealth;
        public int GateRebuildCost;

        /// <summary>The gate as a tower (round 26): its in-level upgrade levels, each a gun at the queue and extra health; null without gates.</summary>
        public TowerStats Gate;

        /// <summary>The base's health and the base shares that earn the stars (LevelDefinition), for the leak model.</summary>
        public float BaseHealth;
        public float[] StarFractions = { 0.5f, 0.9f };

        /// <summary>The base's own turret and how many sides of the base it can stand on (the permanent base weapon upgrade); null without one.</summary>
        public TowerStats BaseTurret;
        public int BaseTurretSides;

        /// <summary>The gadgets the level sells and the drones it offers, and its map for placement (null = ideal placement).</summary>
        public List<GadgetStats> Gadgets = new List<GadgetStats>();
        public List<DroneStats> Drones = new List<DroneStats>();
        public LevelGeometry Geometry;
    }

    /// <summary>
    /// The whole campaign as the balance model sees it: levels in play order, every tower, creature and upgrade, the hero
    /// and the starting set. Builds the ledger input for any level and difficulty, so the generator, the tests and the
    /// BalanceLab tool all judge a level against the same player.
    /// </summary>
    public sealed class CampaignModel
    {
        private const float CalibrationMinScale = 0.25f;
        private const float CalibrationMaxScale = 4f;
        private const int CalibrationIterations = 22;
        private const int CalibrationPasses = 3;

        /// <summary>When the bisection misses the target by more than this share, the whole scale range is scanned in this many steps.</summary>
        private const float ScanTolerance = 0.005f;
        private const int ScanSteps = 160;
        private const float CapStep = 0.92f;
        private const int CapRefinements = 8;
        private const int CapSweeps = 4;

        /// <summary>Times the bosses are sized; between two sizings the waves are trimmed again around them.</summary>
        private const int BossPasses = 2;
        private const int ReliefIterations = 8;

        public readonly List<CampaignLevel> Levels = new List<CampaignLevel>();
        public readonly List<LevelFacts> Facts = new List<LevelFacts>();
        public readonly Dictionary<string, TowerStats> Towers = new Dictionary<string, TowerStats>();
        public readonly Dictionary<string, EnemyStats> Enemies = new Dictionary<string, EnemyStats>();
        public readonly List<UpgradeStats> Upgrades = new List<UpgradeStats>();
        public readonly List<DifficultyStats> Difficulties = new List<DifficultyStats>();
        public readonly List<string> StartingTowers = new List<string>();
        public HeroStats Hero = new HeroStats();
        public int HarvesterCost;
        public BalanceRules Rules = BalanceRules.Default;

        public int IndexOf(string key) => Levels.FindIndex(l => l.Key == key);

        public PlayerState PlayerAt(int levelIndex, int difficulty) =>
            Progression.StateAt(Levels, levelIndex, difficulty, StartingTowers, Upgrades, Rules);

        public LevelInput InputFor(int levelIndex, int difficulty) => InputFor(levelIndex, difficulty, PlayerAt(levelIndex, difficulty));

        /// <summary>
        /// As above for a player without permanent upgrades or perks (a fresh save), owning the towers the campaign has rewarded so far:
        /// what the probe bots play with, so the calibration judges the recorded runs against the player that really played them.
        /// </summary>
        public LevelInput InputForFreshPlayer(int levelIndex, int difficulty) => InputFor(levelIndex, difficulty, PlayerAt(levelIndex, difficulty).Plain());

        public LevelInput InputFor(int levelIndex, int difficulty, PlayerState player)
        {
            LevelFacts facts = Facts[levelIndex];
            DifficultyStats stats = difficulty >= 0 && difficulty < Difficulties.Count ? Difficulties[difficulty] : new DifficultyStats();
            var input = new LevelInput
            {
                Key = facts.Key,
                // Rounded as the game rounds it (Mathf.RoundToInt: to even), so the model's purse is the wallet's.
                StartingMoney = (int)Math.Round(facts.StartingMoney * stats.StartMoney),
                Income = stats.Income,
                HealthScale = facts.HealthScale,
                Shape = facts.Shape,
                MineCount = facts.MineCount,
                MineStock = facts.MineStock,
                Harvest = facts.Harvest,
                HarvesterCost = HarvesterCost,
                Hero = Hero,
                HeroDamage = player.HeroDamage,
                HeroPerks = player.HeroPerks,
                GateHealth = facts.GateHealth * player.GateHealth,
                GateRepairCostPerHealth = facts.GateRepairCostPerHealth,
                GateRebuildCost = facts.GateRebuildCost,
                Gate = facts.Gate,
                GateCell = facts.Geometry?.DefenceGate,
                BaseHealth = facts.BaseHealth + player.BaseHealthBonus,
                StarFractions = facts.StarFractions,
                BaseTurret = facts.BaseTurret,
                BaseTurrets = facts.BaseTurret != null ? Math.Min(facts.BaseTurretSides, player.BaseWeaponLevel) : 0,
                BaseTurretLevel = facts.BaseTurret != null ? Math.Max(0, player.BaseWeaponLevel - facts.BaseTurretSides) : 0,
                Gadgets = facts.Gadgets,
                Drones = facts.Drones,
                Geometry = facts.Geometry,
            };

            foreach (string id in Levels[levelIndex].TowerIds)
            {
                if (player.OwnedTowers.Contains(id) && Towers.TryGetValue(id, out TowerStats tower) && tower.IsGun)
                {
                    input.Towers.Add(tower);
                    input.Boosts[id] = player.Boosts.TryGetValue(id, out TowerBoost boost) ? boost : TowerBoost.None;
                }
            }

            return input;
        }

        /// <summary>
        /// As <see cref="InputFor"/>, but with the level's own starting money and full income: the economy the wave solver
        /// sizes creatures against. A difficulty's money shares (<see cref="DifficultyStats.Income"/>, <see cref="DifficultyStats.StartMoney"/>)
        /// are a lever of their own; solving against them would only make the creatures weaker to make up for the thinner purse.
        /// </summary>
        public LevelInput ReferenceInputFor(int levelIndex, int difficulty)
        {
            LevelInput input = InputFor(levelIndex, difficulty);
            input.StartingMoney = Facts[levelIndex].StartingMoney;
            input.Income = 1f;
            return input;
        }

        /// <summary>Solves a level's budgets on Medium and composes its waves for every difficulty.</summary>
        public List<List<WaveSpec>> BuildWaves(int levelIndex, WaveScriptSpec script, out float[] healths)
        {
            LevelFacts facts = Facts[levelIndex];
            // The script (with the builder's tuning) is the source of the target scale at generation; the asset records it for the checks.
            Levels[levelIndex].TargetScale = script.TargetScale;
            // The flyer floors are this solve's to set (the Easy raid rule and the first-flyer rule below), never the script's.
            script.FirstAirWaveFloor = new[] { 0 };
            script.FlyingTroopFloor = new[] { 0 };
            int medium = Math.Min(WaveSolver.SolveDifficulty, Difficulties.Count - 1);
            healths = WaveSolver.Solve(script, Enemies, ReferenceInputFor(levelIndex, medium), Difficulties[medium], facts.SecondRoad, Rules);
            _calmCurve = Rules.SurprisesLiftNoWave ? WithoutSurprises(() => WaveSolver.Solve(script, Enemies, ReferenceInputFor(levelIndex, medium), Difficulties[medium], facts.SecondRoad, Rules)) : null;
            var sets = new List<List<WaveSpec>>();
            int mediumFloor = 0;
            int mediumTroopFloor = 0;
            for (int d = 0; d < Difficulties.Count; d++)
            {
                float[] own = SolveDifficulty(levelIndex, script, d, medium, healths, out float[] bossScales);
                if (d == EasyDifficulty)
                {
                    // The Easy raid rule (the curator, round 22f): the first raid waits until the expected player's kills have paid for
                    // a sky gun, plus a wave. Since round 80 the flying troops wait for the same wave (a rift's flyers no longer wait
                    // for the raid itself): one bat out of a rift before that purse made the ledger's Easy player buy the sky gun
                    // out of its ground defence, and the human calibration took 38 % off Fault Line Easy. The floor is read on the
                    // waves without any flying troop, so the troops it holds back do not move it.
                    script.FlyingTroopFloor = WithFloor(script.FlyingTroopFloor, d, script.Waves + 1);
                    own = SolveDifficulty(levelIndex, script, d, medium, healths, out bossScales);
                    int first = WaveComposer.FirstAirWave(script, d);
                    int floor = EasyRaidFloor(levelIndex, ComposedWaves(script, d, own, facts.SecondRoad, bossScales));
                    script.FlyingTroopFloor = WithFloor(script.FlyingTroopFloor, d, floor);
                    if (first != int.MaxValue && floor > first)
                    {
                        script.FirstAirWaveFloor = WithFloor(script.FirstAirWaveFloor, d, floor);
                    }

                    own = SolveDifficulty(levelIndex, script, d, medium, healths, out bossScales);
                }

                if (Rules.FlyersAfterBountiesFor(d))
                {
                    own = HoldFlyersUntilPayable(levelIndex, script, d, medium, healths, own, ref bossScales);
                }

                sets.Add(ComposedWaves(script, d, own, facts.SecondRoad, bossScales));
                if (d == medium)
                {
                    // The difficulties after Medium copy the Medium curve wave by wave as the solver wrote it, raids as written
                    // (WaveSolver.SolveVariant compares their waves with Medium's); Medium's own floor would make them read
                    // moved raids against an unmoved curve, so it is set aside until every difficulty is composed.
                    mediumFloor = script.FirstAirWaveFloorFor(d);
                    mediumTroopFloor = script.FlyingTroopFloorFor(d);
                    SetFlyerFloors(script, d, 0, 0);
                }
            }

            SetFlyerFloors(script, medium, mediumFloor, mediumTroopFloor);
            _calmCurve = null;
            return sets;
        }

        // The Medium curve of the level being built, solved without its surprises (BalanceRules.SurprisesLiftNoWave).
        private float[] _calmCurve;

        // Runs a solve with the grammar's surprises switched off. The rules object is this model's own, so nothing else sees it.
        private T WithoutSurprises<T>(Func<T> solve)
        {
            Rules.Grammar.Surprises = false;
            try
            {
                return solve();
            }
            finally
            {
                Rules.Grammar.Surprises = true;
            }
        }

        /// <summary>
        /// The first-flyer rule (<see cref="BalanceRules.FlyersAfterBounties"/>, round 77): while the first flyers of a difficulty
        /// come before the bounties pay for a sky gun (<see cref="CounterPurse"/>: every wave before the first flyer wave plus
        /// the grace waves, through the income share), the difficulty's flyer floors go up a wave (the raids move together, the
        /// flying troops wait) and the difficulty is solved again. A player who owns no gun made for the sky has nothing to pay
        /// for; the level validator's affordability table names that cell instead.
        /// </summary>
        private float[] HoldFlyersUntilPayable(int levelIndex, WaveScriptSpec script, int d, int medium, float[] healths, float[] own, ref float[] bossScales)
        {
            int price = CheapestSkyGunCost(InputFor(levelIndex, d), true);
            int secondRoad = Facts[levelIndex].SecondRoad;
            while (price >= 0)
            {
                List<WaveSpec> waves = ComposedWaves(script, d, own, secondRoad, bossScales);
                int first = FirstFlyerWave(waves);
                if (first == 0 || CounterPurse.PayableFromBounties(KillCoins(waves, d, CounterPurse.DeadlineWave(first, d, waves.Count) - 1), price))
                {
                    break;
                }

                int floor = first + 1;
                if (floor > script.Waves)
                {
                    // Nothing left to move: only the flyers a last boss brings along are this late.
                    break;
                }

                // An earlier rule's raid floor (Easy) is never lowered.
                SetFlyerFloors(script, d, Math.Max(floor, script.FirstAirWaveFloorFor(d)), floor);
                own = SolveDifficulty(levelIndex, script, d, medium, healths, out bossScales);
            }

            return own;
        }

        // The first wave (1-based) that sends a flyer, bosses aside (a boss carries its own fight); 0 for none. As
        // Game.Levels.CounterAffordability.FirstWave reads the stored waves.
        private static int FirstFlyerWave(IReadOnlyList<WaveSpec> waves)
        {
            for (int w = 0; w < waves.Count; w++)
            {
                foreach (GroupSpec group in waves[w].Groups)
                {
                    if (group.Enemy != null && !group.Enemy.IsBoss && group.Enemy.Flies)
                    {
                        return w + 1;
                    }
                }
            }

            return 0;
        }

        // The bounties of the first waveCount waves through the difficulty's income share, every creature killed, as
        // Game.Levels.CounterAffordability.KillCoins counts them on the stored waves.
        private int KillCoins(IReadOnlyList<WaveSpec> waves, int d, int waveCount)
        {
            long bounty = 0;
            for (int w = 0; w < waveCount && w < waves.Count; w++)
            {
                foreach (GroupSpec group in waves[w].Groups)
                {
                    if (group.Enemy != null)
                    {
                        bounty += (long)(group.Enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * Difficulties[d].Count))) * group.Enemy.Reward;
                    }
                }
            }

            return CounterPurse.KillCoins(bounty, Difficulties[d].Income);
        }

        // One difficulty's flyer floors on the script (raids, flying troops); the other difficulties keep theirs.
        private void SetFlyerFloors(WaveScriptSpec script, int d, int raidFloor, int troopFloor)
        {
            script.FirstAirWaveFloor = WithFloor(script.FirstAirWaveFloor, d, raidFloor);
            script.FlyingTroopFloor = WithFloor(script.FlyingTroopFloor, d, troopFloor);
        }

        private int[] WithFloor(int[] current, int d, int floor)
        {
            var floors = new int[Math.Max(1, Difficulties.Count)];
            for (int i = 0; i < floors.Length; i++)
            {
                floors[i] = current != null && i < current.Length ? current[i] : 0;
            }

            floors[d] = floor;
            return floors;
        }

        /// <summary>The first difficulty, where the raid rule applies.</summary>
        public const int EasyDifficulty = 0;

        /// <summary>
        /// The Easy raid rule's floor for a level: the first wave (1-based) by which the expected Easy player's kill money (the purse
        /// above the start money, the ledger's walk over <paramref name="easyWaves"/>) covers the cheapest owned sky gun, plus
        /// <see cref="BalanceRules.EasyRaidMarginWaves"/>. A gun made for the air counts first, else any gun that reaches it; with
        /// neither, or when the kills never cover it, the floor lies past the last wave and Easy has no raid.
        /// </summary>
        public int EasyRaidFloor(int levelIndex, IReadOnlyList<WaveSpec> easyWaves)
        {
            LevelInput input = InputFor(levelIndex, EasyDifficulty);
            int cost = CheapestSkyGunCost(input, false);
            int none = easyWaves.Count + 1;
            if (cost < 0)
            {
                return none;
            }

            List<WaveLine> lines = Ledger.Evaluate(input, easyWaves, Difficulties[EasyDifficulty], Rules.HumanTowersOnly());
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Money - lines[0].Money >= cost)
                {
                    return Math.Min(none, i + 1 + Rules.EasyRaidMarginWaves);
                }
            }

            return none;
        }

        // The cheapest sky gun the player brings: an air-only gun when owned, else the cheapest gun that reaches the air (not
        // when only a gun made for the sky counts: the counter a player buys against flyers); -1 for none.
        private static int CheapestSkyGunCost(LevelInput input, bool madeForTheSky)
        {
            int airOnly = -1;
            int reaches = -1;
            foreach (TowerStats tower in input.Towers)
            {
                if (!tower.IsGun || !Layer.Has(tower.Targets, Layer.Air) || tower.Levels.Length == 0)
                {
                    continue;
                }

                int cost = tower.Levels[0].Cost;
                if (tower.Targets == Layer.Air)
                {
                    airOnly = airOnly < 0 ? cost : Math.Min(airOnly, cost);
                }

                reaches = reaches < 0 ? cost : Math.Min(reaches, cost);
            }

            return airOnly >= 0 || madeForTheSky ? airOnly : reaches;
        }

        // The solver's multipliers of one difficulty, trimmed to the human curve, and the bosses' own scales (round 64; null when
        // the model does not calibrate). The bosses are sized on the trimmed waves, the waves are trimmed once more around the
        // bosses as sized (a tougher boss costs kill money and so shifts the waves after it), and the bosses are sized again.
        private float[] SolveDifficulty(int levelIndex, WaveScriptSpec script, int d, int medium, float[] healths, out float[] bossScales)
        {
            float[] own = SolveAsComposed(levelIndex, script, d, medium, healths, out bossScales);
            int secondRoad = Facts[levelIndex].SecondRoad;
            if (_calmCurve == null || !Rules.CalibrateToHuman || WaveGrammar.SurpriseWaves(script, Enemies, d, secondRoad, Rules).Count == 0)
            {
                return own;
            }

            // BalanceRules.SurprisesLiftNoWave: the same difficulty once more without its surprises. A wave that carries none
            // takes the lower of its two multipliers; a boss keeps the health it was sized at.
            float[] calmCurve = _calmCurve;
            // The guard gun of a rear rift stays bought in the level without surprises (BalanceRules.RearRiftGuardCells).
            List<WaveSpec> composed = ComposedWaves(script, d, own, secondRoad, bossScales);
            LevelInput shape = InputFor(levelIndex, d);
            for (int i = 0; i < composed.Count && Rules.RearGuardFromWave == 0; i++)
            {
                Rules.RearGuardFromWave = KillPurse.ComesFarBehind(shape, composed[i], Rules) ? i + 1 : 0;
            }

            float[] calm;
            try
            {
                calm = WithoutSurprises(() => SolveAsComposed(levelIndex, script, d, medium, calmCurve, out _));
            }
            finally
            {
                Rules.RearGuardFromWave = 0;
            }

            float[] full = (float[])own.Clone();
            float[] fullBosses = bossScales != null ? (float[])bossScales.Clone() : null;
            var lowered = new float[own.Length];
            for (int i = 0; i < own.Length; i++)
            {
                bool takes = calm[i] < own[i] && WaveGrammar.SurpriseOn(script, Enemies, d, i + 1, secondRoad, Rules) == SurpriseKind.None;
                lowered[i] = takes ? calm[i] : own[i];
            }

            // The curve comes first (BalanceRules.SurpriseReliefMost): the lowered waves come back up together, as little as
            // keeps the level's plain mean inside its band. Zero is the calm level's waves, one the level's own.
            float[] bosses = bossScales;
            float leastMean = HumanTarget(levelIndex, d) / (1f + Rules.SurpriseReliefMost);
            LevelInput input = InputFor(levelIndex, d);
            BalanceRules human = Rules.HumanTowersOnly();
            float Mean(float share)
            {
                for (int i = 0; i < own.Length; i++)
                {
                    own[i] = (float)Math.Round(lowered[i] + (full[i] - lowered[i]) * share, 3);
                    if (bosses != null && WaveComposer.BossOn(script, d, i + 1) != null)
                    {
                        bosses[i] = fullBosses[i] * full[i] / own[i];
                    }
                }

                return BalanceChecks.PlainMean(Ledger.Evaluate(input, ComposedWaves(script, d, own, secondRoad, bosses), Difficulties[d], human));
            }

            if (Mean(0f) >= leastMean)
            {
                return own;
            }

            float below = 0f;
            float above = 1f;
            for (int i = 0; i < ReliefIterations; i++)
            {
                float mid = (below + above) * 0.5f;
                if (Mean(mid) >= leastMean)
                {
                    above = mid;
                }
                else
                {
                    below = mid;
                }
            }

            Mean(above);
            return own;
        }

        private float[] SolveAsComposed(int levelIndex, WaveScriptSpec script, int d, int medium, float[] healths, out float[] bossScales)
        {
            int secondRoad = Facts[levelIndex].SecondRoad;
            // Medium is the solver's own curve. With its raids moved by a flyer floor that curve no longer fits its waves (a raid's
            // multiplier on a plain wave), so Medium is solved again on the moved script; the other difficulties keep the first curve.
            float[] solved = d != medium ? WaveSolver.SolveVariant(script, Enemies, ReferenceInputFor(levelIndex, d), Difficulties[d], healths, secondRoad, Rules)
                : script.FirstAirWaveFloorFor(d) > 0 || script.FlyingTroopFloorFor(d) > 0 ? WaveSolver.Solve(script, Enemies, ReferenceInputFor(levelIndex, d), Difficulties[d], secondRoad, Rules)
                : healths;
            bossScales = null;
            if (!Rules.CalibrateToHuman)
            {
                return solved;
            }

            float[] own = CalibrateToHuman(levelIndex, script, d, solved, secondRoad, null);
            for (int pass = 0; pass < BossPasses; pass++)
            {
                bossScales = SizeBosses(levelIndex, script, d, own, secondRoad);
                if (pass == BossPasses - 1 || !Resized(bossScales))
                {
                    break;
                }

                var bossHealths = new float[own.Length];
                for (int i = 0; i < own.Length; i++)
                {
                    bossHealths[i] = own[i] * bossScales[i];
                }

                own = CalibrateToHuman(levelIndex, script, d, solved, secondRoad, bossHealths);
            }

            return own;
        }

        private static bool Resized(float[] bossScales)
        {
            foreach (float scale in bossScales)
            {
                if (scale != 1f)
                {
                    return true;
                }
            }

            return false;
        }

        private List<WaveSpec> ComposedWaves(WaveScriptSpec script, int d, float[] healths, int secondRoad, float[] bossScales = null)
        {
            var waves = new List<WaveSpec>(script.Waves);
            for (int i = 0; i < script.Waves; i++)
            {
                WaveSpec wave = Composed(script, d, i, healths[i], secondRoad);
                wave.BossHealthScale = bossScales != null ? bossScales[i] : 1f;
                waves.Add(wave);
            }

            return waves;
        }

        /// <summary>
        /// The health of the bosses of every wave on top of the wave's multiplier, one per wave, as the assets store it (one
        /// for a wave without a level boss). A level's boss is never scaled under <see cref="BalanceRules.LevelBossHealthFloor"/>;
        /// a walking boss that then asks less on its own than <see cref="BalanceRules.LevelBossAlone"/> grows until it does; and
        /// where its wave as a whole then asks more than the hero cap, the crowd of that wave is trimmed
        /// (<paramref name="healths"/> changes for it) while the boss keeps its health.
        /// </summary>
        private float[] SizeBosses(int levelIndex, WaveScriptSpec script, int difficulty, float[] healths, int secondRoad)
        {
            var scales = new float[healths.Length];
            for (int i = 0; i < scales.Length; i++)
            {
                scales[i] = 1f;
            }

            DifficultyStats stats = Difficulties[difficulty];
            BalanceRules withHero = Rules.HumanWithHero();
            LevelInput input = InputFor(levelIndex, difficulty);
            float floor = Rules.LevelBossHealthFloorFor(difficulty);
            float alone = Rules.LevelBossAloneFor(difficulty);
            float cap = Rules.HeroCapFor(difficulty);

            // What one wave asks, as stored or reduced to its bosses.
            float Pressure(int index, bool bossOnly)
            {
                List<WaveSpec> waves = ComposedWaves(script, difficulty, healths, secondRoad, scales);
                if (bossOnly)
                {
                    WaveSpec whole = waves[index];
                    var only = new WaveSpec { StartDelay = whole.StartDelay, HealthMultiplier = whole.HealthMultiplier, BossHealthScale = whole.BossHealthScale };
                    only.Groups.AddRange(whole.Groups.FindAll(g => g.Enemy.IsBoss));
                    waves[index] = only;
                }

                return Ledger.Evaluate(input, waves, stats, withHero)[index].Pressure;
            }

            for (int i = 0; i < healths.Length; i++)
            {
                BossSpec boss = WaveComposer.BossOn(script, difficulty, i + 1);
                if (boss == null || !Enemies.TryGetValue(boss.Enemy, out EnemyStats enemy) || !enemy.IsBoss || enemy.IsContinentBoss)
                {
                    continue;
                }

                int wave = i;
                scales[wave] = (float)Math.Round(Math.Max(1f, floor / Math.Max(0.001f, healths[wave])), 3);
                if (!enemy.Flies && alone > 0f && Pressure(wave, true) < alone)
                {
                    float from = scales[wave];
                    double growth = Bisect(logGrowth =>
                    {
                        scales[wave] = from * (float)Math.Exp(logGrowth);
                        return Pressure(wave, true);
                    }, alone, 0d, Math.Log(Rules.MaxBossGrowth));
                    scales[wave] = (float)Math.Round(from * growth, 3);
                    // A boss that reaches the base ends the level in seconds, so it grows only as far as its wave still fits
                    // BalanceRules.LevelBossGrowthCap; grown to the hero cap it lost every searched plan Shard Ridge Psycho's last
                    // wave (round 64).
                    float held = Math.Min(cap, Rules.LevelBossGrowthCapFor(difficulty));
                    if (scales[wave] > from && Pressure(wave, false) > held)
                    {
                        float grown = scales[wave];
                        scales[wave] = (float)Math.Round(BossScaleUnder(scale =>
                        {
                            scales[wave] = scale;
                            return Pressure(wave, false);
                        }, held, from, grown), 3);
                    }
                }

                // Round 65: with a thin crowd the wave's own multiplier can carry a boss past what one creature should ask. A
                // walking boss asks at most LevelBossAloneMost on its own; above it the boss comes down, never under its floor.
                float most = Rules.LevelBossAloneMostFor(difficulty);
                float lowest = floor / Math.Max(0.001f, healths[wave]);
                if (!enemy.Flies && most > 0f && scales[wave] > lowest && Pressure(wave, true) > most)
                {
                    float tall = scales[wave];
                    scales[wave] = (float)Math.Round(BossScaleUnder(scale =>
                    {
                        scales[wave] = scale;
                        return Pressure(wave, true);
                    }, most, lowest, tall), 3);
                }

                if (scales[wave] <= 1f || Pressure(wave, false) <= cap)
                {
                    continue;
                }

                // The boss is tougher than the wave had room for: its crowd gives way, the boss keeps its health.
                float crowd = healths[wave];
                float bossHealth = crowd * scales[wave];
                float kept = CapScale(scale =>
                {
                    healths[wave] = crowd * scale;
                    scales[wave] = bossHealth / healths[wave];
                    return Pressure(wave, false);
                }, cap);
                healths[wave] = (float)Math.Round(crowd * kept, 3);
                scales[wave] = (float)Math.Round(bossHealth / healths[wave], 3);
                float least = Math.Max(1f, floor / Math.Max(0.001f, healths[wave]));
                if (Pressure(wave, false) <= cap || scales[wave] <= least)
                {
                    continue;
                }

                // The crowd cannot give way any further (what an earlier wave left on the road is not this wave's to trim): the
                // boss comes down again, never under its floor, until the wave fits. A boss stays killable.
                float tough = scales[wave];
                float fits = BossScaleUnder(scale =>
                {
                    scales[wave] = scale;
                    return Pressure(wave, false);
                }, cap, least, tough);
                scales[wave] = (float)Math.Round(fits, 3);
            }

            return scales;
        }

        // A wave as the generator stores it: composed, on the clock the assets carry (the first wave's longer countdown included), so
        // the calibration judges exactly the waves the ledger will read back (round 22b: a 2 s purse shift put C1_04 Hard w3 over its cap).
        private WaveSpec Composed(WaveScriptSpec script, int difficulty, int index, float health, int secondRoad)
        {
            WaveSpec wave = WaveComposer.Compose(script, Enemies, difficulty, index + 1, health, secondRoad, Rules);
            wave.StartDelay = index == 0 ? Rules.FirstWaveDelay : Rules.WaveDelay;
            return wave;
        }

        /// <summary>Index of a level within its continent (0 for the continent's first level).</summary>
        public int ContinentIndexOf(int levelIndex)
        {
            int continent = Levels[levelIndex].Continent;
            int index = 0;
            for (int i = 0; i < levelIndex; i++)
            {
                index += Levels[i].Continent == continent ? 1 : 0;
            }

            return index;
        }

        /// <summary>The plain-wave pressure the human lens should meet on a level and difficulty (one over the target surplus).</summary>
        public float HumanTarget(int levelIndex, int difficulty) => Levels[levelIndex].TargetScaleFor(difficulty) / Rules.HumanSurplusAt(difficulty, ContinentIndexOf(levelIndex));

        /// <summary>
        /// Trims one difficulty's health multipliers by a single factor, so a competent player's towers alone (the human lens,
        /// with the real money of the difficulty) meet <see cref="HumanTarget"/> on the plain waves. The solver gives the shape
        /// (ramp, rhythm, bosses, breathers) against the bot's calibration; this sets the level of the whole curve, so every
        /// level of a continent is a little harder than the one before, net of the upgrades the player is expected to own.
        /// </summary>
        /// <remarks>
        /// Then no wave may ask more of that player with the hero (<see cref="BalanceRules.HumanWithHero"/>) than the
        /// difficulty's <see cref="BalanceRules.HeroCap"/>: a boss wave or a spike above it is softened on its own (bosses keep
        /// their high health but stay killable), and the other waves are trimmed again so the plain mean stays on the curve.
        /// </remarks>
        /// <param name="bossHealths">Per wave the health multiplier its bosses carry whatever the trim makes of the wave (round 64), or null: the bosses follow their wave.</param>
        private float[] CalibrateToHuman(int levelIndex, WaveScriptSpec script, int difficulty, float[] healths, int secondRoad, float[] bossHealths)
        {
            BalanceRules human = Rules.HumanTowersOnly();
            BalanceRules withHero = Rules.HumanWithHero();
            LevelInput input = InputFor(levelIndex, difficulty);
            DifficultyStats stats = Difficulties[difficulty];
            float target = HumanTarget(levelIndex, difficulty);
            float cap = Rules.HeroCapFor(difficulty);
            var trimmed = (float[])healths.Clone();
            var capped = new bool[healths.Length];
            float raidFloor = Rules.RaidHealthFloorFor(difficulty);

            // BalanceRules.RaidsCappedAtHeldGuns: the trim may soften a raid with the level, never lift it above the solver's
            // crossing (the sky guns held).
            float Trimmed(int wave, float health) =>
                Rules.RaidsCappedAtHeldGuns && WaveComposer.KindOf(script, difficulty, wave + 1) == WaveComposer.Kind.Air ? Math.Min(health, healths[wave]) : health;

            List<WaveLine> Evaluate(float[] health, BalanceRules rules)
            {
                var waves = new List<WaveSpec>(script.Waves);
                for (int i = 0; i < script.Waves; i++)
                {
                    WaveSpec wave = Composed(script, difficulty, i, health[i], secondRoad);
                    wave.BossHealthScale = bossHealths != null && health[i] > 0f ? bossHealths[i] / health[i] : 1f;
                    waves.Add(wave);
                }

                return Ledger.Evaluate(input, waves, stats, rules);
            }

            // Round 64: a wave can stay over the cap through what the wave before it left on the road (a slow, armoured creature
            // arrives with the next wave, KillPurse.Carried), and no trim of its own creatures brings it down (Fault Line Hard
            // wave 10: a Cragback of wave 9). Then the wave before it gives way, unless it is a raid held on its floor.
            void GiveWayBefore(int wave)
            {
                if (wave <= 0 || Evaluate(trimmed, withHero)[wave].Pressure <= cap || (raidFloor > 0f && WaveComposer.KindOf(script, difficulty, wave) == WaveComposer.Kind.Air))
                {
                    return;
                }

                float before = trimmed[wave - 1];
                trimmed[wave - 1] = before * CapScale(scale =>
                {
                    trimmed[wave - 1] = before * scale;
                    return Evaluate(trimmed, withHero)[wave].Pressure;
                }, cap);
                capped[wave - 1] = true;
            }

            for (int pass = 0; pass < CalibrationPasses; pass++)
            {
                // One factor on every wave not capped yet, so the plain mean meets the target.
                float[] start = (float[])trimmed.Clone();
                double factor = Bisect(logScale =>
                {
                    float scale = (float)Math.Exp(logScale);
                    var trial = new float[start.Length];
                    for (int i = 0; i < start.Length; i++)
                    {
                        trial[i] = capped[i] ? start[i] : Trimmed(i, start[i] * scale);
                    }

                    return BalanceChecks.PlainMean(Evaluate(trial, human));
                }, target, Math.Log(CalibrationMinScale), Math.Log(CalibrationMaxScale));
                for (int i = 0; i < trimmed.Length; i++)
                {
                    trimmed[i] = capped[i] ? start[i] : Trimmed(i, start[i] * (float)factor);
                    // BalanceRules.RaidHealthFloor: the flyers of a raid keep their book toughness whatever the trim makes of the
                    // wave. A raid on its floor is held there, so the next pass trims the other waves around it and the plain mean
                    // still meets the curve; the cap below may soften a raid no player with the hero could hold.
                    if (raidFloor > trimmed[i] && WaveComposer.KindOf(script, difficulty, i + 1) == WaveComposer.Kind.Air)
                    {
                        trimmed[i] = raidFloor;
                        capped[i] = true;
                    }
                }

                // Waves over the cap, in order (the purse of a wave depends on the leaks before it).
                for (int i = 0; i < trimmed.Length; i++)
                {
                    if (Evaluate(trimmed, withHero)[i].Pressure <= cap)
                    {
                        continue;
                    }

                    int wave = i;
                    float full = trimmed[i];
                    trimmed[i] = full * CapScale(scale =>
                    {
                        trimmed[wave] = full * scale;
                        return Evaluate(trimmed, withHero)[wave].Pressure;
                    }, cap);
                    capped[i] = true;
                    GiveWayBefore(wave);
                }
            }

            // The last pass's rescale can lift a capped wave back over the cap through the creatures of the waves before it that
            // arrive with it (KillPurse.Carried): sweep the cap alone until every wave fits (round 22b; C1_04 Hard waves 3 and 6).
            for (int sweep = 0; sweep < CapSweeps; sweep++)
            {
                bool over = false;
                List<WaveLine> lines = Evaluate(trimmed, withHero);
                for (int i = 0; i < trimmed.Length; i++)
                {
                    if (lines[i].Pressure <= cap)
                    {
                        continue;
                    }

                    over = true;
                    int wave = i;
                    float full = trimmed[i];
                    trimmed[i] = full * CapScale(scale =>
                    {
                        trimmed[wave] = full * scale;
                        return Evaluate(trimmed, withHero)[wave].Pressure;
                    }, cap);
                    capped[i] = true;
                    GiveWayBefore(wave);
                    lines = Evaluate(trimmed, withHero);
                }

                if (!over)
                {
                    break;
                }
            }

            for (int i = 0; i < trimmed.Length; i++)
            {
                // Rounded so the editor and the BalanceLab tool land on the same numbers.
                trimmed[i] = (float)Math.Round(trimmed[i], 3);
            }

            return trimmed;
        }

        // The largest boss scale between its floor and what it grew to that keeps its wave under the cap; the floor when none does.
        private static float BossScaleUnder(Func<float, float> pressure, float cap, float least, float grown)
        {
            if (pressure(least) > cap)
            {
                return least;
            }

            float below = least;
            float above = grown;
            for (int i = 0; i < CalibrationIterations; i++)
            {
                float mid = (below + above) * 0.5f;
                if (pressure(mid) <= cap)
                {
                    below = mid;
                }
                else
                {
                    above = mid;
                }
            }

            return below;
        }

        // The largest scale of one wave's health (at most one) that brings its pressure under the cap. A wave's own health is
        // not the only thing in its pressure (the ledger re-chooses the defence, and slow creatures of earlier waves arrive with
        // it), so the pressure is not monotone near the bottom: the scale steps down until the wave fits, then a bisection
        // between the last two steps finds the edge; when no step fits, the step with the lowest pressure is kept.
        private static float CapScale(Func<float, float> pressure, float cap)
        {
            float above = 1f;
            float best = 1f;
            float lowest = float.MaxValue;
            for (float scale = CapStep; scale >= CalibrationMinScale; scale *= CapStep)
            {
                float value = pressure(scale);
                if (value <= cap)
                {
                    float below = scale;
                    for (int i = 0; i < CapRefinements; i++)
                    {
                        float mid = (above + below) * 0.5f;
                        if (pressure(mid) <= cap)
                        {
                            below = mid;
                        }
                        else
                        {
                            above = mid;
                        }
                    }

                    return below;
                }

                if (value < lowest)
                {
                    lowest = value;
                    best = scale;
                }

                above = scale;
            }

            return best;
        }

        // Pressure grows with health, so a bisection on the logarithm of a scale finds where it meets the target. Where the purse
        // breaks (leaks cost kill money) the pressure jumps; the bisection then ends on the jump, and the side closer to the
        // target is kept.
        private static double Bisect(Func<double, float> pressure, float target, double lo, double hi)
        {
            if (pressure(hi) <= target)
            {
                return Math.Exp(hi);
            }

            double floor = lo;
            double ceiling = hi;
            for (int i = 0; i < CalibrationIterations; i++)
            {
                double mid = (lo + hi) * 0.5;
                if (pressure(mid) < target)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            double chosen = Math.Abs(pressure(lo) - target) <= Math.Abs(pressure(hi) - target) ? lo : hi;
            float miss = Math.Abs(pressure(chosen) - target);
            if (miss > target * ScanTolerance)
            {
                // The mean is not monotone in the scale (the ledger re-chooses its guns per wave and the purse rounds to coins), so
                // the bisection can land on a jump (round 22c: Quartz Flats Easy came out 5% harder than its target). A scan of the
                // whole range finds the scale whose mean comes closest.
                for (int i = 0; i <= ScanSteps; i++)
                {
                    double scale = floor + (ceiling - floor) * i / ScanSteps;
                    float value = Math.Abs(pressure(scale) - target);
                    if (value < miss)
                    {
                        miss = value;
                        chosen = scale;
                    }
                }
            }

            return Math.Exp(chosen);
        }

        /// <summary>
        /// Research and power the existing levels of one continent pay per difficulty, against what the upgrades tagged to
        /// that continent cost. Dormant upgrades (no continent) count nowhere.
        /// </summary>
        public void MetaTotals(int continent, out int researchCost, out int powerCost, out int[] researchPerDifficulty, out int[] powerPerDifficulty)
        {
            // Round 66: what a continent's upgrades cost is what the tiers it opens cost (UpgradeEconomy.Opened).
            researchCost = UpgradeEconomy.Opened(Upgrades, continent, power: false, Rules);
            powerCost = UpgradeEconomy.Opened(Upgrades, continent, power: true, Rules);

            researchPerDifficulty = new int[Difficulties.Count];
            powerPerDifficulty = new int[Difficulties.Count];
            for (int d = 0; d < Difficulties.Count; d++)
            {
                foreach (CampaignLevel level in Levels)
                {
                    if (level.Continent != continent)
                    {
                        continue;
                    }

                    researchPerDifficulty[d] += (int)Math.Round(level.Research * Difficulties[d].Research, MidpointRounding.AwayFromZero);
                    powerPerDifficulty[d] += (int)Math.Round(level.Power * Difficulties[d].Power, MidpointRounding.AwayFromZero);
                }
            }
        }
    }
}
