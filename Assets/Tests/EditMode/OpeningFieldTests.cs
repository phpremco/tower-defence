using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Remco's playtest notes of 2026-11-08 on the opening cutscene: the drill goes up where the level keeps it (note 3),
    /// the brood roars and runs back into the ground before play starts (note 4), and the shots carry approved sounds (note 7).
    /// </summary>
    public sealed class OpeningFieldTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const float SameSpot = 0.01f;
        private const string SfxLibraryPath = "Assets/_Project/Data/Audio/SfxLibrary.asset";

        private CampaignCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(_catalog, "run Tools/Generate/Scenes and Starter Data");
        }

        [Test]
        public void TheDrill_StandsAtTheColonyRigTheLevelShowsAfterwards()
        {
            LevelDefinition field = _catalog.Story.OpeningCutscene.FieldLevel(_catalog);
            var clearance = new StructureClearance(field);
            Assume.That(clearance.HasColonyRig, $"{field.name} has no colony rig");

            var layout = new FieldLayout(field);

            Assert.IsTrue(layout.HasColonyRig);
            Assert.Less(Vector2.Distance(new Vector2(layout.Drill.x, layout.Drill.z), new Vector2(clearance.ColonyRigPosition.x, clearance.ColonyRigPosition.z)), SameSpot, "the cutscene drill is not where the rig stands in play");
            Assert.Less(Quaternion.Angle(layout.DrillFacing, clearance.ColonyRigFacing), 1f, "the cutscene drill faces another way than the rig in play");
        }

        [Test]
        public void TheHandoff_RoarsAndRunsBack_BeforePlayStarts()
        {
            CutsceneShot handoff = Find(CutsceneAction.Handoff);
            Assert.IsTrue(Has(handoff, CutsceneSound.QueenRoar), "no last roar in the handoff");
            Assert.IsTrue(Has(handoff, CutsceneSound.HeavySteps), "no running steps in the handoff");
        }

        [Test]
        public void TheOrbitAndTheLanding_SoundTheirMachines()
        {
            Assert.IsTrue(Has(Find(CutsceneAction.CapsulesFall), CutsceneSound.ColdGasPuff), "the capsules leave the bay in silence");
            Assert.IsTrue(Has(Find(CutsceneAction.CapsulesFall), CutsceneSound.BoosterIgnition), "no booster in the orbit shot");
            Assert.IsTrue(Has(Find(CutsceneAction.PathfinderLands), CutsceneSound.RetroBurst), "the capsule lands without its retro jets");
            Assert.Greater(AssetDatabase.LoadAssetAtPath<Game.Core.SfxLibrary>(SfxLibraryPath).DistantBlasts.Count, 0, "the approved distant blast is not in the sound library");
        }

        private CutsceneShot Find(CutsceneAction action)
        {
            foreach (CutsceneShot shot in _catalog.Story.OpeningCutscene.Shots)
            {
                if (shot.Action == action)
                {
                    return shot;
                }
            }

            Assert.Fail($"the opening has no {action} shot");
            return default;
        }

        private static bool Has(CutsceneShot shot, CutsceneSound sound)
        {
            foreach (CutsceneCue cue in shot.Cues)
            {
                if (cue.Kind == CueKind.Sound && cue.Sound == sound)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
