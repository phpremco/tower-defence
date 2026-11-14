using Game.Combat;
using Game.Drones;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The repair crew logic: one drone per job, nearby work first, no thrashing, critical work still pulls.</summary>
    public sealed class RepairDispatcherTests
    {
        private sealed class FakeStructure : IStructure
        {
            private readonly float _max;
            private float _current;

            public FakeStructure(Vector3 position, float fraction, bool blocks = false, bool isBase = false)
            {
                Position = position;
                _max = 100f;
                _current = fraction * _max;
                BlocksGround = blocks;
                IsBase = isBase;
            }

            public Vector3 Position { get; }
            public bool IsAlive => _current > 0f;
            public bool BlocksGround { get; }
            public bool IsBase { get; }
            public float HealthFraction => _current / _max;
            public void TakeDamage(float amount) => _current = Mathf.Max(0f, _current - amount);
            public void Repair(float amount) => _current = Mathf.Min(_max, _current + amount);
        }

        private const float Speed = 4f;
        private readonly object _droneA = new object();
        private readonly object _droneB = new object();

        [Test]
        public void BaseIsRepairedOnlyWhenNoTowerNeedsWork()
        {
            var registry = new StructureRegistry();
            var baseStructure = new FakeStructure(Vector3.zero, 0.1f, true, true);
            var tower = new FakeStructure(new Vector3(20f, 0f, 0f), 0.9f);
            registry.Add(baseStructure);
            registry.Add(tower);
            var dispatcher = new RepairDispatcher(registry);

            IStructure first = dispatcher.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(tower, first, "a battered base still waits while any tower is damaged");

            tower.Repair(100f);
            IStructure next = dispatcher.Assign(_droneA, Vector3.zero, Speed, first);
            Assert.AreSame(baseStructure, next, "with every tower whole the base gets its turn");
        }

        [Test]
        public void TwoDronesNeverTakeTheSameJob()
        {
            var registry = new StructureRegistry();
            var worst = new FakeStructure(Vector3.zero, 0.2f);
            var other = new FakeStructure(new Vector3(3f, 0f, 0f), 0.5f);
            registry.Add(worst);
            registry.Add(other);
            var dispatcher = new RepairDispatcher(registry);

            IStructure first = dispatcher.Assign(_droneA, Vector3.zero, Speed, null);
            IStructure second = dispatcher.Assign(_droneB, Vector3.zero, Speed, null);

            Assert.AreSame(worst, first);
            Assert.AreSame(other, second);
        }

        [Test]
        public void NearbyWorkBeatsSlightlyWorseWorkFarAway()
        {
            var registry = new StructureRegistry();
            var near = new FakeStructure(new Vector3(1f, 0f, 0f), 0.6f);
            var far = new FakeStructure(new Vector3(30f, 0f, 0f), 0.5f);
            registry.Add(far);
            registry.Add(near);
            var dispatcher = new RepairDispatcher(registry);

            Assert.AreSame(near, dispatcher.Assign(_droneA, Vector3.zero, Speed, null));
        }

        [Test]
        public void ADroneFinishesItsJobBeforeMovingOn()
        {
            var registry = new StructureRegistry();
            var job = new FakeStructure(Vector3.zero, 0.6f);
            registry.Add(job);
            var dispatcher = new RepairDispatcher(registry);
            IStructure current = dispatcher.Assign(_droneA, Vector3.zero, Speed, null);

            // Another tower takes a bit more damage next door; the drone still finishes what it started.
            var newcomer = new FakeStructure(new Vector3(0.5f, 0f, 0f), 0.5f);
            registry.Add(newcomer);
            job.Repair(10f);

            Assert.AreSame(job, dispatcher.Assign(_droneA, Vector3.zero, Speed, current));
        }

        [Test]
        public void ACriticalGatePullsTheDroneOffLightWork()
        {
            var registry = new StructureRegistry();
            var job = new FakeStructure(Vector3.zero, 0.9f);
            registry.Add(job);
            var dispatcher = new RepairDispatcher(registry);
            IStructure current = dispatcher.Assign(_droneA, Vector3.zero, Speed, null);

            var gate = new FakeStructure(new Vector3(2f, 0f, 0f), 0.1f, true);
            registry.Add(gate);

            Assert.AreSame(gate, dispatcher.Assign(_droneA, Vector3.zero, Speed, current));
            Assert.IsFalse(dispatcher.IsClaimed(job), "the abandoned job is free for another drone");
        }

        [Test]
        public void AFinishedJobIsReleasedAndTheNextOneTaken()
        {
            var registry = new StructureRegistry();
            var job = new FakeStructure(Vector3.zero, 0.5f);
            var next = new FakeStructure(new Vector3(2f, 0f, 0f), 0.7f);
            registry.Add(job);
            registry.Add(next);
            var dispatcher = new RepairDispatcher(registry);
            IStructure current = dispatcher.Assign(_droneA, Vector3.zero, Speed, null);
            job.Repair(100f);

            Assert.AreSame(next, dispatcher.Assign(_droneA, Vector3.zero, Speed, current));
            Assert.IsFalse(dispatcher.IsClaimed(job));
            Assert.IsNull(dispatcher.Assign(_droneB, Vector3.zero, Speed, null), "nothing is left for a second drone");
        }

        [Test]
        public void AJobThatLeftTheField_IsGivenBack_AndNeverHandedOutAgain()
        {
            // Round 62: a damaged tower sold while a drone repaired it stayed that drone's job for ever (it still reads as alive
            // and damaged), and the drone flew to a destroyed object every frame.
            var registry = new StructureRegistry();
            var sold = new FakeStructure(Vector3.zero, 0.5f);
            var other = new FakeStructure(new Vector3(6f, 0f, 0f), 0.8f);
            registry.Add(sold);
            registry.Add(other);
            var dispatcher = new RepairDispatcher(registry);

            IStructure job = dispatcher.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(sold, job);

            registry.Remove(sold);
            IStructure next = dispatcher.Assign(_droneA, Vector3.zero, Speed, job);
            Assert.AreSame(other, next, "the sold tower is dropped and the next damaged structure taken");
            Assert.IsFalse(dispatcher.IsClaimed(sold), "the claim on the sold tower is released");

            registry.Remove(other);
            Assert.IsNull(dispatcher.Assign(_droneA, Vector3.zero, Speed, next), "nothing on the field, nothing to repair");
        }

        [Test]
        public void ALostDroneGivesItsJobBack()
        {
            var registry = new StructureRegistry();
            var job = new FakeStructure(Vector3.zero, 0.5f);
            registry.Add(job);
            var dispatcher = new RepairDispatcher(registry);
            IStructure current = dispatcher.Assign(_droneA, Vector3.zero, Speed, null);

            dispatcher.Release(_droneA, current);

            Assert.AreSame(job, dispatcher.Assign(_droneB, Vector3.zero, Speed, null));
        }
    }
}
