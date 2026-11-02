using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Balance;

namespace BalanceLab
{
    /// <summary>One level as read from its asset: the model's input plus what the writer needs to find its files.</summary>
    public sealed class LevelData
    {
        public string Name = string.Empty;
        public string Key = string.Empty;
        public string Path = string.Empty;
        public int StartingMoney;
        public float HealthScale = 1f;
        public LevelShape Shape;
        public int MineCount;
        public int MineStock;
        public int SecondRoad = -1;
        public List<string> TowerIds = new List<string>();
        public string RewardTowerId = string.Empty;
        public int Research;
        public int Power;
        public string HeroGuid;
        public string GateTowerId = string.Empty;

        /// <summary>Wave asset paths per difficulty index.</summary>
        public List<List<string>> WavePaths = new List<List<string>>();
        public List<List<WaveSpec>> Waves = new List<List<WaveSpec>>();
    }

    /// <summary>Everything the balance model reads, loaded from the generated assets.</summary>
    public sealed class GameData
    {
        public readonly AssetIndex Index;
        public readonly Dictionary<string, EnemyStats> Enemies = new Dictionary<string, EnemyStats>();
        public readonly Dictionary<string, string> EnemyGuids = new Dictionary<string, string>();
        public readonly Dictionary<string, string> EnemyPaths = new Dictionary<string, string>();
        public readonly Dictionary<string, TowerStats> Towers = new Dictionary<string, TowerStats>();
        public readonly Dictionary<string, string> TowerPaths = new Dictionary<string, string>();
        public readonly Dictionary<string, float> TowerHealth = new Dictionary<string, float>();
        public readonly List<DifficultyStats> Difficulties = new List<DifficultyStats>();
        public readonly List<string> DifficultyPaths = new List<string>();
        public readonly List<UpgradeStats> Upgrades = new List<UpgradeStats>();
        public readonly Dictionary<string, string> UpgradePaths = new Dictionary<string, string>();
        public readonly List<LevelData> Levels = new List<LevelData>();
        public readonly List<string> StartingTowers = new List<string>();
        public HeroStats Hero = new HeroStats();
        public int HarvesterCost;

        public GameData(string projectRoot)
        {
            string data = System.IO.Path.Combine(projectRoot, "Assets/_Project/Data");
            Index = new AssetIndex(data);
            LoadEnemies();
            LoadTowers();
            LoadUpgrades();
            LoadCampaign();
            LoadStartingTowers(data);
            string harvester = System.IO.Path.Combine(data, "Drones/HarvestDrone.asset");
            if (File.Exists(harvester))
            {
                HarvesterCost = Index.Read(harvester)["_cost"].Int;
            }
        }

        private void LoadEnemies()
        {
            foreach (string path in Index.OfClass("Game.Enemies.EnemyDefinition"))
            {
                YamlNode n = Index.Read(path);
                string id = AssetIndex.NameOf(path);
                Enemies[id] = new EnemyStats
                {
                    Id = id,
                    Layer = n["_layer"].Int,
                    Flies = n["_flies"].Bool,
                    Health = n["_maxHealth"].Float,
                    Armor = n["_armor"].Float,
                    Speed = n["_speed"].Float,
                    Reward = n["_reward"].Int,
                    BaseDamage = n["_baseDamage"].Int,
                    AttackDamage = n["_attackDamage"].Float,
                    AttackInterval = n["_attackInterval"].Float,
                    BlockadeDamage = n["_blockadeDamage"].Float,
                    IsBoss = n["_isBoss"].Bool,
                };
                EnemyGuids[id] = Index.GuidOf(path);
                EnemyPaths[id] = path;
            }
        }

        private void LoadTowers()
        {
            foreach (string path in Index.OfClass("Game.Towers.TowerDefinition"))
            {
                YamlNode n = Index.Read(path);
                string id = AssetIndex.NameOf(path);
                var levels = new List<TowerLevelStats>();
                foreach (YamlNode level in n["_levels"].Items)
                {
                    levels.Add(new TowerLevelStats(level["Cost"].Int, level["Damage"].Float, level["Range"].Float, level["FireInterval"].Float, level["SlowFactor"].Float));
                }

                Towers[id] = new TowerStats
                {
                    Id = id,
                    Targets = n["_targets"].Int,
                    Splash = n["_splashRadius"].Float,
                    IsSupport = n["_isSupport"].Bool,
                    BlocksGround = n["_blocksGroundEnemies"].Bool,
                    IsBaseDefence = n["_isBaseDefence"].Bool,
                    SupportRateBonus = n["_supportRateBonus"].Float,
                    Levels = levels.ToArray(),
                };
                TowerPaths[id] = path;
                TowerHealth[id] = n["_maxHealth"].Float;
            }
        }

