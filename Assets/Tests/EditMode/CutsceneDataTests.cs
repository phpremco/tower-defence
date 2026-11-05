using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The generated opening cutscene: valid data, every action on its own set, a resolvable handoff, the agreed length.</summary>
    public sealed class CutsceneDataTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        // About two minutes when nobody reads (the agreed pacing); reading the boxes adds to it.
        private const float MinAutoSeconds = 100f;
        private const float MaxAutoSeconds = 170f;

        // The prologue (Earth, survey, orbit) is mostly reading: a few seconds per shot when the boxes close at once.
        private const float MaxPrologueAutoSeconds = 30f;

        // The attack: a few of the brood with the Queen, walkers and flyers together.
        private const int MinBrood = 3;
        private const int MaxBrood = 7;

        private CampaignCatalog _catalog;
        private CutsceneDefinition _cutscene;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(_catalog, "run Tools/Generate/Scenes and Starter Data");
            Assert.IsNotNull(_catalog.Story, "the campaign carries its story");
            _cutscene = _catalog.Story.OpeningCutscene;
            Assert.IsNotNull(_cutscene, "the story names its opening cutscene");
        }

        [Test]
        public void GeneratedCutscene_Validates()
        {
            var errors = new List<string>();
            Assert.IsTrue(_cutscene.Validate(errors, _catalog), string.Join("\n", errors));
            foreach (CutsceneShot shot in _cutscene.Shots)
            {
                if (shot.Dialogue != null)
                {
                    var dialogueErrors = new List<string>();
                    Assert.IsTrue(shot.Dialogue.Validate(dialogueErrors), $"{shot.Dialogue.name}: {string.Join(" ", dialogueErrors)}");
                }
            }
        }

        [Test]
        public void EveryAction_IsSupportedByItsSet()
        {
            foreach (CutsceneShot shot in _cutscene.Shots)
            {
                Assert.IsTrue(CutsceneActions.Supports(shot.Set, shot.Action), $"{shot.Set} cannot show {shot.Action}");
            }
        }

        [Test]
        public void FirstLaunchRequest_HandsOverTheWayTheDataSays()
        {
            CutsceneRequest request = CutsceneRequest.FirstLaunch(_cutscene);

            Assert.AreSame(_cutscene, request.Cutscene);
            Assert.AreEqual(_cutscene.Handoff, request.Handoff, "the first launch follows the cutscene's own handoff, not a fixed one");
        }

        [Test]
        public void Handoff_ResolvesTheFirstLevelOnACampaignDifficulty()
        {
            Assert.AreEqual(CutsceneHandoff.FirstCampaignLevel, _cutscene.Handoff);
            LevelDefinition level = CutsceneDefinition.FirstCampaignLevel(_catalog);
            Assert.IsNotNull(level);
            Assert.AreSame(_catalog.Nodes[_catalog.FirstNode(0)].Level, level, "the first level comes from campaign data");
            Assert.IsNotNull(_cutscene.HandoffDifficulty);
            CollectionAssert.Contains(new List<DifficultyProfile>(_catalog.Difficulties), _cutscene.HandoffDifficulty);
            Assert.AreSame(level, _cutscene.FieldLevel(_catalog), "the story plays on the level it hands over to");
            CutsceneShot last = _cutscene.Shots[_cutscene.Shots.Count - 1];
            Assert.AreEqual(CameraKeyMode.MatchLevelRig, last.Camera[last.Camera.Length - 1].Mode, "the last move aims at the level camera");
        }

        [Test]
        public void AutoAdvanceRuntime_FallsInsideTheAgreedBand()
        {
            float total = _cutscene.AutoSeconds();
            Assert.That(total, Is.InRange(MinAutoSeconds, MaxAutoSeconds), $"auto-advance runs {total:0.#} s");
            int firstField = -1;
            for (int i = 0; i < _cutscene.Shots.Count && firstField < 0; i++)
            {
                if (_cutscene.Shots[i].Set == CutsceneSet.Field)
                {
                    firstField = i;
                }
            }

            Assert.Greater(firstField, 0, "the prologue comes before the field");
            Assert.LessOrEqual(_cutscene.AutoSeconds(0, firstField - 1), MaxPrologueAutoSeconds);
        }

        [Test]
        public void Cast_NamesEveryRole()
        {
            CutsceneCast cast = _cutscene.Cast;
            Assert.IsNotNull(cast.Hero);
            Assert.IsNotNull(cast.Hauler);
            Assert.IsNotNull(cast.Builder);
            Assert.IsNotNull(cast.Screamer);
            Assert.Greater(cast.Swarm.Length, 0);
            foreach (EnemyDefinition creature in cast.Swarm)
            {
                Assert.IsNotNull(creature);
                Assert.IsFalse(creature.Flies, $"{creature.name} must walk the road");
            }

            foreach (EnemyDefinition flyer in cast.Flyers)
            {
                Assert.IsNotNull(flyer);
                Assert.IsTrue(flyer.Flies, $"{flyer.name} must fly over the swarm");
            }
        }

        // Remco, 2026-11-05: no legion of rollers in the opening. The angry Queen and a few of her brood, of different
        // species, standing and glaring at the colony are enough; nothing in it rolls.
        [Test]
        public void Attack_IsTheQueenAndAFewOfHerBrood_NothingRolls()
        {
            CutsceneCast cast = _cutscene.Cast;
            Assert.LessOrEqual(cast.SwarmCount + cast.FlyerCount, MaxBrood, "the attack is a horde again");
            Assert.GreaterOrEqual(cast.SwarmCount, MinBrood, "the Queen needs a few of her brood around her");
            var species = new HashSet<EnemyDefinition>();
            for (int i = 0; i < cast.SwarmCount; i++)
            {
                species.Add(cast.Swarm[i % cast.Swarm.Length]);
            }

            Assert.AreEqual(cast.SwarmCount, species.Count, "every one of the brood is a different species");
            foreach (EnemyDefinition creature in species)
            {
                Assert.IsFalse(creature.Rolls, $"{creature.name} rolls");
            }

            Assert.IsFalse(cast.Screamer.Rolls);
        }

        [Test]
        public void LevelPlayPose_LooksAtTheCommandersFirstPost()
        {
            LevelDefinition level = CutsceneDefinition.FirstCampaignLevel(_catalog);
            LevelPlayPose.HeroStart(level, out Vector3 hero, out Vector3 facing);
            CameraSample pose = LevelPlayPose.Pose(level, 16f / 9f, 14f);

            Assert.AreEqual(hero.x, pose.LookAt.x, 0.001f);
            Assert.AreEqual(hero.z, pose.LookAt.z, 0.001f);
            Vector3 view = (pose.LookAt - pose.Position).normalized;
            Vector3 levelView = Quaternion.Euler(level.Camera.PitchDegrees, level.Camera.YawDegrees, 0f) * Vector3.forward;
            Assert.Greater(Vector3.Dot(view, levelView), 0.999f, "the long lens looks along the level camera");
            Assert.AreEqual(1f, facing.magnitude, 0.001f);
        }

        // Playtest 2026-11-04: the camera shook all through the planet shots. Remco's rule since: a camera shake is only ever
        // a brief kick on a real impact (the capsule landing, the drill bit biting, the ground bursting open), one per shot
        // at most, and the drift stays too slow to read as shake.
        [Test]
        public void Camera_KicksOnlyOnImpacts_AndNeverShakes()
        {
            var impacts = new[] { CutsceneAction.PathfinderLands, CutsceneAction.DrillDown, CutsceneAction.SwarmPours };
            foreach (CutsceneShot shot in _cutscene.Shots)
            {
                int kicks = 0;
                foreach (CutsceneCue cue in shot.Cues)
                {
                    if (cue.Kind == CueKind.Shake)
                    {
                        kicks++;
                        Assert.LessOrEqual(cue.Strength, _cutscene.CameraFeel.MaxKick, "a kick stronger than the camera allows");
                        Assert.Contains(shot.Action, impacts, $"{shot.Action} kicks the camera without an impact");
                    }
                }

                Assert.LessOrEqual(kicks, 1, $"{shot.Action} kicks the camera more than once");
            }

            CutsceneCameraFeel feel = _cutscene.CameraFeel;
            Assert.LessOrEqual(feel.MaxKick, 0.35f, "the kick cap is too strong to stay small");
            Assert.LessOrEqual(feel.DriftHertz, 0.2f, "a drift faster than this reads as shake");
            Assert.LessOrEqual(feel.Drift, 0.02f, "the drift is wide enough to see");
        }
    }
}
