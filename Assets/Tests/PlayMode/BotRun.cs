using System.Collections.Generic;
using Game.Drones;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// What one probe bot run keeps track of (round 20): its strategy and seeded draws, what it built, sold and placed, the
    /// drones it bought and what they brought in, and per tower type the damage really dealt against the data's DPS over the
    /// seconds the towers stood while creatures were on the field (the in-game DPS of BalanceProbeTests and the monoculture probe).
    /// </summary>
    internal sealed class BotRun
    {
        // A tower this hurt counts as worn: the creative bot's cue for a repair drone.
        private const float WornBelow = 0.9f;
        private const int SeedSpread = 7919;

        private readonly System.Random _random;
        private readonly HashSet<Enemy> _watched = new HashSet<Enemy>();
        private readonly List<Game.Combat.ITargetable> _near = new List<Game.Combat.ITargetable>();
        private Game.Combat.TargetRegistry _targets;

        public BotRun(ProbeStrategy strategy, int seed)
        {
            Strategy = strategy;
            _random = new System.Random(unchecked(seed * SeedSpread + strategy.Name.Length));
        }

        public ProbeStrategy Strategy { get; }

        public int Built;
        public int Upgrades;
        public int Sold;
        public int Mines;
        public int Fields;
        public int Harvested;
        public float DroneDamage;
        public float Seconds;
        public bool WearSeen;
        public int MinesWave = -1;
        public int MinesThisWave;
        public int FieldWave = -1;
        public int BurrowMined;
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
        /// Round 21a3, where a tower type's data DPS goes in play: seconds standing with creatures on the field, seconds with
        /// something it can hit in reach, the tower power (wear) over those seconds, shots fired, aimed hits that landed, and
        /// of the damage the hits carried: lost to plating, and wasted past the creature's last hit point (overkill).
        /// </summary>
        public readonly Dictionary<string, TowerUse> Use = new Dictionary<string, TowerUse>();

        internal sealed class TowerUse
        {
            public float Seconds;
            public float Busy;
            public float PowerSeconds;
            public float DataDamage;
            public int Shots;
            public int Hits;
            public float Carried;
            public float Plate;
            public float Overkill;
            public float Dealt;
        }

        private readonly Dictionary<Enemy, float> _lastHealth = new Dictionary<Enemy, float>();
        private readonly HashSet<Tower> _counted = new HashSet<Tower>();

        private TowerUse UseOf(string id)
        {
            if (!Use.TryGetValue(id, out TowerUse use))
            {
                Use[id] = use = new TowerUse();
            }

            return use;
        }

        /// <summary>The roles a hit is sorted into, in report order.</summary>
        public static readonly string[] Roles = { "loose", "bunched", "plated", "air", "boss" };

        // Light creatures with at least this many others this close (cells) count as bunched: a shell catches them together.
        private const float BunchRadius = 1.2f;
        private const int BunchNeighbours = 2;

        // A reach that covers every map: "is anything flying at all".
        private const float AnywhereRange = 1000f;

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

        public void Sample(LevelContext context, List<Tower> towers, float deltaTime)
        {
            Seconds += deltaTime;
            foreach (Tower tower in towers)
            {
                WearSeen |= tower.HealthFraction < WornBelow;
            }

            if (context.Targets.Count == 0)
            {
                return;
            }

            bool airPresent = _targets.FindBest(Vector3.zero, AnywhereRange, Game.Core.TargetLayers.Air) != null;
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
                // A sky gun is only judged while something flies: the ledger counts it on the air waves alone.
                if ((tower.Definition.Targets & Game.Core.TargetLayers.Ground) == 0 && !airPresent)
                {
                    continue;
                }

                TowerUse use = UseOf(tower.Definition.Id);
                use.Seconds += deltaTime;
                use.DataDamage += dps * deltaTime;
                use.PowerSeconds += tower.Power * deltaTime;
                if (_targets.FindBest(tower.Position, tower.Range, tower.Definition.Targets) != null)
                {
                    use.Busy += deltaTime;
                }

                if (_counted.Add(tower))
                {
                    string id = tower.Definition.Id;
                    tower.Fired += _ => UseOf(id).Shots++;
                }
            }
        }

        private void OnSpawned(Enemy enemy)
        {
            // Pooled creatures come back: one subscription each.
            if (_watched.Add(enemy))
            {
                enemy.DamagedBy += OnDamaged;
            }

            _lastHealth[enemy] = enemy.Health != null ? enemy.Health.Current : 0f;
        }

        private void OnDamaged(Enemy enemy, object source, float amount)
        {
            float before = _lastHealth.TryGetValue(enemy, out float last) ? last : enemy.Health.Max;
            float dealt = Mathf.Max(0f, before - enemy.Health.Current);
            _lastHealth[enemy] = enemy.Health.Current;
            if (source is Tower tower)
            {
                (float damage, float seconds, float data) = Fire.TryGetValue(tower.Definition.Id, out var known) ? known : (0f, 0f, 0f);
                Fire[tower.Definition.Id] = (damage + amount, seconds, data);
                TowerUse use = UseOf(tower.Definition.Id);
                float through = amount * enemy.ShareThrough(amount);
                use.Hits++;
                use.Carried += amount;
                use.Plate += amount - through;
                use.Overkill += enemy.Health.IsDead ? Mathf.Max(0f, through - dealt) : 0f;
                use.Dealt += dealt;
                // The role numbers count what the hit really took off the creature: a shell on one small ant counts its last hit points.
                var key = (tower.Definition.Id, RoleOf(enemy));
                RoleDamage[key] = (RoleDamage.TryGetValue(key, out float sum) ? sum : 0f) + dealt;
            }
            else if (source is Drone)
            {
                DroneDamage += amount;
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

            if (kind.HasPlating)
            {
                return "plated";
            }

            _targets.FindInRadius(enemy.Position, BunchRadius, Game.Core.TargetLayers.Ground, _near);
            return _near.Count - 1 >= BunchNeighbours ? "bunched" : "loose";
        }

        private void OnDrone(Drone drone) => drone.Delivered += value => Harvested += value;
    }
}
