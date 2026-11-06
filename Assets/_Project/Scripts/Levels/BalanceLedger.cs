using System.Collections.Generic;
using Game.Balance;
using Game.Economy;
using Game.Enemies;
using Game.Towers;

namespace Game.Levels
{
    /// <summary>
    /// The bridge between the game's definitions and the pure balance model (<c>Game.Balance</c>): turns towers,
    /// creatures, difficulty profiles, upgrades, maps and waves into the model's numbers, and builds the campaign the
    /// ledger and the wave solver judge. The generator, the tests and the BalanceLab tool all go through the same model.
    /// </summary>
    public static class BalanceLedger
    {
        public static EnemyStats Stats(EnemyDefinition enemy) => new EnemyStats
        {
            Id = enemy.name,
            Layer = (int)enemy.Layer,
            Flies = enemy.Flies,
            // An ice armour that soaks part of each hit counts as that much more health to burn through.
            Health = enemy.MaxHealth * (enemy.Boss != null ? enemy.Boss.EffectiveHealthFactor() : 1f),
            Armor = enemy.Armor,
            Plating = enemy.Plating,
            PlatingFloor = enemy.PlatingFloor,
            Speed = enemy.AverageSpeed,
            FightSpeed = enemy.Rages ? enemy.AverageSpeed * enemy.RageSpeedMultiplier : 0f,
            Reward = enemy.Reward,
            BaseDamage = enemy.BaseDamage,
            AttackDamage = enemy.AttackDamage,
            AttackInterval = enemy.AttackInterval,
            BlockadeDamage = enemy.BlockadeDamage,
            IsBoss = enemy.IsBoss,
            MechanicLossSeconds = enemy.Boss != null ? enemy.Boss.FirepowerLossSeconds() : 0f,
            IsContinentBoss = enemy.Boss != null,
        };

        public static TowerStats Stats(TowerDefinition tower)
        {
            var levels = new TowerLevelStats[tower.MaxLevelIndex + 1];
            for (int i = 0; i < levels.Length; i++)
            {
                TowerLevel level = tower.GetLevel(i);
                levels[i] = new TowerLevelStats(level.Cost, level.Damage, level.Range, level.FireInterval, level.SlowFactor);
            }

            return new TowerStats
            {
                Id = tower.Id,
                Targets = (int)tower.Targets,
                Splash = tower.SplashRadius,
                GroundFactor = tower.GroundDamageFactor,
                AirFactor = tower.AirDamageFactor,
                IsSupport = tower.IsSupport,
                BlocksGround = tower.BlocksGroundEnemies,
                IsBaseDefence = tower.IsBaseDefence,
                Levels = levels,
            };
        }

        public static DifficultyStats Stats(DifficultyProfile difficulty, int index) => new DifficultyStats
        {
            Id = difficulty.name,
            Index = index,
            Health = difficulty.HealthMultiplier,
            Count = difficulty.CountMultiplier,
            Interval = difficulty.SpawnIntervalMultiplier,
            Research = difficulty.RewardMultiplier,
            Power = difficulty.PowerCurrencyMultiplier,
            StructureDamage = difficulty.StructureDamageMultiplier,
            Income = difficulty.IncomeMultiplier,
            StartMoney = difficulty.StartingMoneyMultiplier,
            BossHealth = difficulty.BossHealthShare,
        };

        public static UpgradeStats Stats(UpgradeDefinition upgrade)
        {
            var costs = new int[upgrade.MaxLevel];
            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = upgrade.CostForNextLevel(i);
            }

            return new UpgradeStats
            {
                Id = upgrade.Id,
                Kind = KindOf(upgrade.Target),
                TowerId = upgrade.TowerId,
                PaidInPower = upgrade.Currency == UpgradeCurrency.PowerCurrency,
                BonusPerLevel = upgrade.BonusPerLevel,
                Costs = costs,
                Continent = upgrade.Continent,
            };
        }

