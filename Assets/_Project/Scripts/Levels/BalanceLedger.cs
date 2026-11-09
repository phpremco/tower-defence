using System.Collections.Generic;
using Game.Balance;
using Game.Economy;
using Game.Enemies;
using Game.Towers;
using UnityEngine;

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
                levels[i] = new TowerLevelStats(level.Cost, level.Damage, level.Range, level.FireInterval, level.SlowFactor, level.HealthBonus);
            }

            return new TowerStats
            {
                Id = tower.Id,
                Targets = (int)tower.Targets,
                Splash = tower.SplashRadius,
                GroundFactor = tower.GroundDamageFactor,
                AirFactor = tower.AirDamageFactor,
                ArmourFactor = tower.ArmourFactor,
                SplashTargets = tower.SplashTargets,
                SellRefund = tower.SellRefundFraction,
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

        /// <summary>Cells beside the road (within this many cells) a tower may stand on: the plan model's build spots.</summary>
        public const float BesideRoadCells = 2f;

        /// <summary>
        /// The map for placement: walking and flight routes, gates, the defence gate (the first on the trunk, the ledger's kill
        /// point), every buildable cell beside the road with its reach multiplier from the terrain (<see cref="TerrainBalance"/>).
        /// </summary>
        public static LevelGeometry Geometry(LevelDefinition level)
        {
            var roads = new List<RoadInput>();
            var flights = new List<RoadInput>();
            for (int i = 0; i < level.Paths.Length; i++)
            {
                (level.Paths[i].Flying ? flights : roads).Add(new RoadInput { PathIndex = i, Waypoints = Cells(level.Paths[i].Waypoints) });
            }

            var gates = new List<Cell>();
            foreach (BlockadeSpot spot in level.Blockades)
            {
                gates.Add(new Cell(spot.Cell.x, spot.Cell.y));
            }

            LevelShape shape = Shape(level);
            Cell? defence = null;
            int best = -1;
            for (int i = 0; i < shape.Gates.Count && i < level.Blockades.Length; i++)
            {
                if (shape.Gates[i].OnTrunk && (best < 0 || shape.Gates[i].Progress < shape.Gates[best].Progress))
                {
                    best = i;
                }
            }

            if (best < 0 && level.Blockades.Length > 0)
            {
                best = 0;
            }

            if (best >= 0)
            {
                defence = gates[best];
            }

            var spots = new List<Cell>();
            var grid = new BuildGrid(level);
            var road = new List<Vector2Int>(level.Road.Cells);
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!grid.CanBuild(cell))
                    {
                        continue;
                    }

                    float nearest = float.MaxValue;
                    foreach (Vector2Int roadCell in road)
                    {
                        nearest = Mathf.Min(nearest, Vector2Int.Distance(cell, roadCell));
                    }

                    if (nearest <= BesideRoadCells)
                    {
                        spots.Add(new Cell(x, y));
                    }
                }
            }

            LevelGeometry geometry = LevelGeometry.Build(roads, flights, gates, defence, spots, level.CellSize);
            foreach (Cell spot in spots)
            {
                float multiplier = TerrainBalance.RangeMultiplier(level.Terrain.KindAt(new Vector2Int(spot.X, spot.Y)));
                if (multiplier != 1f)
                {
                    geometry.SetSpotRange(spot, multiplier);
                }
            }

            return geometry;
        }

        /// <summary>The gadgets a level sells, as the plan model prices them.</summary>
        public static List<GadgetStats> Gadgets(LevelDefinition level)
        {
            var gadgets = new List<GadgetStats>();
            foreach (Game.PathObjects.PathObjectDefinition item in level.PathObjects)
            {
                if (item == null)
                {
                    continue;
                }

                gadgets.Add(new GadgetStats
                {
                    Id = item.name,
                    Kind = item.Kind == Game.PathObjects.PathObjectKind.Mine ? GadgetKind.Mine : item.Kind == Game.PathObjects.PathObjectKind.SlowField ? GadgetKind.SlowField : GadgetKind.Other,
                    Cost = item.Cost,
                    Damage = item.Damage,
                    Radius = item.Radius,
                    SlowFactor = item.SlowFactor,
                });
            }

            return gadgets;
        }

        /// <summary>The drones a level offers, as the plan model prices them.</summary>
        public static List<DroneStats> Drones(LevelDefinition level)
        {
            var drones = new List<DroneStats>();
            foreach (Game.Drones.DroneDefinition drone in level.Drones)
            {
                if (drone == null)
                {
                    continue;
                }

                drones.Add(new DroneStats
                {
                    Id = drone.name,
                    Role = drone.Kind == Game.Drones.DroneKind.Harvest ? DroneRole.Harvest : drone.Kind == Game.Drones.DroneKind.Repair ? DroneRole.Repair : drone.Kind == Game.Drones.DroneKind.Attack ? DroneRole.Attack : DroneRole.Other,
                    Cost = drone.Cost,
                    Dps = drone.FireInterval > 0f ? drone.Damage / drone.FireInterval : 0f,
                    Targets = (int)drone.Targets,
                    RepairPerSecond = drone.RepairPerSecond,
                });
            }

            return drones;
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

        /// <summary>
        /// The whole campaign: levels in map order, every tower, creature and upgrade they use, and the starting set, with the
        /// calibration asset's measured numbers applied (<see cref="BalanceCalibration"/>; null leaves the rules' defaults).
        /// </summary>
        public static CampaignModel Campaign(CampaignCatalog catalog, UpgradeCatalog upgrades, IEnumerable<string> startingTowers, BalanceRules rules, BalanceCalibration calibration)
        {
            CampaignModel model = Campaign(catalog, upgrades, startingTowers, rules);
            calibration?.Apply(model);
            return model;
        }

        /// <summary>The whole campaign without a calibration: every tower at efficiency one, the rules as given.</summary>
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
                TargetScale = level.BalanceTargetScale,
            };
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                campaign.TowerIds.Add(tower.Id);
                model.Towers[tower.Id] = Stats(tower);
            }

            Game.Towers.TowerDefinition blockade = level.Blockades.Length > 0 ? level.Blockades[0].Blockade : null;
            float gateHealth = blockade != null ? blockade.MaxHealth : 0f;
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
                GateRepairCostPerHealth = blockade != null ? blockade.RepairCostPerHealth : 0f,
                GateRebuildCost = blockade != null ? blockade.BuildCost : 0,
                Gate = blockade != null ? Stats(blockade) : null,
                BaseHealth = level.BaseHealth,
                StarFractions = level.StarBaseHealthFractions,
                BaseTurret = level.BaseTurret != null ? Stats(level.BaseTurret) : null,
                BaseTurretSides = PlacementController.BaseTurretSides,
                Gadgets = Gadgets(level),
                Drones = Drones(level),
                Geometry = Geometry(level),
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
                    MaxHealth = level.Hero.MaxHealth,
                    GuardShare = level.Hero.GuardShare,
                    Blocks = !level.Hero.Flies,
                    ReviveSeconds = level.Hero.ReviveSeconds,
                    BladeArmourFactor = level.Hero.Weapon.ArmourFactor,
                    GunArmourFactor = level.Hero.RangedWeapon != null ? level.Hero.RangedWeapon.ArmourFactor : 1f,
                    RegenPerSecond = level.Hero.RegenPerSecond,
                    RegenDelaySeconds = level.Hero.RegenDelaySeconds,
                };
                foreach (Game.Hero.HeroAbilityDefinition ability in level.Hero.Abilities)
                {
                    // The pulse is the one ability that kills; the gate repair is the plan model's repair step.
                    if (ability != null && ability.Kind == Game.Hero.HeroAbilityKind.Shockwave && ability.RequiresPerk == null)
                    {
                        model.Hero.PulseDamage = ability.Damage;
                        model.Hero.PulseCooldown = ability.Cooldown;
                        model.Hero.PulseArmourFactor = ability.ArmourFactor;
                    }
                }
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
