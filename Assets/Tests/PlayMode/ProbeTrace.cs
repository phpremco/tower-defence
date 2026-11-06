using System.Collections.Generic;
using System.Text;
using Game.Enemies;
using Game.Levels;
using Game.Towers;

namespace Game.Tests
{
    /// <summary>
    /// What the balance bot really had at the start of every wave: money earned so far (start plus kills), money still
    /// unspent, its towers with their levels and raw damage per second, the base and the creatures each wave brought.
    /// The probe prints it beside the ledger's line for the same wave, so a gap between model and game shows as numbers.
    /// </summary>
    public sealed class ProbeTrace
    {
        private readonly LevelContext _context;
        private readonly int _startingMoney;
        private readonly List<string> _lines = new List<string>();
        private readonly Dictionary<int, float> _spawnedHealth = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _spawned = new Dictionary<int, int>();
        private const float BaseHitDistance = 3f;
        private readonly SortedDictionary<string, int> _baseHits = new SortedDictionary<string, int>();
        private readonly Dictionary<Enemy, System.Action<UnityEngine.Vector3>> _attackHandlers = new Dictionary<Enemy, System.Action<UnityEngine.Vector3>>();
        private int _kills;
        private int _killed;
        private int _spawnedTotal;
        private int _frames;
        private int _shots;
        private readonly HashSet<Tower> _hooked = new HashSet<Tower>();
        private float _frameTime;
        private readonly List<Enemy> _living = new List<Enemy>();
        private int _recordedWave;

        public ProbeTrace(LevelContext context)
        {
            _context = context;
            _startingMoney = context.Wallet.Money;
            context.EnemySpawned.Subscribe(OnSpawned);
            context.EnemyKilled.Subscribe(OnKilled);
        }

        public IReadOnlyList<string> Lines => _lines;

        /// <summary>Money the bot has had in all: the start plus every kill so far.</summary>
        public int Earned => _startingMoney + _kills;

        /// <summary>Hits flyers landed on the base: the air defence did not stop them on the way.</summary>
        public int FlyerBaseHits { get; private set; }

        /// <summary>Flyers that came, and those the defence shot down.</summary>
        public int FlyersSpawned { get; private set; }

        public int FlyersKilled { get; private set; }

        public void Detach()
        {
            _context.EnemySpawned.Unsubscribe(OnSpawned);
            _context.EnemyKilled.Unsubscribe(OnKilled);
            foreach (KeyValuePair<Enemy, System.Action<UnityEngine.Vector3>> pair in _attackHandlers)
            {
                pair.Key.Attacked -= pair.Value;
            }

            _attackHandlers.Clear();
        }

        /// <summary>Called every frame; writes one line when a new wave has just started.</summary>
        public void Sample(List<Tower> towers, string ledgerLine, int repairSpent = 0)
        {
            foreach (Tower tower in towers)
            {
                if (_hooked.Add(tower))
                {
                    tower.Fired += _ => _shots++;
                }
            }

            _frames++;
            _frameTime += UnityEngine.Time.deltaTime;
            if (_context.WaveNumber == _recordedWave)
            {
                return;
            }

            // The line for the wave before: what it brought, now that it has fully spawned or been overtaken.
            _recordedWave = _context.WaveNumber;
            var line = new StringBuilder();
            line.Append($"w{_recordedWave,-2} earned {Earned,5} unspent {_context.Wallet.Money,4} base {_context.BaseHealth.Current,4:0}");
            line.Append($" repairs {repairSpent,4}");
            line.Append($" hero {(_context.Hero == null ? "none" : _context.Hero.IsDown ? "down" : "up")}");
            line.Append($" alive {_spawnedTotal - _killed,3}");
            line.Append($" frame {(_frames > 0 ? _frameTime / _frames : 0f):0.000}s");
            line.Append($" shots {_shots}");
            line.Append($" air {FlyersKilled}/{FlyersSpawned} basehits {FlyerBaseHits}");
            foreach (KeyValuePair<string, int> pair in _baseHits)
            {
                line.Append($" {pair.Key}x{pair.Value}");
            }

            _baseHits.Clear();
            _shots = 0;
            _frames = 0;
            _frameTime = 0f;
            line.Append(" where ").Append(Where());
            line.Append($" inRange {InRange(towers)}");
            line.Append(" centre ").Append(_centre);
            line.Append($" dps {Dps(towers),5:0} towers {Describe(towers)}");
            int previous = _recordedWave - 1;
            if (_spawned.TryGetValue(previous, out int count))
            {
                line.Append($" | w{previous} brought {count} creatures, {_spawnedHealth[previous]:0} hp");
            }

            if (!string.IsNullOrEmpty(ledgerLine))
            {
                line.Append(" | ledger ").Append(ledgerLine);
            }

            _lines.Add(line.ToString());
        }

        /// <summary>Where the gates and the bot's towers sit along the main road, as a share of its length (0 = spawn, 1 = base).</summary>
        public static string Layout(LevelDefinition level, List<Tower> towers)
        {
            UnityEngine.Vector2Int[] road = Walk(level.MainRoad);
            var text = new StringBuilder($"layout: main road {road.Length} cells; gates");
            foreach (BlockadeSpot gate in level.Blockades)
            {
                text.Append($" {Along(road, gate.Cell):0.00}");
            }

            text.Append("; towers");
            foreach (Tower tower in towers)
            {
                text.Append($" {tower.Definition.Id.Substring(0, 2)}@{Along(road, level.Grid.WorldToCell(tower.Position)):0.00}");
            }

            return text.ToString();
        }

