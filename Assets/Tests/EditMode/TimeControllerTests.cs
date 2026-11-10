using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The level clock: pause and speed act while the level runs; once it has ended the result screen owns the time
    /// scale, so neither a pause nor a speed change nor a hold may write it, while holds still open the input gate.
    /// </summary>
    public sealed class TimeControllerTests
    {
        private LevelDefinition _level;
        private LevelContext _context;
        private FakeInputProvider _input;
        private GameObject _host;
        private TimeController _time;
        private readonly object _owner = new object();

        [SetUp]
        public void SetUp()
        {
            _level = ScriptableObject.CreateInstance<LevelDefinition>();
            _context = new LevelContext(_level, null, null, null, null, null, null, null, null, null, 1, true);
            _input = new FakeInputProvider();
            _host = new GameObject("TimeControllerTest");
            _time = _host.AddComponent<TimeController>();
            _time.Initialize(_context, _input);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_level);
            Time.timeScale = 1f;
        }

        [Test]
        public void WhileRunning_PauseAndHoldStopTheClock()
        {
            _input.Pause();
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(_context.Paused);
            _input.Pause();
            Assert.AreEqual(1f, Time.timeScale);

            _time.Hold(_owner);
            Assert.AreEqual(0f, Time.timeScale);
            _time.Release(_owner);
            Assert.AreEqual(1f, Time.timeScale);
        }

        // Remco 2026-11-10: the Codex (and its combinations page) holds the clock while it is open and gives back exactly the
        // state it found: the chosen speed, or the pause when it was opened from the pause menu.
        [Test]
        public void AHold_GivesBackTheSpeedItFound()
        {
            _input.Speed();
            Assert.AreEqual(2f, Time.timeScale);

            _time.Hold(_owner);
            Assert.AreEqual(0f, Time.timeScale, "the book stops the fight while it is read");
            _time.Release(_owner);

            Assert.AreEqual(2f, Time.timeScale, "closing the book returns to double speed, not to normal speed");
        }

        [Test]
        public void AHold_OverThePause_LeavesThePauseStanding()
        {
            _input.Pause();
            _time.Hold(_owner);
            _time.Release(_owner);

            Assert.IsTrue(_context.Paused, "a book opened from the pause menu closes back into the pause menu");
            Assert.AreEqual(0f, Time.timeScale);
        }

        [Test]
        public void AfterTheOutcome_PauseAndSpeedAreIgnored()
        {
            _context.Outcome = LevelOutcome.Won;
            const float ResultScale = 1f;
            Time.timeScale = ResultScale;

            _input.Pause();
            Assert.IsFalse(_context.Paused, "no pause menu opens after the end, so no pause may freeze the ending");
            Assert.AreEqual(ResultScale, Time.timeScale);

            _input.Speed();
            Assert.AreEqual(ResultScale, Time.timeScale, "the speed button no longer changes the clock");
        }

        [Test]
        public void AfterTheOutcome_HoldsOnlyMoveTheInputGate()
        {
            _time.Hold(_owner);
            Assert.AreEqual(0f, Time.timeScale);
            _context.Outcome = LevelOutcome.Won;

            _time.Release(_owner);

            Assert.IsFalse(_context.Held, "a dialogue that ends after the victory still opens the input gate");
            Assert.AreEqual(0f, Time.timeScale, "but the time scale is left to the result screen");
        }

        [Test]
        public void AnInterruption_PausesWithTheMenuAndStaysPaused()
        {
            _time.PauseForInterruption();
            Assert.IsTrue(_context.Paused, "the pause menu opens while the player is away");
            Assert.AreEqual(0f, Time.timeScale);

            _time.PauseForInterruption();
            Assert.IsTrue(_context.Paused, "a second interruption does not toggle the pause off again");

            _input.Pause();
            Assert.IsFalse(_context.Paused, "the player resumes as from any pause");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void TheGamepadInUseDisconnecting_OpensThePauseMenu()
        {
            _input.LoseDevice();
            Assert.IsTrue(_context.Paused, "consoles require a pause when the controller drops out");
            Assert.AreEqual(0f, Time.timeScale);

            _input.Pause();
            Assert.IsFalse(_context.Paused, "the player resumes from the pause menu as from any pause");
        }

        [Test]
        public void AnInterruption_AfterTheOutcome_ChangesNothing()
        {
            _context.Outcome = LevelOutcome.Lost;

            _time.PauseForInterruption();

            Assert.IsFalse(_context.Paused, "no pause menu after the end, so no pause may freeze the result screen");
        }
    }
}
