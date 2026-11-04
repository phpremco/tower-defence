using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The spawn origins' cycle follows the wave data: marked when used, opening ahead of the first creature, then open for good.</summary>
    public sealed class SpawnOriginTests
    {
        private const float Lead = 2.5f;
        private const float Burst = 1.8f;
        private const float Emerge = 2.53f;
        private const float Open = 3.2f;
        private const float Step = 0.05f;
        private const float NestDelay = 10f;
        private const float NestInterval = 2f;
        private const int NestCount = 3;

        private static readonly OriginTiming Timing = new OriginTiming(Lead, Burst, Emerge, Open);

        private EnemyDefinition _stone;
        private EnemyDefinition _walker;
        private DifficultyProfile _difficulty;
        private ElementNest[] _nests;

        [SetUp]
        public void SetUp()
        {
            _stone = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(_stone);
            so.FindProperty("_element").enumValueIndex = (int)Element.Stone;
            so.ApplyModifiedPropertiesWithoutUndo();
            _walker = ScriptableObject.CreateInstance<EnemyDefinition>();
            _difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();
            // One stone nest the waves use, one fire nest no wave uses.
            _nests = new[]
            {
                new ElementNest { Cell = new Vector2Int(2, 2), Element = Element.Stone },
                new ElementNest { Cell = new Vector2Int(6, 6), Element = Element.Fire },
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_stone);
            Object.DestroyImmediate(_walker);
            Object.DestroyImmediate(_difficulty);
        }

        private WaveDefinition NestWave() => new WaveDefinition(new[]
        {
            new SpawnGroup { Enemy = _walker, Count = 4, Interval = 1f, Delay = 0f, Source = SpawnSource.Gate },
            new SpawnGroup { Enemy = _stone, Count = NestCount, Interval = NestInterval, Delay = NestDelay, Source = SpawnSource.Nest },
        });

        private WaveDefinition GateWave() => new WaveDefinition(new[]
        {
            new SpawnGroup { Enemy = _walker, Count = 4, Interval = 1f, Delay = 0f, Source = SpawnSource.Gate },
        });

        private OriginDirector Director(params WaveDefinition[] waves)
        {
            var random = new System.Random(3);
            var assignments = new List<NestAssignment>();
            foreach (WaveDefinition wave in waves)
            {
                assignments.Add(NestAssignment.Draw(wave, _difficulty, _nests, random));
            }

            return new OriginDirector(_nests.Length, assignments, Timing);
        }

        [Test]
        public void AtLevelStart_UsedOriginsAreMarked_UnusedStayClosed()
        {
            OriginDirector director = Director(GateWave(), NestWave());

            Assert.AreEqual(OriginState.Marked, director.StateOf(0), "a wave sends stone creatures through the stone nest");
            Assert.AreEqual(OriginState.Closed, director.StateOf(1), "no wave uses the fire nest");
        }

        [Test]
        public void InAWaveWithoutNestCreatures_TheOriginStaysShut()
        {
            OriginDirector director = Director(GateWave(), NestWave());
            var scheduler = new WaveScheduler(GateWave(), _difficulty);
            director.BeginWave(0, scheduler);

            Run(director, 30f);

            Assert.AreEqual(OriginState.Marked, director.StateOf(0));
        }

        [Test]
        public void OriginOpens_LeadBeforeItsFirstCreature_BurstsThenLetsThemOut_AndStaysOpen()
        {
            OriginDirector director = Director(NestWave());
            var scheduler = new WaveScheduler(NestWave(), _difficulty);
            director.BeginWave(0, scheduler);
            var signals = new List<(OriginSignal Signal, OriginState State)>();
            float clock = 0f;
            director.Signalled += (origin, signal) => signals.Add((signal, director.StateOf(origin)));

            float openAt = NestDelay - Lead;
            clock = Run(director, openAt - Step, clock);
            Assert.AreEqual(OriginState.Marked, director.StateOf(0), "still shut just before the lead time");

            clock = Run(director, 2f * Step, clock);
            Assert.AreEqual(OriginState.Opening, director.StateOf(0), "opening at the lead time");
            Assert.IsFalse(director.IsReady(0), "no creature before the maw is up");

            // The opening began within a step after the lead time, so the burst falls between these two runs.
            clock = Run(director, Burst - 2f * Step, clock);
            Assert.IsFalse(signals.Exists(s => s.Signal == OriginSignal.Burst), "no burst before the opening's frame 54");
            clock = Run(director, 3f * Step, clock);
            Assert.IsTrue(signals.Exists(s => s.Signal == OriginSignal.Burst), "the burst falls on the opening's frame 54");

            clock = Run(director, Emerge - Burst, clock);
            Assert.IsTrue(director.IsReady(0), "the first creature can come out when it is due");
            Assert.LessOrEqual(openAt + Emerge, NestDelay + Step, "ready no later than the first creature's moment");

            // The three creatures come out on their schedule; after the last the origin stays open (Remco: it must not
            // sink shut again like a sphincter).
            for (int k = 0; k < NestCount; k++)
            {
                float due = scheduler.PlannedTime(1, k);
                clock = Run(director, Mathf.Max(0f, due - clock), clock);
                Assert.AreNotEqual(OriginState.Marked, director.StateOf(0), $"open while creature {k} comes out");
                director.NotifyEmerged(0);
            }

            float last = clock;
            Run(director, 30f, clock);
            Assert.AreEqual(OriginState.Open, director.StateOf(0), "still open long after the last creature");
            Assert.Greater(last, openAt);
        }

        [Test]
        public void AnOpenOrigin_StaysOpenThroughLaterWaves_AndLetsTheirCreaturesOutWithoutANewBurst()
        {
            OriginDirector director = Director(NestWave(), GateWave(), NestWave());
            var signals = new List<OriginSignal>();
            director.Signalled += (origin, signal) => signals.Add(signal);

            director.BeginWave(0, new WaveScheduler(NestWave(), _difficulty));
            Run(director, NestDelay + Open);
            Assert.AreEqual(OriginState.Open, director.StateOf(0), "open after its first wave");
            int bursts = signals.FindAll(s => s == OriginSignal.Burst).Count;

            director.BeginWave(1, new WaveScheduler(GateWave(), _difficulty));
            Run(director, 30f);
            Assert.AreEqual(OriginState.Open, director.StateOf(0), "a wave that sends nothing through it leaves it open");

            director.BeginWave(2, new WaveScheduler(NestWave(), _difficulty));
            Assert.IsTrue(director.IsReady(0), "creatures of a later wave come straight out");
            Run(director, NestDelay + Open);
            Assert.AreEqual(OriginState.Open, director.StateOf(0));
            Assert.AreEqual(bursts, signals.FindAll(s => s == OriginSignal.Burst).Count, "the ground bursts only the first time");
        }

        [Test]
        public void NestAssignment_GivesEveryNestSpawnANestOfItsElement()
        {
            var random = new System.Random(9);
            NestAssignment assignment = NestAssignment.Draw(NestWave(), _difficulty, _nests, random);

            for (int k = 0; k < assignment.CountOf(1); k++)
            {
                Assert.AreEqual(0, assignment.NestOf(1, k), "only the stone nest takes stone creatures");
            }

            Assert.AreEqual(NestAssignment.None, assignment.NestOf(0, 0), "a gate group has no nest");
            Assert.IsFalse(assignment.Uses(1));
        }

        [Test]
        public void Scheduler_PlannedTimes_MatchWhenTheSpawnsAreDue()
        {
            var scheduler = new WaveScheduler(NestWave(), _difficulty, new System.Random(5));
            var output = new List<SpawnRequest>();
            float clock = 0f;
            while (!scheduler.IsFinished && clock < 60f)
            {
                output.Clear();
                scheduler.Advance(Step, output);
                clock += Step;
                foreach (SpawnRequest request in output)
                {
                    // A spawn drawn early before the wave start comes on the first step.
                    float due = Mathf.Max(Step, scheduler.PlannedTime(request.Group, request.Index));
                    Assert.AreEqual(due, clock, Step + 1e-4f, "a spawn lands on its planned moment");
                }
            }

            Assert.IsTrue(scheduler.IsFinished);
        }

        private static float Run(OriginDirector director, float seconds, float clock = 0f)
        {
            int steps = Mathf.CeilToInt(seconds / Step - 1e-4f);
            for (int i = 0; i < steps; i++)
            {
                director.Advance(Step);
            }

            return clock + steps * Step;
        }
    }
}