        private void LoadUpgrades()
        {
            foreach (string path in Index.OfClass("Game.Economy.UpgradeDefinition"))
            {
                YamlNode n = Index.Read(path);
                string id = AssetIndex.NameOf(path);
                Upgrades.Add(new UpgradeStats
                {
                    Id = id,
                    Kind = KindOf(n["_target"].Int),
                    TowerId = n["_towerId"].Text,
                    PaidInPower = n["_currency"].Int == 1,
                    BonusPerLevel = n["_bonusPerLevel"].Float,
                    Costs = HexInts(n["_costs"].Text),
                });
                UpgradePaths[id] = path;
            }

            // The catalogue order decides ties in the progression model, so read it the way the game does.
            string catalog = Index.OfClass("Game.Economy.UpgradeCatalog").Count > 0 ? Index.OfClass("Game.Economy.UpgradeCatalog")[0] : null;
            if (catalog != null)
            {
                var order = new List<string>();
                foreach (YamlNode item in Index.Read(catalog)["_upgrades"].Items)
                {
                    order.Add(AssetIndex.NameOf(Index.PathOf(item.Guid)));
                }

                Upgrades.RemoveAll(u => !order.Contains(u.Id));
                Upgrades.Sort((a, b) => order.IndexOf(a.Id).CompareTo(order.IndexOf(b.Id)));
            }
        }

        // Game.Economy.UpgradeTarget values.
        private static UpgradeKind KindOf(int target)
        {
            switch (target)
            {
                case 0:
                    return UpgradeKind.TowerDamage;
                case 1:
                    return UpgradeKind.TowerRange;
                case 2:
                    return UpgradeKind.TowerFireRate;
                case 4:
                    return UpgradeKind.BaseHealth;
                case 5:
                    return UpgradeKind.BaseWeapon;
                case 6:
                    return UpgradeKind.GateHealth;
                case 7:
                    return UpgradeKind.HeroDamage;
                default:
                    return UpgradeKind.Other;
            }
        }

