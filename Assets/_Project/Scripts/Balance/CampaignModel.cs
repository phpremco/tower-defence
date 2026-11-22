using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>A level's own numbers that do not change with the campaign position: map, money, mines, gates.</summary>
    public sealed class LevelFacts
    {
        public string Key = string.Empty;
        public int StartingMoney;

        /// <summary>Start money per difficulty beyond the multiplied sum (Levels.StartMoneyRule.BeyondBase): the air-raid money of round 88d, or the difference to the level's own sum (round 88e; may be negative). Coins as the game counts them, not through the start-money multiplier.</summary>
        public int[] RaidStartMoney = new int[0];
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
                // Rounded as the game rounds it (to even) plus the coins beyond it (air-raid money, or the level's own sum), so the model's purse is the wallet's (Levels.StartMoneyRule).
                StartingMoney = (int)Math.Round(facts.StartingMoney * stats.StartMoney) + (difficulty >= 0 && difficulty < facts.RaidStartMoney.Length ? facts.RaidStartMoney[difficulty] : 0),
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
        /// Sizes a level's waves for every difficulty (round 87: by formula, WaveSolver.Solve) and composes them. <paramref name="healths"/>
        /// receives Medium's multipliers.
        /// </summary>
        public List<List<WaveSpec>> BuildWaves(int levelIndex, WaveScriptSpec script, out float[] healths)
        {
            LevelFacts facts = Facts[levelIndex];
            // The script (with the level's scalar) is the source of the target scale at generation; the asset records it.
            Levels[levelIndex].TargetScale = script.TargetScale;
            // The flyer floors are this solve's to set (the Easy raid rule and the first-flyer rule below), never the script's.
            script.FirstAirWaveFloor = new[] { 0 };
            script.FlyingTroopFloor = new[] { 0 };
            int medium = Math.Min(WaveSolver.SolveDifficulty, Difficulties.Count - 1);
            healths = null;
            var sets = new List<List<WaveSpec>>();
            for (int d = 0; d < Difficulties.Count; d++)
            {
                float[] own = SolveDifficulty(levelIndex, script, d, out float[] bossScales);
                if (d == EasyDifficulty)
                {
                    // The Easy raid rule (the curator, round 22f): the first raid waits until the expected player's kills have paid for
                    // a sky gun, plus a wave. Since round 80 the flying troops wait for the same wave (a rift's flyers no longer wait
                    // for the raid itself): one bat out of a rift before that purse made the ledger's Easy player buy the sky gun
                    // out of its ground defence. The floor is read on the waves without any flying troop, so the troops it holds
                    // back do not move it.
                    script.FlyingTroopFloor = WithFloor(script.FlyingTroopFloor, d, script.Waves + 1);
                    own = SolveDifficulty(levelIndex, script, d, out bossScales);
                    int first = WaveComposer.FirstAirWave(script, d);
                    int floor = EasyRaidFloor(levelIndex, ComposedWaves(script, d, own, facts.SecondRoad, bossScales));
                    script.FlyingTroopFloor = WithFloor(script.FlyingTroopFloor, d, floor);
                    if (first != int.MaxValue && floor > first)
                    {
                        script.FirstAirWaveFloor = WithFloor(script.FirstAirWaveFloor, d, floor);
                    }

                    own = SolveDifficulty(levelIndex, script, d, out bossScales);
                }

                if (Rules.FlyersAfterBountiesFor(d))
                {
                    own = HoldFlyersUntilPayable(levelIndex, script, d, own, ref bossScales);
                }

                sets.Add(ComposedWaves(script, d, own, facts.SecondRoad, bossScales));
                if (d == medium)
                {
                    healths = own;
                }
            }

            return sets;
        }

        /// <summary>
        /// The first-flyer rule (<see cref="BalanceRules.FlyersAfterBounties"/>, round 77): while the first flyers of a difficulty
        /// come before the bounties pay for a sky gun (<see cref="CounterPurse"/>: every wave before the first flyer wave plus
        /// the grace waves, through the income share), the difficulty's flyer floors go up a wave (the raids move together, the
        /// flying troops wait) and the difficulty is solved again. A player who owns no gun made for the sky has nothing to pay
        /// for; the level validator's affordability table names that cell instead.
        /// </summary>
        private float[] HoldFlyersUntilPayable(int levelIndex, WaveScriptSpec script, int d, float[] own, ref float[] bossScales)
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
                own = SolveDifficulty(levelIndex, script, d, out bossScales);
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

        // One difficulty as stored: the formula's multipliers (WaveFormula, through WaveSolver.Solve) with the level's own
        // scalar from the closed loop (the script's target scale) and the boss on the bar its level and difficulty give it.
        private float[] SolveDifficulty(int levelIndex, WaveScriptSpec script, int d, out float[] bossScales)
        {
            WaveSolver.Solution solution = WaveSolver.Solve(script, Enemies, d, Facts[levelIndex].SecondRoad, Rules, script.TargetScaleFor(d), ContinentIndexOf(levelIndex), Difficulties[d].Health * Facts[levelIndex].HealthScale);
            bossScales = solution.BossScales;
            return solution.Healths;
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

        /// <summary>
        /// The plain-wave pressure the paper aims a level and difficulty at for a competent player's towers alone (one over
        /// the target surplus). The finale's stand-in waves are trimmed to it (FinaleModel); no stored wave is.
        /// </summary>
        public float ModelTarget(int levelIndex, int difficulty) => 1f / Rules.HumanSurplusAt(difficulty, ContinentIndexOf(levelIndex));

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
