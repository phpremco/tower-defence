using System;
using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>What a drone is busy with, for the view to show.</summary>
    public enum DroneActivity
    {
        /// <summary>Hovering at the base with nothing to do.</summary>
        Idle,

        /// <summary>On its way somewhere.</summary>
        Flying,

        /// <summary>Hanging over a structure and welding it back together.</summary>
        Repairing,

        /// <summary>Drilling ore out of a resource node.</summary>
        Mining,

        /// <summary>Carrying a load back to the base.</summary>
        Hauling,

        /// <summary>Holding position over prey and shooting.</summary>
        Fighting,
    }

    /// <summary>
    /// A drone that works on its own: the repair drone takes repair jobs from the shared dispatcher, the harvest
    /// drone mines a resource node and carries the yield home, the attack drone hunts the enemy furthest along
    /// the road. Flying enemies can shoot it down, so it is also a structure. Has no knowledge of how it looks.
    /// </summary>
    public sealed class Drone : MonoBehaviour, IStructure
    {
        private const float Altitude = 2.2f;
        private const float WorkDistance = 0.5f;
        private const float RetargetSeconds = 0.5f;
        private const float NodeJitter = 6f;

        // The attack drone hunts anywhere on the field and keeps a little distance from its prey.
        private const float HuntRange = 1000f;
        private const float StandoffFraction = 0.7f;

        private DroneDefinition _definition;
        private DroneWorld _world;
        private Vector3 _home;
        private float _speed;
        private DroneModifiers _modifiers;
        private float _retarget;
        private IStructure _repairTarget;
        private int _nodeIndex = -1;
        private float _harvestRemaining;
        private int _carried;
        private ITargetable _prey;
        private float _cooldown;
        private Vector3 _lastPosition;

        public DroneDefinition Definition => _definition;

        public Health Health { get; private set; }

        public Vector3 Position => transform.position;

        public bool IsAlive => Health != null && !Health.IsDead;

        /// <summary>Drones never block the road.</summary>
        public bool BlocksGround => false;

        /// <summary>Drones are not repaired; the full bar keeps repair drones from trying to heal each other.</summary>
        public float HealthFraction => 1f;

        /// <summary>The point the drone works on right now, for the light beam. Null while it is idle or hunting.</summary>
        public Vector3? WorkPoint { get; private set; }

        /// <summary>True while a harvest drone carries a load home.</summary>
        public bool IsCarrying => _carried > 0;

        /// <summary>Where the drone is heading, so the view can turn it that way.</summary>
        public Vector3 Heading { get; private set; } = Vector3.forward;

        /// <summary>What the drone is doing this frame.</summary>
        public DroneActivity Activity { get; private set; }

        /// <summary>How fast the drone moved last frame, as a share of its top speed, so the view can tilt it.</summary>
        public float Pace { get; private set; }

        public event Action<Drone> Lost;

        /// <summary>Raised with the target position whenever the attack drone fires.</summary>
        public event Action<Vector3> Fired;

        /// <summary>Raised with the money when a harvest drone hands its load in at the base.</summary>
        public event Action<int> Delivered;

        public void Initialize(DroneDefinition definition, Vector3 start, DroneModifiers modifiers, DroneWorld world)
        {
            _definition = definition;
            _world = world;
            _home = new Vector3(world.Home.x, Altitude, world.Home.z);
            _modifiers = modifiers;
            _speed = definition.Speed * modifiers.SpeedMultiplier;
            Health = new Health(definition.MaxHealth * modifiers.HealthMultiplier);
            Health.Died += OnDied;
            transform.position = new Vector3(start.x, Altitude, start.z);
            _lastPosition = transform.position;
            _world.Structures.Add(this);
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

            switch (_definition.Kind)
            {
                case DroneKind.Repair:
                    UpdateRepair();
                    break;
                case DroneKind.Harvest:
                    UpdateHarvest();
                    break;
                default:
                    UpdateAttack();
                    break;
            }

            float step = _speed * Time.deltaTime;
            Pace = step > 0f ? Mathf.Clamp01(Vector3.Distance(transform.position, _lastPosition) / step) : 0f;
            _lastPosition = transform.position;
        }

        // The dispatcher hands out the work; the drone only asks again now and then, or at once when its job is done.
        private void UpdateRepair()
        {
            _retarget -= Time.deltaTime;
            bool done = _repairTarget != null && (!_repairTarget.IsAlive || _repairTarget.HealthFraction >= 1f);
            if (_repairTarget == null || done || _retarget <= 0f)
            {
                _retarget = RetargetSeconds;
                _repairTarget = _world.Repairs.Assign(this, transform.position, _speed, _repairTarget);
            }

            if (_repairTarget == null)
            {
                WorkPoint = null;
                Activity = FlyTo(_home) <= WorkDistance ? DroneActivity.Idle : DroneActivity.Flying;
                return;
            }

            if (FlyTo(Above(_repairTarget.Position)) <= WorkDistance)
            {
                _repairTarget.Repair(_definition.RepairPerSecond * _modifiers.OutputMultiplier * Time.deltaTime);
                WorkPoint = _repairTarget.Position;
                Activity = DroneActivity.Repairing;
            }
            else
            {
                WorkPoint = null;
                Activity = DroneActivity.Flying;
            }
        }

        // A full load goes home first; otherwise the drone works a resource node for a few seconds and carries the
        // yield to the base.
        private void UpdateHarvest()
        {
            if (_carried > 0)
            {
                WorkPoint = null;
                Activity = DroneActivity.Hauling;
                if (FlyTo(_home) <= WorkDistance)
                {
                    Delivered?.Invoke(_carried);
                    _carried = 0;
                }

                return;
            }

            if (_world.ResourceNodes == null || _world.ResourceNodes.Length == 0)
            {
                WorkPoint = null;
                Activity = FlyTo(_home) <= WorkDistance ? DroneActivity.Idle : DroneActivity.Flying;
                return;
            }

            Harvest();
        }

        private void Harvest()
        {
            if (_nodeIndex < 0)
            {
                _nodeIndex = NearestNode();
                _harvestRemaining = _definition.HarvestSeconds / _modifiers.TempoMultiplier;
                if (_nodeIndex < 0)
                {
                    // Every node is spent: the drone waits at home.
                    WorkPoint = null;
                    Activity = FlyTo(_home) <= WorkDistance ? DroneActivity.Idle : DroneActivity.Flying;
                    return;
                }
            }

            Vector3 node = _world.ResourceNodes[_nodeIndex];
            if (FlyTo(Above(node)) > WorkDistance)
            {
                WorkPoint = null;
                Activity = DroneActivity.Flying;
                return;
            }

            WorkPoint = node;
            Activity = DroneActivity.Mining;
            _harvestRemaining -= Time.deltaTime;
            if (_harvestRemaining <= 0f)
            {
                int take = Mathf.Min(Mathf.RoundToInt(_definition.HarvestValue * _modifiers.OutputMultiplier), Mathf.FloorToInt(_world.MineStocks[_nodeIndex]));
                _world.MineStocks[_nodeIndex] -= take;
                _carried = take;
                _nodeIndex = -1;
                WorkPoint = null;
            }
        }

        // The enemy furthest along the road is the most urgent one; the drone closes to firing range and hovers
        // there while it shoots, moving on when the target falls.
        private void UpdateAttack()
        {
            WorkPoint = null;
            _retarget -= Time.deltaTime;
            _cooldown -= Time.deltaTime;
            if (_prey == null || !_prey.IsAlive || _retarget <= 0f)
            {
                _retarget = RetargetSeconds;
                _prey = _world.Targets.FindBest(transform.position, HuntRange, _definition.Targets);
            }

            if (_prey == null)
            {
                Activity = FlyTo(_home) <= WorkDistance ? DroneActivity.Idle : DroneActivity.Flying;
                return;
            }

            Vector3 preyPosition = _prey.Position;
            float distance = FlatDistance(transform.position, preyPosition);
            if (distance > _definition.Range * StandoffFraction)
            {
                FlyTo(Above(preyPosition));
                Activity = DroneActivity.Flying;
            }
            else
            {
                Heading = Flat(preyPosition - transform.position);
                Activity = DroneActivity.Fighting;
            }

            if (distance <= _definition.Range && _cooldown <= 0f)
            {
                _cooldown = _definition.FireInterval / _modifiers.TempoMultiplier;
                _world.Projectiles.Fire(transform.position, _prey, _definition.Damage * _modifiers.OutputMultiplier, _definition.ProjectileSpeed, 0f, _definition.Targets, 1f, 0f, _definition.ProjectileStyle, _definition.Color);
                Fired?.Invoke(preyPosition);
            }
        }

        // The closest node that still holds ore, or -1 when the level is mined out.
        private int NearestNode()
        {
            Vector3[] nodes = _world.ResourceNodes;
            int best = -1;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (_world.MineStocks[i] <= 0f)
                {
                    continue;
                }

                Vector3 d = nodes[i] - _home;
                d.y = 0f;
                float sqr = d.sqrMagnitude + UnityEngine.Random.value * NodeJitter;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            return best;
        }

        // Moves towards a point and returns the distance that is left on the ground plane.
        private float FlyTo(Vector3 destination)
        {
            Vector3 toTarget = destination - transform.position;
            float distance = toTarget.magnitude;
            float step = _speed * Time.deltaTime;
            if (distance > 0.001f)
            {
                Heading = Flat(toTarget);
            }

            transform.position = distance <= step ? destination : transform.position + toTarget / distance * step;
            return FlatDistance(transform.position, destination);
        }

        private static Vector3 Above(Vector3 point) => new Vector3(point.x, Altitude, point.z);

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Heading;
        }

        private void OnDied()
        {
            if (_definition.Kind == DroneKind.Repair)
            {
                _world.Repairs.Release(this, _repairTarget);
            }

            _world.Structures.Remove(this);
            WorkPoint = null;
            Lost?.Invoke(this);
        }
    }
}