        /// <summary>Unity writes a plain int array as little-endian hex, eight digits per value.</summary>
        public static int[] HexInts(string hex)
        {
            int count = hex.Length / 8;
            var values = new int[count];
            for (int i = 0; i < count; i++)
            {
                string chunk = hex.Substring(i * 8, 8);
                uint value = 0;
                for (int b = 3; b >= 0; b--)
                {
                    value = (value << 8) | uint.Parse(chunk.Substring(b * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }

                values[i] = (int)value;
            }

            return values;
        }

        public static string ToHexInts(int[] values)
        {
            var text = new System.Text.StringBuilder();
            foreach (int value in values)
            {
                uint v = (uint)value;
                for (int b = 0; b < 4; b++)
                {
                    text.Append(((v >> (b * 8)) & 0xff).ToString("x2", CultureInfo.InvariantCulture));
                }
            }

            return text.ToString();
        }

        private void LoadStartingTowers(string data)
        {
            string unlocks = System.IO.Path.Combine(data, "Unlocks/UnlockTable.asset");
            if (!File.Exists(unlocks))
            {
                StartingTowers.Add("PulseRifle");
                return;
            }

            foreach (YamlNode id in Index.Read(unlocks)["_startingTowerIds"].Items)
            {
                StartingTowers.Add(id.Text);
            }
        }

        private void LoadCampaign()
        {
            string catalog = Index.OfClass("Game.Levels.CampaignCatalog")[0];
            YamlNode root = Index.Read(catalog);
            foreach (YamlNode difficulty in root["_difficulties"].Items)
            {
                string path = Index.PathOf(difficulty.Guid);
                YamlNode n = Index.Read(path);
                Difficulties.Add(new DifficultyStats
                {
                    Id = AssetIndex.NameOf(path),
                    Index = Difficulties.Count,
                    Health = n["_healthMultiplier"].Float,
                    Count = n["_countMultiplier"].Float,
                    Interval = n["_spawnIntervalMultiplier"].Float,
                    Research = n["_rewardMultiplier"].Float,
                    Power = n["_powerCurrencyMultiplier"].Float,
                    StructureDamage = n["_structureDamageMultiplier"].Float,
                });
                DifficultyPaths.Add(path);
            }

            foreach (YamlNode node in root["_nodes"].Items)
            {
                string guid = node["Level"].Guid;
                if (guid == null || Index.PathOf(guid) == null)
                {
                    continue;
                }

                Levels.Add(LoadLevel(Index.PathOf(guid)));
            }
        }

        private LevelData LoadLevel(string path)
        {
            YamlNode n = Index.Read(path);
            var level = new LevelData
            {
                Name = AssetIndex.NameOf(path),
                Path = path,
                StartingMoney = n["_startingMoney"].Int,
                HealthScale = n["_enemyHealthMultiplier"].Float,
                MineCount = n["_mineCells"].Items.Count,
                MineStock = n["_mineStock"].Int,
                Research = n["_reward"]["ResearchPoints"].Int,
                Power = n["_reward"]["PowerCurrency"].Int,
                HeroGuid = n["_hero"].Guid,
            };
            level.Key = level.Name.StartsWith("Level_", StringComparison.Ordinal) ? level.Name.Substring(6) : level.Name;
            string reward = n["_reward"]["NewTower"].Guid;
            level.RewardTowerId = reward != null && Index.PathOf(reward) != null ? AssetIndex.NameOf(Index.PathOf(reward)) : string.Empty;
            foreach (YamlNode tower in n["_availableTowers"].Items)
            {
                level.TowerIds.Add(AssetIndex.NameOf(Index.PathOf(tower.Guid)));
            }

            var roads = new List<RoadInput>();
            YamlNode paths = n["_paths"];
            for (int i = 0; i < paths.Items.Count; i++)
            {
                YamlNode p = paths.Items[i];
                if (p["Flying"].Bool)
                {
                    continue;
                }

                if (i > 0 && level.SecondRoad < 0)
                {
                    level.SecondRoad = i;
                }

                roads.Add(new RoadInput { PathIndex = i, Waypoints = Cells(p["Waypoints"]) });
            }

            var gates = new List<Cell>();
            foreach (YamlNode spot in n["_blockades"].Items)
            {
                gates.Add(CellOf(spot["Cell"]));
                if (string.IsNullOrEmpty(level.GateTowerId))
                {
                    level.GateTowerId = AssetIndex.NameOf(Index.PathOf(spot["Blockade"].Guid));
                }
            }

            var nests = new List<Cell>();
            foreach (YamlNode nest in n["_nests"].Items)
            {
                nests.Add(CellOf(nest["Cell"]));
            }

            YamlNode entrance = n["_bossEntrance"];
            Cell? entranceCell = entrance.Exists && entrance["Kind"].Int != 0 ? CellOf(entrance["Cell"]) : (Cell?)null;
            level.Shape = LevelShape.Build(roads, gates, nests, entranceCell);

            if (level.HeroGuid != null)
            {
                LoadHero(level.HeroGuid);
            }

            foreach (YamlNode set in n["_difficultyWaves"].Items)
            {
                var wavePaths = new List<string>();
                var waves = new List<WaveSpec>();
                foreach (YamlNode wave in set["Waves"].Items)
                {
                    string wavePath = Index.PathOf(wave.Guid);
                    wavePaths.Add(wavePath);
                    waves.Add(LoadWave(wavePath));
                }

                level.WavePaths.Add(wavePaths);
                level.Waves.Add(waves);
            }

            return level;
        }

        private void LoadHero(string guid)
        {
            YamlNode hero = Index.ReadByGuid(guid);
            YamlNode blade = Index.ReadByGuid(hero["_weapon"].Guid);
            YamlNode gun = Index.ReadByGuid(hero["_rangedWeapon"].Guid);
            Hero = new HeroStats
            {
                BladeDamage = blade?["_damage"].Float ?? 0f,
                BladeInterval = blade?["_attackInterval"].Float ?? 1f,
                GunDamage = gun?["_damage"].Float ?? 0f,
                GunInterval = gun?["_attackInterval"].Float ?? 1f,
            };
        }

        public WaveSpec LoadWave(string path)
        {
            YamlNode n = Index.Read(path);
            var wave = new WaveSpec
            {
                StartDelay = n["_startDelay"].Float,
                HealthMultiplier = n["_healthMultiplier"].Float,
            };
            foreach (YamlNode g in n["_groups"].Items)
            {
                string enemy = AssetIndex.NameOf(Index.PathOf(g["Enemy"].Guid));
                wave.Groups.Add(new GroupSpec
                {
                    Enemy = Enemies[enemy],
                    Count = g["Count"].Int,
                    Path = g["PathIndex"].Int,
                    Source = g["Source"].Int,
                    Interval = g["Interval"].Float,
                    Delay = g["Delay"].Float,
                    Stagger = g["Stagger"].Float,
                });
            }

            return wave;
        }

        private static Cell[] Cells(YamlNode list)
        {
            var cells = new List<Cell>();
            foreach (YamlNode item in list.Items)
            {
                cells.Add(CellOf(item));
            }

            return cells.ToArray();
        }

        private static Cell CellOf(YamlNode node) => new Cell(node["x"].Int, node["y"].Int);
    }
}