        // Every cell between the waypoints, in walking order.
        private static UnityEngine.Vector2Int[] Walk(UnityEngine.Vector2Int[] waypoints)
        {
            var cells = new List<UnityEngine.Vector2Int>();
            for (int i = 0; i + 1 < waypoints.Length; i++)
            {
                UnityEngine.Vector2Int from = waypoints[i];
                UnityEngine.Vector2Int to = waypoints[i + 1];
                int steps = System.Math.Max(System.Math.Abs(to.x - from.x), System.Math.Abs(to.y - from.y));
                for (int s = 0; s < steps; s++)
                {
                    cells.Add(new UnityEngine.Vector2Int(from.x + (to.x - from.x) * s / steps, from.y + (to.y - from.y) * s / steps));
                }
            }

            if (waypoints.Length > 0)
            {
                cells.Add(waypoints[waypoints.Length - 1]);
            }

            return cells.ToArray();
        }

        private static float Along(UnityEngine.Vector2Int[] road, UnityEngine.Vector2Int cell)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < road.Length; i++)
            {
                float distance = UnityEngine.Vector2Int.Distance(road[i], cell);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return road.Length > 1 ? best / (float)(road.Length - 1) : 0f;
        }

        // Pooled creatures are reused, so a creature's attack handler is replaced on every spawn and removed on its death.
        private void Hook(Enemy enemy)
        {
            Unhook(enemy);
            bool flies = enemy.Definition.Flies;
            FlyersSpawned += flies ? 1 : 0;
            string name = enemy.Definition.name;
            System.Action<UnityEngine.Vector3> onAttacked = position =>
            {
                if (_context.Base != null && UnityEngine.Vector3.Distance(position, _context.Base.Position) < BaseHitDistance)
                {
                    FlyerBaseHits += flies ? 1 : 0;
                    _baseHits[name] = (_baseHits.TryGetValue(name, out int hits) ? hits : 0) + 1;
                }
            };
            enemy.Attacked += onAttacked;
            _attackHandlers[enemy] = onAttacked;
        }

        private void Unhook(Enemy enemy)
        {
            if (_attackHandlers.TryGetValue(enemy, out System.Action<UnityEngine.Vector3> handler))
            {
                enemy.Attacked -= handler;
                _attackHandlers.Remove(enemy);
            }
        }

        private void OnSpawned(Enemy enemy)
        {
            int wave = _context.WaveNumber;
            _spawnedTotal++;
            _living.Add(enemy);
            Hook(enemy);
            _spawned[wave] = (_spawned.TryGetValue(wave, out int count) ? count : 0) + 1;
            _spawnedHealth[wave] = (_spawnedHealth.TryGetValue(wave, out float health) ? health : 0f) + enemy.Health.Max;
        }

        private void OnKilled(Enemy enemy)
        {
            if (_attackHandlers.ContainsKey(enemy) && enemy.Definition.Flies)
            {
                FlyersKilled++;
            }

            Unhook(enemy);
            _kills += enemy.Reward;
            _killed++;
        }

        private int InRange(List<Tower> towers)
        {
            int count = 0;
            foreach (Enemy enemy in _living)
            {
                if (towers.Exists(t => UnityEngine.Vector3.Distance(t.Position, enemy.Position) <= t.Range))
                {
                    count++;
                }
            }

            UnityEngine.Vector3 sum = UnityEngine.Vector3.zero;
            foreach (Enemy enemy in _living)
            {
                sum += enemy.Position;
            }

            UnityEngine.Vector3 centre = _living.Count > 0 ? sum / _living.Count : sum;
            _centre = $"({centre.x:0},{centre.z:0}) hero {(_context.Hero != null ? _context.Hero.transform.position.ToString("0") : "-")} gate0 {(_context.Level.Blockades.Length > 0 ? _context.Level.Grid.CellToWorld(_context.Level.Blockades[0].Cell).ToString("0") : "-")} tower0 {(towers.Count > 0 ? towers[0].Position.ToString("0") : "-")}";
            return count;
        }

        private string _centre = string.Empty;

        // The living creatures by origin and how far they have walked, to see where a queue forms.
        private string Where()
        {
            _living.RemoveAll(e => e == null || !e.IsAlive);
            var groups = new SortedDictionary<string, (int Count, float Walked)>();
            foreach (Enemy enemy in _living)
            {
                string key = enemy.Origin.ToString();
                groups.TryGetValue(key, out var group);
                groups[key] = (group.Count + 1, group.Walked + enemy.PathProgress);
            }

            var text = new StringBuilder();
            foreach (var pair in groups)
            {
                text.Append($"{pair.Key}:{pair.Value.Count}@{pair.Value.Walked / pair.Value.Count:0} ");
            }

            return text.ToString();
        }

        private static float Dps(List<Tower> towers)
        {
            float sum = 0f;
            foreach (Tower tower in towers)
            {
                TowerLevel level = tower.Definition.GetLevel(tower.LevelIndex);
                if (tower.IsAlive && level.FireInterval > 0f)
                {
                    sum += level.Damage / level.FireInterval * tower.Power;
                }
            }

            return sum;
        }

        private static string Describe(List<Tower> towers)
        {
            var text = new StringBuilder();
            foreach (Tower tower in towers)
            {
                text.Append(tower.Definition.Id.Substring(0, 2)).Append(tower.LevelIndex + 1).Append(tower.IsWrecked ? "x" : string.Empty).Append(' ');
            }

            return text.ToString().TrimEnd();
        }
    }
}