        private static UpgradeKind KindOf(UpgradeTarget target)
        {
            switch (target)
            {
                case UpgradeTarget.TowerDamage:
                    return UpgradeKind.TowerDamage;
                case UpgradeTarget.TowerRange:
                    return UpgradeKind.TowerRange;
                case UpgradeTarget.TowerFireRate:
                    return UpgradeKind.TowerFireRate;
                case UpgradeTarget.BaseHealth:
                    return UpgradeKind.BaseHealth;
                case UpgradeTarget.BaseWeapon:
                    return UpgradeKind.BaseWeapon;
                case UpgradeTarget.BlockadeHealth:
                    return UpgradeKind.GateHealth;
                case UpgradeTarget.HeroDamage:
                    return UpgradeKind.HeroDamage;
                default:
                    return UpgradeKind.Other;
            }
        }

        /// <summary>The map as the model sees it: roads, gates, nests and the boss entrance.</summary>
        public static LevelShape Shape(LevelDefinition level)
        {
            var roads = new List<RoadInput>();
            for (int i = 0; i < level.Paths.Length; i++)
            {
                if (!level.Paths[i].Flying)
                {
                    roads.Add(new RoadInput { PathIndex = i, Waypoints = Cells(level.Paths[i].Waypoints) });
                }
            }

            var gates = new List<Cell>();
            foreach (BlockadeSpot spot in level.Blockades)
            {
                gates.Add(new Cell(spot.Cell.x, spot.Cell.y));
            }

            var nests = new List<Cell>();
            foreach (ElementNest nest in level.Nests)
            {
                nests.Add(new Cell(nest.Cell.x, nest.Cell.y));
            }

            Cell? entrance = level.BossEntrance.Kind != EntranceKind.Gate ? new Cell(level.BossEntrance.Cell.x, level.BossEntrance.Cell.y) : (Cell?)null;
            return LevelShape.Build(roads, gates, nests, entrance);
        }

        /// <summary>Index of the first ground path after the main road, or -1: the second road of the waves.</summary>
        public static int SecondRoad(LevelDefinition level)
        {
            for (int i = 1; i < level.Paths.Length; i++)
            {
                if (!level.Paths[i].Flying)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The whole campaign: levels in map order, every tower, creature and upgrade they use, and the starting set.</summary>
        public static CampaignModel Campaign(CampaignCatalog catalog, UpgradeCatalog upgrades, IEnumerable<string> startingTowers, BalanceRules rules)
        {
            var model = new CampaignModel { Rules = rules };
            model.StartingTowers.AddRange(startingTowers);
            for (int d = 0; d < catalog.Difficulties.Count; d++)
            {
                model.Difficulties.Add(Stats(catalog.Difficulties[d], d));
            }

            if (upgrades != null)
            {
                foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
                {
                    model.Upgrades.Add(Stats(upgrade));
                }
            }

            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    Add(model, node.Level, node.Region);
                }
            }

            return model;
        }

        private static void Add(CampaignModel model, LevelDefinition level, int continent)
        {
            var campaign = new CampaignLevel
            {
                Continent = continent,
                Key = level.name.StartsWith("Level_") ? level.name.Substring(6) : level.name,
                RewardTowerId = level.Reward.NewTower != null ? level.Reward.NewTower.Id : string.Empty,
                RewardHeroPerkId = level.Reward.HeroPerk != null ? level.Reward.HeroPerk.Id : string.Empty,
                Research = level.Reward.ResearchPoints,
                Power = level.Reward.PowerCurrency,
            };
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                campaign.TowerIds.Add(tower.Id);
                model.Towers[tower.Id] = Stats(tower);
            }

            float gateHealth = level.Blockades.Length > 0 && level.Blockades[0].Blockade != null ? level.Blockades[0].Blockade.MaxHealth : 0f;
            model.Levels.Add(campaign);
            model.Facts.Add(new LevelFacts
            {
                Key = campaign.Key,
                StartingMoney = level.StartingMoney,
                HealthScale = level.EnemyHealthScale,
                Shape = Shape(level),
                MineCount = level.MineCells.Length,
                MineStock = level.MineStock,
                Harvest = level.HarvestDrone != null,
                SecondRoad = SecondRoad(level),
                GateHealth = gateHealth,
                FieldEase = level.BalanceFieldEase,
            });
            if (level.HarvestDrone != null)
            {
                model.HarvesterCost = level.HarvestDrone.Cost;
            }

