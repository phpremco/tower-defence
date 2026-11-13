using System.Collections.Generic;
using Game.Balance;
using Game.Drones;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// What one probe bot run keeps track of (round 20): its strategy and seeded draws, what it built, sold and placed, the
    /// drones it bought and what they brought in, per tower type the damage really dealt against the data's DPS, and (round
    /// 22) per wave the towers standing, the health the wave spawned and the damage each tower type took off that wave's
    /// creatures: the records the calibration is fitted on (<see cref="CalibrationFit"/>).
    /// </summary>
    internal sealed class BotRun
    {
        private const int SeedSpread = 7919;
        private const float DroneLookupSeconds = 1f;

        private readonly System.Random _random;
        private readonly HashSet<Enemy> _watched = new HashSet<Enemy>();
        private readonly List<Game.Combat.ITargetable> _near = new List<Game.Combat.ITargetable>();
        private readonly Dictionary<Enemy, int> _spawnWave = new Dictionary<Enemy, int>();
        private readonly Dictionary<Enemy, float> _lastHealth = new Dictionary<Enemy, float>();
        private readonly Dictionary<int, WaveRecord> _records = new Dictionary<int, WaveRecord>();
        private Game.Combat.TargetRegistry _targets;
        private LevelContext _context;
        private int _recordedWave = -1;
        private float _lastBase = -1f;
        private float _lastGates;
        private Drone[] _drones = new Drone[0];
        private float _droneLookup;
        public int Repairs;

        public BotRun(ProbeStrategy strategy, int seed)
        {
            Strategy = strategy;
            Seed = seed;
            // Each strategy draws its own stream of the seed (the name's hash, not its length: "mono Cannon" and "mono Rocket" differ).
            _random = new System.Random(unchecked(seed * SeedSpread + Hash(strategy.Name)));
        }

        public ProbeStrategy Strategy { get; }

        public int Seed { get; }

        public int Built;
        public int Upgrades;
        public int Sold;
        public int Mines;
        public int Fields;
        public int Harvested;
        public float DroneDamage;
        public float Seconds;
        /// <summary>The largest share of its health any gun or gate has lost so far: the creative bot's cue for a repair drone.</summary>
        public float Wear;

        /// <summary>The build order really taken (builds, upgrades, gadgets, drones, sales with their wave), for the README and the acceptance rules (round 22e).</summary>
        public readonly List<PlanStep> Moves = new List<PlanStep>();

        public void Note(string kind, string item, Vector2Int cell)
        {
            Moves.Add(new PlanStep { Kind = kind, Item = item ?? string.Empty, X = cell.x, Y = cell.y, Wave = Mathf.Max(1, _context != null ? _context.WaveNumber : 1) });
        }
        public int MinesWave = -1;
        public int MinesThisWave;
        public int FieldWave = -1;
        public int BurrowMined;

        /// <summary>Purchases of the obvious bots (towers and upgrades), for their round-robin rules.</summary>
        public int Purchases;

        /// <summary>Scripted plans: the next step to take, and the steps that could not be taken at all.</summary>
        public int PlanStep;
        public int PlanSkipped;
        public readonly List<string> DronesBought = new List<string>();

        /// <summary>Per tower type: damage dealt, tower-seconds with creatures on the field, and the data damage of those seconds.</summary>
        public readonly Dictionary<string, (float Damage, float Seconds, float DataDamage)> Fire = new Dictionary<string, (float, float, float)>();

        /// <summary>
        /// Round 21a2, the roles in play: damage per tower type by what it hit (see <see cref="RoleOf"/>), and coin-seconds per
        /// tower type (coins spent on the standing towers times the seconds with creatures on the field), for DPS per 100 coins.
        /// </summary>
        public readonly Dictionary<(string Tower, string Role), float> RoleDamage = new Dictionary<(string, string), float>();
        public readonly Dictionary<string, float> CoinSeconds = new Dictionary<string, float>();

        /// <summary>
        /// Round 61, who clears the sky: health really taken off flyers per source (a tower type, the base's own turret, "Hero",
        /// "Drone", else the source's type), per wave the flyers spawned in, and the flyer health each wave brought.
        /// </summary>
        public readonly Dictionary<(int Wave, string Source), float> AirDealt = new Dictionary<(int, string), float>();
        public readonly Dictionary<int, float> AirHealth = new Dictionary<int, float>();

        /// <summary>The wave records in order, for the calibration.</summary>
        public List<WaveRecord> Records
        {
            get
            {
                var list = new List<WaveRecord>(_records.Values);
                list.Sort((a, b) => a.Wave.CompareTo(b.Wave));
                return list;
            }
        }

        /// <summary>The roles a hit is sorted into, in report order.</summary>
        public static readonly string[] Roles = { "loose", "bunched", "plated", "air", "boss" };

        // Light creatures with at least this many others this close (cells) count as bunched: a shell catches them together.
        private const float BunchRadius = 1.2f;
        private const int BunchNeighbours = 2;

        public int Next(int count) => _random.Next(count);

        public void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void Attach(LevelContext context)
        {
            _context = context;
            _targets = context.Targets;
            context.EnemySpawned.Subscribe(OnSpawned);
            context.DroneLaunched.Subscribe(OnDrone);
        }

        public void Detach(LevelContext context)
        {
            context.EnemySpawned.Unsubscribe(OnSpawned);
            context.DroneLaunched.Unsubscribe(OnDrone);
            foreach (Enemy enemy in _watched)
            {
                if (enemy != null)
                {
                    enemy.DamagedBy -= OnDamaged;
                }
            }

            _watched.Clear();
        }

        public void Sample(LevelContext context, List<Tower> towers, float deltaTime) => Sample(context, towers, null, deltaTime);

        public void Sample(LevelContext context, List<Tower> towers, List<Tower> gates, float deltaTime)
        {
            Seconds += deltaTime;
            // The standing set is refreshed while the wave runs, so towers built mid-wave count for it (the paper side counts them too).
            if (context.WaveNumber >= 1)
            {
                _recordedWave = context.WaveNumber;
                WaveRecord record = RecordOf(_recordedWave);
                RecordLosses(context, gates, record);
                RecordRepairDrones(record, deltaTime);
                record.Standing.Clear();
                foreach (Tower tower in towers)
                {
                    if (tower.IsAlive)
                    {
                        Vector2Int cell = context.Level.Grid.WorldToCell(tower.Position);
                        record.Standing.Add((tower.Definition.Id, tower.LevelIndex, new Cell(cell.x, cell.y)));
                    }
                }
            }

            foreach (Tower tower in towers)
            {
                Wear = Mathf.Max(Wear, 1f - tower.HealthFraction);
            }

            for (int i = 0; gates != null && i < gates.Count; i++)
            {
                Wear = gates[i] != null ? Mathf.Max(Wear, 1f - gates[i].HealthFraction) : Wear;
            }

            if (context.Targets.Count == 0)
            {
                return;
            }

            foreach (Tower tower in towers)
            {
                if (!tower.IsAlive)
                {
                    continue;
                }

                TowerLevel level = tower.Definition.GetLevel(tower.LevelIndex);
                float dps = level.FireInterval > 0f ? level.Damage / level.FireInterval : 0f;
                (float damage, float seconds, float data) = Fire.TryGetValue(tower.Definition.Id, out var known) ? known : (0f, 0f, 0f);
                Fire[tower.Definition.Id] = (damage, seconds + deltaTime, data + dps * deltaTime);
                CoinSeconds[tower.Definition.Id] = (CoinSeconds.TryGetValue(tower.Definition.Id, out float coins) ? coins : 0f) + tower.TotalSpent * deltaTime;
            }
        }

        // What the base and the gates lost since the last sample goes on the wave being fought (drops only: a repair is not a gain of the wave).
        private void RecordLosses(LevelContext context, List<Tower> gates, WaveRecord record)
        {
            float baseNow = context.BaseHealth.Current;
            float gatesNow = 0f;
            if (gates != null)
            {
                foreach (Tower gate in gates)
                {
                    gatesNow += gate.Health.Current;
                }
            }

            if (_lastBase >= 0f)
            {
                record.BaseLost += Mathf.Max(0f, _lastBase - baseNow);
                record.GateLost += Mathf.Max(0f, _lastGates - gatesNow);
            }

            _lastBase = baseNow;
            _lastGates = gatesNow;
        }

        // Seconds a repair drone is on the field and seconds it spends repairing, per wave (the drones are looked up once a second).
        private void RecordRepairDrones(WaveRecord record, float deltaTime)
        {
            _droneLookup -= deltaTime;
            if (_droneLookup <= 0f)
            {
                _droneLookup = DroneLookupSeconds;
                _drones = Object.FindObjectsByType<Drone>();
            }

            foreach (Drone drone in _drones)
            {
                if (drone == null || drone.Definition == null || drone.Definition.Kind != DroneKind.Repair)
                {
                    continue;
                }

                record.RepairDroneSeconds += deltaTime;
                record.RepairDroneBusy += drone.Activity == DroneActivity.Repairing ? deltaTime : 0f;
            }
        }

        private WaveRecord RecordOf(int wave)
        {
            if (!_records.TryGetValue(wave, out WaveRecord record))
            {
                _records[wave] = record = new WaveRecord { Wave = wave };
            }

            return record;
        }

        private void OnSpawned(Enemy enemy)
        {
            // Pooled creatures come back: one subscription each.
            if (_watched.Add(enemy))
            {
                enemy.DamagedBy += OnDamaged;
            }

            int wave = _context != null ? Mathf.Max(1, _context.WaveNumber) : 1;
            _spawnWave[enemy] = wave;
            _lastHealth[enemy] = enemy.Health != null ? enemy.Health.Current + enemy.Health.Armor : 0f;
            // A boss's own minions and the like spawn mid-wave and count with the wave they appear in.
            RecordOf(wave).Health += enemy.Health != null ? enemy.Health.Max + enemy.Health.ArmorMax : 0f;
            if (enemy.Definition.Flies && enemy.Health != null)
            {
                AirHealth[wave] = (AirHealth.TryGetValue(wave, out float sky) ? sky : 0f) + enemy.Health.Max + enemy.Health.ArmorMax;
            }
        }

        private static string AirSourceOf(object source)
        {
            switch (source)
            {
                case Tower tower:
                    return tower.Definition.Id;
                case Drone _:
                    return "Drone";
                case Game.Hero.Commander _:
                    return "Hero";
                default:
                    return source != null ? source.GetType().Name : "unknown";
            }
        }

        private void OnDamaged(Enemy enemy, object source, float amount)
        {
            WaveRecord record = RecordOf(_spawnWave.TryGetValue(enemy, out int wave) ? wave : Mathf.Max(1, _context.WaveNumber));
            // What the hit really took off the creature, armour chipped and body hurt (the paper counts both as effective health);
            // the armour factor and overkill are the paper model's business, not realised damage.
            float left = enemy.Health.Current + enemy.Health.Armor;
            float before = _lastHealth.TryGetValue(enemy, out float last) ? last : enemy.Health.Max + enemy.Health.ArmorMax;
            float dealt = Mathf.Max(0f, before - left);
            _lastHealth[enemy] = left;
            if (enemy.Definition.Flies)
            {
                var sky = (record.Wave, AirSourceOf(source));
                AirDealt[sky] = (AirDealt.TryGetValue(sky, out float taken) ? taken : 0f) + dealt;
            }

            if (source is Tower tower && !tower.Definition.IsBaseDefence)
            {
                (float damage, float seconds, float data) = Fire.TryGetValue(tower.Definition.Id, out var known) ? known : (0f, 0f, 0f);
                Fire[tower.Definition.Id] = (damage + amount, seconds, data);
                // The role numbers count what the hit really took off the creature: a shell on one small ant counts its last hit points.
                var key = (tower.Definition.Id, RoleOf(enemy));
                RoleDamage[key] = (RoleDamage.TryGetValue(key, out float sum) ? sum : 0f) + amount;
                record.Dealt[tower.Definition.Id] = (record.Dealt.TryGetValue(tower.Definition.Id, out float known2) ? known2 : 0f) + dealt;
            }
            else
            {
                DroneDamage += source is Drone ? amount : 0f;
                record.OtherDealt += dealt;
            }
        }

        // What a hit landed on: a boss, a flyer, a plated creature, or a light walker that is bunched (two or more others
        // within a shell's reach) or loose.
        private string RoleOf(Enemy enemy)
        {
            EnemyDefinition kind = enemy.Definition;
            if (kind.IsBoss)
            {
                return "boss";
            }

            if (kind.Flies)
            {
                return "air";
            }

            if (kind.HasArmor)
            {
                return "plated";
            }

            _targets.FindInRadius(enemy.Position, BunchRadius, Game.Core.TargetLayers.Ground, _near);
            return _near.Count - 1 >= BunchNeighbours ? "bunched" : "loose";
        }

        private void OnDrone(Drone drone) => drone.Delivered += value => Harvested += value;

        private static int Hash(string text)
        {
            int hash = 17;
            foreach (char c in text)
            {
                hash = unchecked(hash * 31 + c);
            }

            return hash & 0xFFFF;
        }
    }
}
