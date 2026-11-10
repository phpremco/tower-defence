using Game.Balance;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Remco 2026-11-10: rifts that join the road behind the defence gate stay, and the first one that opens in a save gets
    /// Voss's warning with a ping. The rule is the balance model's (round 31, RouteKill), the trigger fires once per level, and
    /// the data carries the warning and lists it beside the scans.
    /// </summary>
    public sealed class RearNestTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const int RoadLength = 20;
        private const int GateAt = 5;

        // A straight road of 21 cells with the defence gate on cell 5.
        private static LevelGeometry StraightRoad()
        {
            var road = new RoadInput { PathIndex = 0, Waypoints = new[] { new Cell(0, 0), new Cell(RoadLength, 0) } };
            return LevelGeometry.Build(new[] { road }, new RoadInput[0], new[] { new Cell(GateAt, 0) }, new Cell(GateAt, 0), new Cell[0], 1f);
        }

        [Test]
        public void ANest_JoiningPastTheDefenceGate_IsBehindIt()
        {
            // Remaining shares: the spawn end (joins at 0), the gate's own cell (joins at 5: held there), and two past it.
            bool[] behind = RearNests.BehindGate(StraightRoad(), new[] { 1f, 1f - GateAt / (float)RoadLength, 0.5f, 0.1f });

            Assert.AreEqual(new[] { false, false, true, true }, behind);
        }

        [Test]
        public void WithoutADefenceGate_NoNestIsBehindIt()
        {
            var road = new RoadInput { PathIndex = 0, Waypoints = new[] { new Cell(0, 0), new Cell(RoadLength, 0) } };
            LevelGeometry geometry = LevelGeometry.Build(new[] { road }, new RoadInput[0], new Cell[0], null, new Cell[0], 1f);

            Assert.AreEqual(new[] { false }, RearNests.BehindGate(geometry, new[] { 0.1f }));
        }

        [Test]
        public void TheWatch_FiresOnce_ForTheFirstRearNestThatOpens()
        {
            var watch = new RearNestWatch(new[] { false, true, true });

            Assert.IsTrue(watch.HasAny);
            Assert.IsFalse(watch.TryFire(0, OriginState.Opening), "a nest before the gate is no surprise");
            Assert.IsFalse(watch.TryFire(1, OriginState.Marked), "marked is not open yet");
            Assert.IsTrue(watch.TryFire(1, OriginState.Opening), "the first rear rift opening warns");
            Assert.IsFalse(watch.TryFire(2, OriginState.Open), "one warning per level");
            Assert.IsFalse(new RearNestWatch(new[] { false }).HasAny);
        }

        [Test]
        public void TheWatch_FiresOnAForcedOpening()
        {
            // A wave called early opens its nests at once (OriginDirector.ForceOpen), without the Opening state.
            Assert.IsTrue(new RearNestWatch(new[] { true }).TryFire(0, OriginState.Open));
        }

        [Test]
        public void CrystalPlains_HasItsRiftBehindTheGate_AndTheStoryWarnsAboutIt()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            LevelDefinition first = catalog.Nodes[0].Level;

            // Round 31: Crystal Plains' rift nest joins the road behind the defence gate; Remco keeps it there on purpose.
            Assert.Contains(true, RearNests.BehindGate(first), $"{first.name}: no nest joins behind the defence gate any more");

            DialogueDefinition warning = catalog.Story.RearNestWarning;
            Assert.IsNotNull(warning, "run Tools/Generate/Scan Hints and Button Prompts");
            Assert.Greater(warning.Lines.Count, 0);
            foreach (DialogueLine line in warning.Lines)
            {
                Assert.AreEqual("Speaker_Voss", line.Speaker.name, "the warning is Voss's");
            }

            bool listed = false;
            foreach (StoryIndex.Page page in new StoryIndex(catalog).Pages)
            {
                foreach (StoryIndex.Entry entry in page.Entries)
                {
                    listed |= page.Title == StoryIndex.ScanPageTitle && entry.Dialogue == warning;
                }
            }

            Assert.IsTrue(listed, "the developer story viewer does not list the warning on the scans page");
        }
    }
}
