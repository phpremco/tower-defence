using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class DialoguePlayerTests
    {
        /// <summary>Writes every presenter call down as a short string.</summary>
        private sealed class RecordingPresenter : IDialoguePresenter
        {
            public readonly List<string> Calls = new List<string>();

            public void Open(DialogueDefinition dialogue) => Calls.Add($"open {dialogue.Id}");

            public void ShowLine(DialogueLine line, int lineIndex) => Calls.Add($"line {line.Id}");

            public void Reveal(int visibleCharacters, bool complete) => Calls.Add(complete ? "complete" : $"reveal {visibleCharacters}");

            public void Close() => Calls.Add("close");
        }

        private StoryTestData _data;
        private RecordingPresenter _presenter;
        private List<string> _seen;
        private DialoguePlayer _player;

        [SetUp]
        public void SetUp()
        {
            _data = new StoryTestData();
            _presenter = new RecordingPresenter();
            _seen = new List<string>();
            _player = new DialoguePlayer(_seen.Add, 10f) { Presenter = _presenter };
        }

        [TearDown]
        public void TearDown() => _data.Dispose();

        [Test]
        public void Advance_FirstFinishesTheLine_ThenMovesOn_AndMarksSeenAtTheEnd()
        {
            _player.Enqueue(_data.Dialogue("Dlg_T", "Hello there.", "Bye."));
            Assert.IsTrue(_player.TryStart());
            _player.Tick(0.3f);
            Assert.AreEqual(3, _player.VisibleCharacters);

            _player.Advance();
            Assert.IsTrue(_player.LineComplete);
            Assert.AreEqual(0, _player.LineIndex);

            _player.Advance();
            Assert.AreEqual(1, _player.LineIndex);
            CollectionAssert.IsEmpty(_seen);

            _player.Advance();
            _player.Advance();
            Assert.IsFalse(_player.IsBusy);
            CollectionAssert.AreEqual(new[] { "Dlg_T" }, _seen);
            CollectionAssert.AreEqual(new[] { "open Dlg_T", "line Dlg_T.01", "reveal 0", "reveal 3", "complete", "line Dlg_T.02", "reveal 0", "complete", "close" }, _presenter.Calls);
        }

        [Test]
        public void Queue_PlaysInOrder_InOneBox()
        {
            _player.Enqueue(_data.Dialogue("Dlg_1", "a"));
            _player.Enqueue(_data.Dialogue("Dlg_2", "b"));
            _player.TryStart();
            _player.SkipConversation();

            Assert.AreEqual("Dlg_2", _player.Current.Id, "skip ends only the current conversation");
            Assert.IsFalse(_presenter.Calls.Contains("close"), "the box stays up between queued conversations");
            _player.SkipConversation();
            CollectionAssert.AreEqual(new[] { "Dlg_1", "Dlg_2" }, _seen);
            Assert.IsFalse(_player.IsBusy);
        }

        [Test]
        public void Clear_DropsEverything_WithoutMarkingSeen()
        {
            _player.Enqueue(_data.Dialogue("Dlg_1", "a"));
            _player.Enqueue(_data.Dialogue("Dlg_2", "b"));
            _player.TryStart();
            _player.Clear();

            Assert.IsFalse(_player.IsBusy);
            CollectionAssert.IsEmpty(_seen);
            Assert.AreEqual("close", _presenter.Calls[_presenter.Calls.Count - 1]);
        }

        [Test]
        public void BusyChanged_FlipsOnEnqueueAndWhenDone()
        {
            int changes = 0;
            _player.BusyChanged += () => changes++;
            _player.Enqueue(_data.Dialogue("Dlg_1", "a"));
            Assert.AreEqual(1, changes);
            Assert.IsTrue(_player.IsBusy);
            Assert.IsFalse(_player.IsPlaying, "queued, not started until the caller says so");

            _player.TryStart();
            _player.SkipConversation();
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void AutoAdvance_RunsAConversationToTheEnd()
        {
            _player.AutoAdvance = true;
            _player.Enqueue(_data.Dialogue("Dlg_1", "first line", "second line", "third line"));
            _player.TryStart();
            for (int i = 0; i < 10 && _player.IsPlaying; i++)
            {
                _player.Tick(0f);
            }

            Assert.IsFalse(_player.IsBusy);
            CollectionAssert.AreEqual(new[] { "Dlg_1" }, _seen);
        }

        [Test]
        public void TryStart_DoesNothingWhileAConversationPlays()
        {
            _player.Enqueue(_data.Dialogue("Dlg_1", "a"));
            _player.Enqueue(_data.Dialogue("Dlg_2", "b"));
            Assert.IsTrue(_player.TryStart());
            Assert.IsFalse(_player.TryStart());
            Assert.AreEqual("Dlg_1", _player.Current.Id);
            Assert.AreEqual(1, _player.QueuedCount);
        }
    }
}
