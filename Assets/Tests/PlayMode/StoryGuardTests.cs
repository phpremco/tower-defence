using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Regression tests for the S1/S2 review: a dialogue box parks the gamepad focus and gates the tower and drone
    /// menus (finding 1), and a dialogue raised by the last kill or holding the clock when the level ends neither gets
    /// lost nor keeps the hold (finding 2). Plays the landing level; the real save is restored afterwards.
    /// </summary>
    public sealed class StoryGuardTests
    {
        private const int MediumDifficultyIndex = 1;
        private const int MaxWaitFrames = 300;
        private const int MaxAdvances = 20;
        private const int GuardMoney = 100000;
        private const string StartDialogue = "Dlg_L01_Start";
        private const string BossDialogue = "Dlg_L01_Boss";
        private const string WonDialogue = "Dlg_L01_Won";
        private static readonly Vector2Int GuardCell = new Vector2Int(2, 2);

        private sealed class RecordingPresenter : IDialoguePresenter
        {
            public readonly List<string> Opened = new List<string>();

            public void Open(DialogueDefinition dialogue) => Opened.Add(dialogue.Id);

            public void ShowLine(DialogueLine line, int lineIndex)
            {
            }

            public void Reveal(int visibleCharacters, bool complete)
            {
            }

            public void Close()
            {
            }
        }

        private string _savePath;
        private byte[] _saveBytes;
        private List<string> _seenBefore;
        private LevelContext _context;

        [SetUp]
        public void SetUp()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "Saves", "progress.json");
            _saveBytes = File.Exists(_savePath) ? File.ReadAllBytes(_savePath) : null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_seenBefore != null && Services.TryGet(out ProgressService progress))
            {
                progress.Data.SeenStoryIds.Clear();
                progress.Data.SeenStoryIds.AddRange(_seenBefore);
            }

            if (_saveBytes != null)
            {
                File.WriteAllBytes(_savePath, _saveBytes);
            }
            else if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
        }

        [UnityTest]
        public IEnumerator Dialogue_ParksTheMenuFocus_AndGatesTheMenuActions()
        {
            yield return LoadLanding(StartDialogue);
            LevelContext context = _context;

            TowerDefinition definition = context.AvailableTowers[0];
            var go = new GameObject("GuardTower");
            go.transform.position = context.Level.Grid.CellToWorld(GuardCell);
            var tower = go.AddComponent<Tower>();
            go.AddComponent<TowerView>();
            tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
            tower.Register(context.Structures);
            context.Wallet.Add(GuardMoney);
            context.SelectedPlacedTower = tower;
            context.Mode = PlacementMode.TowerMenu;
            context.Changed.Raise();
            yield return null;

            GameObject focused = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(focused, "the tower menu focuses its first button for the gamepad");

            // The level's real dialogue box (the HUD's presenter) plays the opening line.
            context.StoryEnabled = true;
            yield return WaitUntilPlaying(context);
            yield return null;
            Assert.IsNull(EventSystem.current.currentSelectedGameObject, "the box parks the focus, so A cannot submit the menu");
            Assert.IsTrue(context.InputBlocked);

            int level = tower.LevelIndex;
            int money = context.Wallet.Money;
            DroneDefinition drone = context.Level.Drones.Length > 0 ? context.Level.Drones[0] : null;
            int drones = context.ActiveDrones(drone);
            context.UpgradeRequested.Raise();
            context.RepairRequested.Raise();
            context.SellRequested.Raise();
            context.CombineRequested.Raise();
            if (drone != null)
            {
                context.DroneRequested.Raise(drone);
            }

            Assert.AreEqual(level, tower.LevelIndex, "no upgrade behind the box");
            Assert.AreEqual(money, context.Wallet.Money, "nothing bought or sold behind the box");
            Assert.IsTrue(tower != null && tower.gameObject.activeSelf, "the tower is still there");
            Assert.AreEqual(drones, context.ActiveDrones(drone), "no drone launched behind the box");

            Advance(context, StartDialogue);
            var view = Object.FindAnyObjectByType<DialogueView>();
            for (int i = 0; i < MaxWaitFrames && view != null && view.IsShown; i++)
            {
                yield return null;
            }

            yield return null;
            Assert.IsFalse(context.InputBlocked, "the hold ends with the conversation");
            Assert.AreSame(focused, EventSystem.current.currentSelectedGameObject, "the focus comes back once the box has gone");
            context.UpgradeRequested.Raise();
            Assert.AreEqual(level + 1, tower.LevelIndex, "the same request works once the box is gone");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DialogueOfTheLastFrame_PlaysBeforeTheVictory()
        {
            var presenter = new RecordingPresenter();
            yield return LoadLanding(StartDialogue, BossDialogue, WonDialogue);
            LevelContext context = _context;
            yield return FinishOpening(context, presenter);

            // The last frame of a level: a boss line is raised and the level ends before the story's late update.
            Spawner().Spawn(FindBoss(context), 0);
            context.Outcome = LevelOutcome.Won;
            context.Changed.Raise();
            Assert.IsTrue(context.StoryBusy, "the result screen waits for the story");

            yield return null;
            Assert.IsTrue(context.Dialogue.IsPlaying);
            Assert.AreEqual(BossDialogue, context.Dialogue.Current.Id, "the line of the last frame plays first");
            Assert.IsFalse(context.Held, "no hold is taken after the outcome");

            Advance(context, BossDialogue);
            yield return null;
            Assert.IsTrue(context.Dialogue.IsPlaying && context.Dialogue.Current.Id == WonDialogue, "then the victory line");
            Advance(context, WonDialogue);
            yield return null;
            Assert.IsFalse(context.StoryBusy, "the result screen may show");
            CollectionAssert.AreEqual(new[] { StartDialogue, BossDialogue, WonDialogue }, presenter.Opened);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator HoldTakenBeforeTheEnd_IsReleasedAfterIt()
        {
            var presenter = new RecordingPresenter();
            yield return LoadLanding(StartDialogue, BossDialogue, WonDialogue);
            LevelContext context = _context;
            yield return FinishOpening(context, presenter);

            Spawner().Spawn(FindBoss(context), 0);
            yield return WaitUntilPlaying(context);
            Assert.AreEqual(BossDialogue, context.Dialogue.Current.Id);
            yield return null;
            Assert.IsTrue(context.Held, "the boss line holds the clock");

            context.Outcome = LevelOutcome.Won;
            context.Changed.Raise();
            Advance(context, BossDialogue);
            yield return null;

            // The victory line follows on in the same box, still under the hold taken before the end.
            Assert.IsTrue(context.Dialogue.IsPlaying && context.Dialogue.Current.Id == WonDialogue, "the victory line follows");
            Advance(context, WonDialogue);
            yield return null;

            Assert.IsFalse(context.Dialogue.IsBusy);
            Assert.IsFalse(context.Held, "the hold from before the end is given back once the story is done");
            Assert.IsFalse(context.InputBlocked, "so the result screen's input is not gated");
            Assert.AreEqual(0f, Time.timeScale, "the time scale is left to the result screen");
            CollectionAssert.AreEqual(new[] { StartDialogue, BossDialogue, WonDialogue }, presenter.Opened);
            LogAssert.NoUnexpectedReceived();
        }

        // Boot, forget the given dialogues as seen, then the landing level on Medium without briefing.
        private IEnumerator LoadLanding(params string[] unseen)
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var progress = Services.Get<ProgressService>();
            _seenBefore = new List<string>(progress.Data.SeenStoryIds);
            foreach (string id in unseen)
            {
                progress.Data.SeenStoryIds.Remove(id);
            }

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[0].Level, catalog.Difficulties[MediumDifficultyIndex]));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;

            Assert.IsTrue(Services.TryGet(out _context), "LevelContext was never registered.");
        }

        // Story on with the recording presenter; the opening dialogue plays and is read to its end.
        private static IEnumerator FinishOpening(LevelContext context, RecordingPresenter presenter)
        {
            context.Dialogue.Presenter = presenter;
            context.StoryEnabled = true;
            yield return WaitUntilPlaying(context);
            Assert.AreEqual(StartDialogue, context.Dialogue.Current.Id);
            Advance(context, StartDialogue);
            yield return null;
            Assert.IsFalse(context.Held);
        }

        private static IEnumerator WaitUntilPlaying(LevelContext context)
        {
            for (int i = 0; i < MaxWaitFrames && !context.Dialogue.IsPlaying; i++)
            {
                yield return null;
            }

            Assert.IsTrue(context.Dialogue.IsPlaying, "a dialogue starts");
        }

        private static void Advance(LevelContext context, string id)
        {
            for (int i = 0; i < MaxAdvances && context.Dialogue.IsPlaying && context.Dialogue.Current.Id == id; i++)
            {
                context.Dialogue.Advance();
            }
        }

        private static EnemyDefinition FindBoss(LevelContext context)
        {
            foreach (WaveDefinition wave in context.Level.GetWaves(context.Difficulty))
            {
                foreach (SpawnGroup group in wave.Groups)
                {
                    if (group.Enemy != null && group.Enemy.IsBoss)
                    {
                        return group.Enemy;
                    }
                }
            }

            Assert.Fail("the landing level has a boss");
            return null;
        }

        // The level's own spawner, so the boss comes in through the real events.
        private static EnemySpawner Spawner()
        {
            var controller = Object.FindAnyObjectByType<LevelController>();
            FieldInfo field = typeof(LevelController).GetField("_spawner", BindingFlags.NonPublic | BindingFlags.Instance);
            return (EnemySpawner)field.GetValue(controller);
        }
    }
}
