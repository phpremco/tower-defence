using System;
using Game.Combat;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// A drone that works on its own: a repair drone heals the most damaged structure, a collector brings in loot.
    /// Flying enemies can shoot it down, so it is also a structure. Has no knowledge of how it looks.
    /// </summary>
    public sealed class Drone : MonoBehaviour, IStructure
    {
        private const float Altitude = 2.2f;
        private const float WorkDistance = 0.5f;
        private const float RetargetSeconds = 0.5f;

        private DroneDefinition _definition;
        private StructureRegistry _structures;
        private PickupField _pickups;
        private Action<int> _onCollected;
        private Vector3 _home;
        private float _speed;
        private float _retarget;
        private IStructure _repairTarget;
        private bool _hasLootTarget;
        private Vector3 _lootTarget;

        public DroneDefinition Definition => _definition;

        public Health Health { get; private set; }

        public Vector3 Position => transform.position;

        public bool IsAlive => Health != null && !Health.IsDead;

        /// <summary>Drones never block the road.</summary>
        public bool BlocksGround => false;

        /// <summary>Drones are not repaired; the full bar keeps repair drones from trying to heal each other.</summary>
        public float HealthFraction => 1f;

        /// <summary>The point the drone works on right now, for the light beam. Null while it is idle.</summary>
        public Vector3? WorkPoint { get; private set; }

        public event Action<Drone> Lost;

        public void Initialize(DroneDefinition definition, Vector3 home, float speedMultiplier, StructureRegistry structures, PickupField pickups, Action<int> onCollected)
        {
            _definition = definition;
            _home = new Vector3(home.x, Altitude, home.z);
            _speed = definition.Speed * speedMultiplier;
            _structures = structures;
            _pickups = pickups;
            _onCollected = onCollected;
            Health = new Health(definition.MaxHealth);
            Health.Died += OnDied;
            transform.position = _home;
            _structures.Add(this);
        }

        public void TakeDamage(float amount) => Health.TakeDamage(amount);

        public void Repair(float amount)
        {
        }

        private void OnDestroy()
        {
            if (Health != null)
            {
                Health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            if (_definition.Kind == DroneKind.Repair)
            {
                UpdateRepair();
            }
            else
            {
                UpdateCollector();
            }
        }

        private void UpdateRepair()
        {
            _retarget -= Time.deltaTime;
            if (_repairTarget == null || !_repairTarget.IsAlive || _repairTarget.HealthFraction >= 1f || _retarget <= 0f)
            {
                _retarget = RetargetSeconds;
                _repairTarget = _structures.FindMostDamaged();
            }

            if (_repairTarget == null)
            {
                WorkPoint = null;
                FlyTo(_home);
                return;
            }

            Vector3 above = new Vector3(_repairTarget.Position.x, Altitude, _repairTarget.Position.z);
            if (FlyTo(above) <= WorkDistance)
            {
                _repairTarget.Repair(_definition.RepairPerSecond * Time.deltaTime);
                WorkPoint = _repairTarget.Position;
            }
            else
            {
                WorkPoint = null;
            }
        }

        private void UpdateCollector()
        {
            if (!_hasLootTarget)
            {
                _hasLootTarget = _pickups.TryFindNearest(transform.position, out _lootTarget);
            }

            if (!_hasLootTarget)
            {
                WorkPoint = null;
                FlyTo(_home);
                return;
            }

            if (FlyTo(new Vector3(_lootTarget.x, Altitude, _lootTarget.z)) <= WorkDistance)
            {
                int value = _pickups.Collect(_lootTarget);
                if (value > 0)
                {
                    _onCollected(value);
                }

                _hasLootTarget = false;
            }

            WorkPoint = _hasLootTarget ? (Vector3?)_lootTarget : null;
        }

        // Moves towards a point and returns the distance that is left on the ground plane.
        private float FlyTo(Vector3 destination)
        {
            Vector3 toTarget = destination - transform.position;
            float distance = toTarget.magnitude;
            float step = _speed * Time.deltaTime;
            transform.position = distance <= step ? destination : transform.position + toTarget / distance * step;

            Vector3 left = destination - transform.position;
            left.y = 0f;
            return left.magnitude;
        }

        private void OnDied()
        {
            _structures.Remove(this);
            WorkPoint = null;
            Lost?.Invoke(this);
        }
    }
}
