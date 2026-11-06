using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Launches drones. Every level can use them: the player pays money to send a repair, harvest or attack drone
    /// from the base. No drone comes free. Once flying, drones work on their own; lost drones can be bought again.
    /// </summary>
    public sealed class DroneController : MonoBehaviour
    {
        private const float SpawnSpread = 0.6f;
        private const float HalfStockShare = 0.5f;

        private LevelContext _context;
        private Transform _parent;
        private Vector3 _home;
        private Vector3[] _docks;
        private Vector3[] _idleSpots;
        private int _launched;
        private Vector3[] _mineNodes;
        private float[] _mineStocks;
        private bool[] _mineEmpty;
        private bool[] _mineHalved;
        private RepairDispatcher _repairs;
        private readonly List<Drone> _drones = new List<Drone>();

        /// <summary>Every drone in the air right now, for audio and effects that follow them.</summary>
        public IReadOnlyList<Drone> Drones => _drones;

        public void Initialize(LevelContext context, Transform parent)
        {
            _context = context;
            _parent = parent;

            Vector2Int[] road = context.Level.MainRoad;
            _home = context.Level.Road.CentreOf(road[road.Length - 1]);
            BaseArtDefinition art = context.Level.Theme != null ? context.Level.Theme.BaseArt : null;
            _docks = BasePoints(context.Level, art != null ? art.DockPoints : null);
            _idleSpots = BasePoints(context.Level, art != null ? art.IdlePoints : null);

            Vector2Int[] mines = context.Level.MineCells;
            _mineNodes = new Vector3[mines.Length];
            _mineStocks = new float[mines.Length];
            _mineEmpty = new bool[mines.Length];
            _mineHalved = new bool[mines.Length];
            for (int i = 0; i < mines.Length; i++)
            {
                _mineNodes[i] = context.Level.Grid.CellToWorld(mines[i]);
                _mineStocks[i] = context.Level.MineStock;
            }

            _repairs = new RepairDispatcher(context.Structures);
            context.DroneRequested.Subscribe(OnRequested);
        }

        private void OnDestroy()
        {
            if (_context != null)
            {
                _context.DroneRequested.Unsubscribe(OnRequested);
            }
        }

        private void OnRequested(DroneDefinition definition)
        {
            // A dialogue or the pause menu owns confirm, so a focused drone card must not buy behind it.
            if (definition == null || _context.InputBlocked || _context.ActiveDrones(definition) >= _context.MaxDrones(definition))
            {
                return;
            }

            if (_context.Wallet.TrySpend(definition.Cost))
            {
                Launch(definition);
            }
        }

        private void Launch(DroneDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            var go = new GameObject($"Drone_{definition.DisplayName}");
            go.transform.SetParent(_parent, false);
            var drone = go.AddComponent<Drone>();

            // The first drones each live in a bay of the base and start from it; the next ones hover over the base deck at a
            // spot of their own (the base art's idle points), and only past those do they wait side by side at the road end.
            Vector3 home = HomeFor(_launched, out float offset);
            _launched++;
            var world = new DroneWorld(home, _context.Structures, _repairs, _context.Targets, _context.Projectiles, _mineNodes, _mineStocks);
            drone.Initialize(definition, home + new Vector3(offset, 0f, 0f), _context.ModifiersFor(definition), world);
            go.AddComponent<DroneView>().Initialize(drone);
            drone.Downed += OnDowned;
            drone.Delivered += OnCollected;
            _drones.Add(drone);
            _context.DroneLaunched.Raise(drone);

            _context.SetActiveDrones(definition, _context.ActiveDrones(definition) + 1);
            _context.DronesChanged.Raise();
        }

        // A bay, else an idle spot over the deck, else the road end with a sideways start offset so they do not stack.
        private Vector3 HomeFor(int index, out float offset)
        {
            offset = 0f;
            if (index < _docks.Length)
            {
                return _docks[index];
            }

            index -= _docks.Length;
            if (index < _idleSpots.Length)
            {
                return _idleSpots[index];
            }

            offset = ((index % 4) - 1.5f) * SpawnSpread;
            return _home;
        }

        // Points of the imported base (its bays or idle spots) on the field, or none (the procedural outpost has neither).
        private static Vector3[] BasePoints(LevelDefinition level, Vector3[] local)
        {
            if (local == null || level.MainRoad.Length == 0)
            {
                return new Vector3[0];
            }

            Vector3 centre = LevelVisualBuilder.BasePosition(level);
            Quaternion facing = LevelVisualBuilder.BaseFacing(level);
            var points = new Vector3[local.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = BaseArtDefinition.ToWorld(local[i], centre, facing);
            }

            return points;
        }

        // A delivery is when a node may have run dry or half dry, so that is when the field is told about it.
        private void OnCollected(int value)
        {
            _context.Wallet.Earn(value);
            float half = _context.Level.MineStock * HalfStockShare;
            for (int i = 0; i < _mineStocks.Length; i++)
            {
                if (!_mineHalved[i] && _mineStocks[i] <= half)
                {
                    _mineHalved[i] = true;
                    _context.MineHalved.Raise(i);
                }

                if (!_mineEmpty[i] && _mineStocks[i] <= 0f)
                {
                    _mineEmpty[i] = true;
                    _context.MineDepleted.Raise(i);
                }
            }
        }

        // A drone that is shot down is not gone: it charges at home and comes back, so the HUD only hears about it.
        private void OnDowned(Drone drone) => _context.DronesChanged.Raise();
    }
}
