using Game.Core;
using Game.Drones;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Launches drones. Every level can use them: the player pays energy, which gathers over time, to send a
    /// repair or collector drone from the base. Permanent upgrades may add free drones at the start. Once flying,
    /// drones work on their own; lost drones can be bought again.
    /// </summary>
    public sealed class DroneController : MonoBehaviour
    {
        private const float SpawnSpread = 0.6f;

        private LevelContext _context;
        private Transform _parent;
        private Vector3 _home;
        private float _speed;
        private int _launched;
        private Vector3[] _mineNodes;

        public void Initialize(LevelContext context, Transform parent)
        {
            _context = context;
            _parent = parent;

            PlayerModifiers modifiers = context.GlobalModifiers;
            _speed = modifiers.DroneSpeedMultiplier;

            var pickups = new GameObject("Pickups").AddComponent<PickupField>();
            pickups.transform.SetParent(parent, false);
            context.Pickups = pickups;

            Vector2Int[] road = context.Level.MainRoad;
            _home = context.Level.Grid.CellToWorld(road[road.Length - 1]);

            Vector2Int[] mines = context.Level.MineCells;
            _mineNodes = new Vector3[mines.Length];
            for (int i = 0; i < mines.Length; i++)
            {
                _mineNodes[i] = context.Level.Grid.CellToWorld(mines[i]);
            }

            for (int i = 0; i < modifiers.RepairDroneCount; i++)
            {
                Launch(context.Level.RepairDrone);
            }

            for (int i = 0; i < modifiers.CollectorDroneCount; i++)
            {
                Launch(context.Level.CollectorDrone);
            }

            context.DroneRequested.Subscribe(OnRequested);
        }

        private void OnDestroy()
        {
            if (_context != null)
            {
                _context.DroneRequested.Unsubscribe(OnRequested);
            }
        }

        // Energy gathers in game time, so it follows pause and fast forward.
        private void Update()
        {
            if (_context != null && _context.Energy != null)
            {
                _context.Energy.Tick(Time.deltaTime);
            }
        }

        private void OnRequested(DroneDefinition definition)
        {
            if (definition == null || _context.ActiveDrones(definition) >= definition.MaxActive)
            {
                return;
            }

            if (_context.Energy.TrySpend(definition.EnergyCost))
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
            drone.Initialize(definition, _home + new Vector3(offset, 0f, 0f), _speed, _context.Structures, _context.Pickups, _mineNodes, OnCollected);
            go.AddComponent<DroneView>().Initialize(drone);
            drone.Lost += OnLost;

            _context.SetActiveDrones(definition, _context.ActiveDrones(definition) + 1);
            _context.DronesChanged.Raise();
        }

        private void OnCollected(int value)
        {
            _context.Wallet.Add(value);
        }

        private void OnLost(Drone drone)
        {
            drone.Lost -= OnLost;
            DroneDefinition definition = drone.Definition;
            _context.SetActiveDrones(definition, Mathf.Max(0, _context.ActiveDrones(definition) - 1));
            Destroy(drone.gameObject);
            _context.DronesChanged.Raise();
        }
    }
}