            if (level.Hero != null && level.Hero.Weapon != null)
            {
                model.Hero = new HeroStats
                {
                    BladeDamage = level.Hero.Damage,
                    BladeInterval = level.Hero.StrikeInterval,
                    GunDamage = level.Hero.RangedWeapon != null ? level.Hero.RangedWeapon.Damage : 0f,
                    GunInterval = level.Hero.RangedWeapon != null ? level.Hero.RangedWeapon.AttackInterval : 1f,
                    Magazine = level.Hero.RangedWeapon != null ? level.Hero.RangedWeapon.Magazine : 0,
                    ReloadSeconds = level.Hero.RangedWeapon != null ? level.Hero.RangedWeapon.ReloadSeconds : 0f,
                };
                foreach (Game.Hero.HeroPerkDefinition perk in level.Hero.Perks)
                {
                    if (perk != null)
                    {
                        model.Hero.Perks.Add(new HeroPerkStats { Id = perk.Id, IntervalMultiplier = perk.RangedIntervalMultiplier, ReloadMultiplier = perk.ReloadMultiplier });
                    }
                }
            }

            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                foreach (WaveDefinition wave in set.Waves)
                {
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        if (group.Enemy != null && !model.Enemies.ContainsKey(group.Enemy.name))
                        {
                            model.Enemies[group.Enemy.name] = Stats(group.Enemy);
                        }
                    }
                }
            }
        }

        /// <summary>A stored wave as the model reads it, with the creatures from the model's own table.</summary>
        public static WaveSpec Spec(WaveDefinition wave, IDictionary<string, EnemyStats> enemies)
        {
            var spec = new WaveSpec { StartDelay = wave.StartDelay, HealthMultiplier = wave.HealthMultiplier };
            foreach (SpawnGroup group in wave.Groups)
            {
                if (group.Enemy == null)
                {
                    continue;
                }

                if (!enemies.TryGetValue(group.Enemy.name, out EnemyStats enemy))
                {
                    enemy = Stats(group.Enemy);
                    enemies[group.Enemy.name] = enemy;
                }

                spec.Groups.Add(new GroupSpec
                {
                    Enemy = enemy,
                    Count = group.Count,
                    Path = group.PathIndex,
                    Source = (int)group.Source,
                    Interval = group.Interval,
                    Delay = group.Delay,
                    Stagger = group.Stagger,
                });
            }

            return spec;
        }

        /// <summary>The ledger of a level's waves on one difficulty, as they are stored, against the expected player.</summary>
        public static List<WaveLine> Evaluate(CampaignModel model, int levelIndex, LevelDefinition level, DifficultyProfile profile, int difficulty) =>
            Evaluate(model, levelIndex, level, profile, difficulty, null);

        /// <summary>As above; <paramref name="loads"/>, when given, receives every wave's load (see <see cref="Ledger.PressureOf"/>).</summary>
        public static List<WaveLine> Evaluate(CampaignModel model, int levelIndex, LevelDefinition level, DifficultyProfile profile, int difficulty, List<WaveLoad> loads) =>
            Evaluate(model, levelIndex, level, profile, difficulty, loads, model.Rules);

        /// <summary>As above, judged by other rules (the human lens, <see cref="BalanceRules.HumanTowersOnly"/>).</summary>
        public static List<WaveLine> Evaluate(CampaignModel model, int levelIndex, LevelDefinition level, DifficultyProfile profile, int difficulty, List<WaveLoad> loads, BalanceRules rules)
        {
            var waves = new List<WaveSpec>();
            foreach (WaveDefinition wave in level.GetWaves(profile))
            {
                waves.Add(Spec(wave, model.Enemies));
            }

            return Ledger.Evaluate(model.InputFor(levelIndex, difficulty), waves, model.Difficulties[difficulty], rules, loads);
        }

        private static Cell[] Cells(UnityEngine.Vector2Int[] waypoints)
        {
            var cells = new Cell[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                cells[i] = new Cell(waypoints[i].x, waypoints[i].y);
            }

            return cells;
        }
    }
}
