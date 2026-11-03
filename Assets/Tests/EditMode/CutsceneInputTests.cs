using Game.Levels;
using Game.UI;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Cutscene buttons: while a dialogue box is open, Start never arms the skip of the whole cutscene (S2 decision 10).</summary>
    public sealed class CutsceneInputTests
    {
        private StoryTestData _data;
        private FakeInputProvider _input;
        private DialoguePlayer _dialogue;
        private CutsceneInput _cutsceneInput;
        private int _skipPresses;

        [SetUp]
        public void SetUp()
        {
            _data = new StoryTestData();
            _input = new FakeInputProvider();
            _dialogue = new DialoguePlayer(null);
            _skipPresses = 0;
            _cutsceneInput = new CutsceneInput(_input, _dialogue, () => _skipPresses++);
        }

        [TearDown]
        public void TearDown()
        {
            _cutsceneInput.Dispose();
            _data.Dispose();
        }

        [Test]
        public void Start_WhileABoxIsOpen_DoesNotArmTheSkip()
        {
            _dialogue.Enqueue(_data.Dialogue("Dlg_Test", "First line.", "Second line."));
            Assert.IsTrue(_dialogue.TryStart());

            _input.Pause();

            Assert.AreEqual(0, _skipPresses, "Start belongs to nobody while someone speaks");
            Assert.IsTrue(_dialogue.IsPlaying, "and it does not close the box either");
        }

        [Test]
        public void Start_WithNoBoxOpen_ArmsTheSkip()
        {
            _input.Pause();

            Assert.AreEqual(1, _skipPresses);
        }

        [Test]
        public void Cancel_WhileABoxIsOpen_SkipsOnlyTheConversation()
        {
            _dialogue.Enqueue(_data.Dialogue("Dlg_Test", "First line.", "Second line."));
            _dialogue.TryStart();

            _input.Cancel();

            Assert.AreEqual(0, _skipPresses);
            Assert.IsFalse(_dialogue.IsPlaying);
        }
    }
}
