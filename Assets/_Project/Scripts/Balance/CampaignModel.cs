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
        public int SecondRoad = -1;
        public float GateHealth;
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

        public LevelInput InputFor(int levelIndex, int difficulty)
        {
            LevelFacts facts = Facts[levelIndex];
            PlayerState player = PlayerAt(levelIndex, difficulty);
            var input = new LevelInput
            {
                Key = facts.Key,
                StartingMoney = facts.StartingMoney,
                HealthScale = facts.HealthScale,
                Shape = facts.Shape,
                MineCount = facts.MineCount,
                MineStock = facts.MineStock,
                HarvesterCost = HarvesterCost,
                Hero = Hero,
                HeroDamage = player.HeroDamage,
                GateHealth = facts.GateHealth * player.GateHealth,
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

        /// <summary>Solves a level's budgets on Medium and composes its waves for every difficulty.</summary>
        public List<List<WaveSpec>> BuildWaves(int levelIndex, WaveScriptSpec script, out float[] healths)
        {
            LevelFacts facts = Facts[levelIndex];
            int medium = Math.Min(WaveSolver.SolveDifficulty, Difficulties.Count - 1);
            healths = WaveSolver.Solve(script, Enemies, InputFor(levelIndex, medium), Difficulties[medium], facts.SecondRoad, Rules);
            var sets = new List<List<WaveSpec>>();
            for (int d = 0; d < Difficulties.Count; d++)
            {
                float[] own = d == medium ? healths : WaveSolver.SolveVariant(script, Enemies, InputFor(levelIndex, d), Difficulties[d], healths, facts.SecondRoad, Rules);
                var waves = new List<WaveSpec>();
                for (int i = 0; i < script.Waves; i++)
                {
                    waves.Add(WaveComposer.Compose(script, Enemies, d, i + 1, own[i], facts.SecondRoad, Rules));
                }

                sets.Add(waves);
            }

            return sets;
        }

        /// <summary>Total research and power the campaign pays per difficulty, against what the upgrades cost.</summary>
        public void MetaTotals(out int researchCost, out int powerCost, out int[] researchPerDifficulty, out int[] powerPerDifficulty)
        {
            researchCost = 0;
            powerCost = 0;
            foreach (UpgradeStats upgrade in Upgrades)
            {
                if (upgrade.PaidInPower)
                {
                    powerCost += upgrade.TotalCost;
                }
                else
                {
                    researchCost += upgrade.TotalCost;
                }
            }

            researchPerDifficulty = new int[Difficulties.Count];
            powerPerDifficulty = new int[Difficulties.Count];
            for (int d = 0; d < Difficulties.Count; d++)
            {
                foreach (CampaignLevel level in Levels)
                {
                    researchPerDifficulty[d] += (int)Math.Round(level.Research * Difficulties[d].Research, MidpointRounding.AwayFromZero);
                    powerPerDifficulty[d] += (int)Math.Round(level.Power * Difficulties[d].Power, MidpointRounding.AwayFromZero);
                }
            }
        }
    }
}
