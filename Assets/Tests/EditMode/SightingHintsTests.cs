using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>Round 33: Dr. Voss's creature scans, the first-sighting rule and the campaign data.</summary>
    public sealed class SightingHintsTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const int ScannedLevels = 6;
        private const string VossName = "Speaker_Voss";

        private StoryTestData _data;
        private EnemyDefinition _cragback;
        private EnemyDefinition _wisp;
        private DialogueDefinition _scan;
        private ScanHint[] _hints;
        private readonly HashSet<string> _seen = new HashSet<string>();
        private readonly List<DialogueDefinition> _picked = new List<DialogueDefinition>();

        [SetUp]
        public void SetUp()
        {
            _data = new StoryTestData();
            _cragback = _data.Enemy("Cragback");
            _wisp = _data.Enemy("Wisp");
            _scan = _data.Dialogue("Dlg_Scan_Cragback", "Use the cannon.");
            _hints = new[] { new ScanHint(_cragback, _scan) };
            _seen.Clear();
            _picked.Clear();
        }

        [TearDown]
        public void TearDown() => _data.Dispose();

        [Test]
        public void FirstSighting_OfAScannedCreature_PicksItsScan()
        {
            Assert.AreSame(_scan, SightingHints.Pick(_hints, _cragback, _seen.Contains, _picked));
        }

        [Test]
        public void ACreatureWithoutAScan_PicksNothing()
        {
            Assert.IsNull(SightingHints.Pick(_hints, _wisp, _seen.Contains, _picked));
            Assert.IsNull(SightingHints.Pick(_hints, null, _seen.Contains, _picked));
            Assert.IsNull(SightingHints.Pick(null, _cragback, _seen.Contains, _picked));
        }

        [Test]
        public void AScanSeenInThisSave_NeverPlaysAgain_InAnyLevel()
        {
            _seen.Add(_scan.Id);
            Assert.IsNull(SightingHints.Pick(_hints, _cragback, _seen.Contains, _picked));
        }

        [Test]
        public void AScanTheLevelAlreadyPlaysAtThisMoment_IsNotQueuedTwice()
        {
            _picked.Add(_scan);
            Assert.IsNull(SightingHints.Pick(_hints, _cragback, _seen.Contains, _picked));
        }

        [Test]
        public void ScanRequest_EndsTheScanAsSeen_AndAsksForTheCreaturesPage()
        {
            var so = new SerializedObject(_scan);
            so.FindProperty("_codexEntry").objectReferenceValue = _cragback;
            so.ApplyModifiedPropertiesWithoutUndo();
            var seen = new List<string>();
            var player = new DialoguePlayer(seen.Add);
            EnemyDefinition asked = null;
            Assert.IsFalse(player.CanOpenCodex, "nothing plays");
            player.CodexRequested += enemy => asked = enemy;
            player.Enqueue(_scan);
            player.TryStart();

            Assert.IsTrue(player.CanOpenCodex);
            player.RequestCodex();

            Assert.AreSame(_cragback, asked);
            Assert.IsFalse(player.IsBusy, "the scan closed, so the clock is released and the Codex can open");
            CollectionAssert.Contains(seen, _scan.Id);
        }

        [Test]
        public void AConversationWithoutACodexEntry_OffersNoCodex()
        {
            var player = new DialoguePlayer(null);
            player.CodexRequested += _ => Assert.Fail("no entry to open");
            player.Enqueue(_data.Dialogue("Dlg_Plain", "Hello."));
            player.TryStart();
            Assert.IsFalse(player.CanOpenCodex);
            player.RequestCodex();
            Assert.IsTrue(player.IsPlaying, "the request did nothing");
        }

        // Every creature (not the bosses, who have their own intros) that Crystal Plains levels 1 to 6 send on any difficulty
        // has a scan by Dr. Voss, linked to its own Codex page.
        [Test]
        public void EveryCreatureOfTheFirstSixLevels_HasAVossScanWithACodexLink()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "campaign missing");
            Assert.IsNotNull(catalog.Story, "story missing");
            IReadOnlyList<ScanHint> hints = catalog.Story.ScanHints;
            var roster = new HashSet<EnemyDefinition>();
            var missing = new List<string>();
            for (int i = 0; i < ScannedLevels && i < catalog.Nodes.Count; i++)
            {
                LevelRoster.Collect(catalog.Nodes[i].Level, null, roster);
                foreach (EnemyDefinition enemy in roster)
                {
                    if (enemy.IsBoss)
                    {
                        continue;
                    }

                    DialogueDefinition scan = SightingHints.Find(hints, enemy);
                    if (scan == null)
                    {
                        missing.Add($"{catalog.Nodes[i].Level.name}: {enemy.name}");
                        continue;
                    }

                    Assert.AreSame(enemy, scan.CodexEntry, $"{scan.name} links to the wrong Codex entry");
                    Assert.IsTrue(SpokenByVoss(scan), $"{scan.name} has no line by Dr. Voss");
                }
            }

            CollectionAssert.IsEmpty(missing, "creatures without a scan");
        }

        private static bool SpokenByVoss(DialogueDefinition dialogue)
        {
            foreach (DialogueLine line in dialogue.Lines)
            {
                if (line.Speaker != null && line.Speaker.name == VossName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
