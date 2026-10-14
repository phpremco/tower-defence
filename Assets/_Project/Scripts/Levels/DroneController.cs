using Game.Core;
using Game.Drones;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Sends the drones the player owns into the level. How many repair and collector drones there are comes from
    /// permanent upgrades; the drones then work on their own. Lost drones stay lost for the rest of the level.
    /// </summary>
    public sealed class DroneController : MonoBehaviour
    {
        private const float SpawnSpread = 0.6f;

        private LevelContext _context;
        private Transform _parent;

        public void Initialize(LevelContext context, Transform parent)
        {
            _context = context;
            _parent = parent;

            PlayerModifiers modifiers = context.GlobalModifiers;
            var pickups = new GameObject("Pickups").AddComponent<PickupField>();
            pickups.transform.SetParent(parent, false);
            context.Pickups = pickups;

            Vector2Int[] road = context.Level.MainRoad;
            Vector3 home = context.Level.Grid.CellToWorld(road[road.Length - 1]);
            Spawn(context.Level.RepairDrone, modifiers.RepairDroneCount, home, modifiers.DroneSpeedMultiplier);
            Spawn(context.Level.CollectorDrone, modifiers.CollectorDroneCount, home, modifiers.DroneSpeedMultiplier);
        }

        private void Spawn(DroneDefinition definition, int count, Vector3 home, float speed)
        {
            if (definition == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Drone_{definition.DisplayName}");
                go.transform.SetParent(_parent, false);
                var drone = go.AddComponent<Drone>();
                Vector3 offset = new Vector3((i - (count - 1) * 0.5f) * SpawnSpread, 0f, 0f);
                drone.Initialize(definition, home + offset, speed, _context.Structures, _context.Pickups, OnCollected);
                go.AddComponent<DroneView>().Initialize(drone);
                drone.Lost += OnLost;
            }
        }

        private void OnCollected(int value)
        {
            _context.Wallet.Add(value);
        }

        private static void OnLost(Drone drone)
        {
            drone.Lost -= OnLost;
            Destroy(drone.gameObject);
        }
    }
}
