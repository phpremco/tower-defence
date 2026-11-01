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

        private LevelContext _context;
        private Transform _parent;
        private Vector3 _home;
        private int _launched;
        private Vector3[] _mineNodes;
        private float[] _mineStocks;
        private bool[] _mineEmpty;
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

            Vector2Int[] mines = context.Level.MineCells;
            _mineNodes = new Vector3[mines.Length];
            _mineStocks = new float[mines.Length];
            _mineEmpty = new bool[mines.Length];
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
            if (definition == null || _context.ActiveDrones(definition) >= _context.MaxDrones(definition))
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

            // Newly launched drones start side by side so they do not sit on top of each other.
            float offset = ((_launched % 4) - 1.5f) * SpawnSpread;
            _launched++;
            var world = new DroneWorld(_home, _context.Structures, _repairs, _context.Targets, _context.Projectiles, _mineNodes, _mineStocks);
            drone.Initialize(definition, _home + new Vector3(offset, 0f, 0f), _context.ModifiersFor(definition), world);
            go.AddComponent<DroneView>().Initialize(drone);
            drone.Downed += OnDowned;
            drone.Delivered += OnCollected;
            _drones.Add(drone);

            _context.SetActiveDrones(definition, _context.ActiveDrones(definition) + 1);
            _context.DronesChanged.Raise();
        }

        // A delivery is when a node may have run dry, so that is when the field is told about it.
        private void OnCollected(int value)
        {
            _context.Wallet.Add(value);
            for (int i = 0; i < _mineStocks.Length; i++)
            {
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
