using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Remco 2026-11-08 (playtest note 12): Dr. Voss teaches the in-level gate upgrade and sends Spencer to the gate under
    /// pressure. Each tip waits for the moment it can be acted on, plays once per save, and the story data carries both.
    /// </summary>
    public sealed class GateTipTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const float HurtBelow = 0.6f;
        private const float QuietAbove = 0.9f;

        private StoryTestData _data;
        private DialogueDefinition _upgrade;
        private DialogueDefinition _move;
        private GateTip[] _tips;

        [SetUp]
        public void SetUp()
        {
            _data = new StoryTestData();
            _upgrade = _data.Dialogue("Tip_Upgrade", "Upgrade that gate.");
            _move = _data.Dialogue("Tip_Move", "Go to the other gate.");
            _tips = new[]
            {
                new GateTip(GateTipKind.UpgradeGate, _upgrade, HurtBelow, 0f),
                new GateTip(GateTipKind.MoveHero, _move, HurtBelow, QuietAbove),
            };
        }

        [TearDown]
        public void TearDown() => _data.Dispose();

        private static GateTipGate Gate(float health, bool affordable = false, bool standing = true) => new GateTipGate(health, standing, affordable);

        [Test]
        public void TheUpgradeTip_WaitsForAHurtGate_ThatThePlayerCanPayToUpgrade()
        {
            GateTip tip = _tips[0];

            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(1f, affordable: true) }, 0, true), "whole gate");
            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(0.4f) }, 0, true), "hurt, but no money or no next level");
            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(0f, affordable: true, standing: false) }, 0, true), "a wreck is rebuilt, not upgraded");
            Assert.IsTrue(GateTipRule.Applies(tip, new[] { Gate(1f), Gate(0.4f, affordable: true) }, 0, true), "the second gate is hurt and affordable");
        }

        [Test]
        public void TheMoveTip_WantsAnotherHurtGate_WhileSpencersOwnGateIsQuiet()
        {
            GateTip tip = _tips[1];

            Assert.IsTrue(GateTipRule.Applies(tip, new[] { Gate(1f), Gate(0.4f) }, 0, true), "his gate quiet, the other hurt");
            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(0.5f), Gate(0.4f) }, 0, true), "his own gate is under attack too");
            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(1f), Gate(0.4f) }, 1, true), "he already stands at the hurt gate");
            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(1f), Gate(0.4f) }, 0, false), "down or in flight");
            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(1f), Gate(0.4f) }, -1, true), "he holds the base");
            Assert.IsFalse(GateTipRule.Applies(tip, new[] { Gate(1f), Gate(0f, standing: false) }, 0, true), "the other gate is a wreck");
        }

        [Test]
        public void Pick_SkipsSeenAndAlreadyPickedTips()
        {
            GateTipGate[] gates = { Gate(1f), Gate(0.4f, affordable: true) };
            var seen = new HashSet<string>();

            Assert.AreSame(_upgrade, GateTipRule.Pick(_tips, gates, 0, true, seen.Contains, null), "the first tip whose moment came");
            Assert.AreSame(_move, GateTipRule.Pick(_tips, gates, 0, true, seen.Contains, new[] { _upgrade }), "the upgrade tip is queued already");

            seen.Add(_upgrade.Id);
            seen.Add(_move.Id);
            Assert.IsNull(GateTipRule.Pick(_tips, gates, 0, true, seen.Contains, null), "both seen in this save");
        }

        [Test]
        public void TheStory_CarriesBothTips_InVossVoice_AndTheViewerListsThem()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            IReadOnlyList<GateTip> tips = catalog.Story.GateTips;
            var kinds = new HashSet<GateTipKind>();
            var listed = new HashSet<DialogueDefinition>();
            foreach (StoryIndex.Page page in new StoryIndex(catalog).Pages)
            {
                foreach (StoryIndex.Entry entry in page.Entries)
                {
                    if (page.Title == StoryIndex.ScanPageTitle && entry.Dialogue != null)
                    {
                        listed.Add(entry.Dialogue);
                    }
                }
            }

            foreach (GateTip tip in tips)
            {
                Assert.IsNotNull(tip.Dialogue, "run Tools/Generate/Scan Hints and Button Prompts");
                Assert.Greater(tip.Dialogue.Lines.Count, 0);
                foreach (DialogueLine line in tip.Dialogue.Lines)
                {
                    Assert.AreEqual("Speaker_Voss", line.Speaker.name, $"{tip.Dialogue.Id} is Voss's");
                }

                Assert.IsTrue(listed.Contains(tip.Dialogue), $"the story viewer does not list {tip.Dialogue.Id} on the scans page");
                kinds.Add(tip.Kind);
            }

            Assert.IsTrue(kinds.Contains(GateTipKind.UpgradeGate) && kinds.Contains(GateTipKind.MoveHero), "both gate tips are in the story");
        }
    }
}
