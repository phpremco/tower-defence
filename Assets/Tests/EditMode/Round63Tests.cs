using Game.Core;
using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 63, Remco's second playtest of the opening: the base stands on levelled ground, Spencer braces against a tremor
    /// that is felt (louder rumble, no wind bed), the Queen climbs out of her own burrow, the brood gets no boss alarm in the
    /// intro, Spencer's angry line follows his startle, and the base shuttle never leaves the picture by a cut.
    /// </summary>
    public sealed class Round63Tests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string BaseArtPath = "Assets/_Project/Data/Art/Colony/BaseArt.asset";
        private const float Frame = 1f / 60f;
        // The reaction shot of round 44, and the most its angry beat may add (Remco: "keep the scene timing tight").
        private const float ReactionBefore = 5.5f;
        private const float MaxAngryAdds = 2.5f;
        // A tremor, not a shaking table: no stone lifts more than this (metres).
        private const float MaxStoneLift = 0.04f;
        // The levelled pad: no ground under the footprint stands above the base's own height by more than this (metres).
        private const float PadTolerance = 0.02f;

        private static CutsceneDefinition Opening() => AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath).Story.OpeningCutscene;

        private static CutsceneShot ShotOf(CutsceneAction action, int skip = 0)
        {
            foreach (CutsceneShot shot in Opening().Shots)
            {
                if (shot.Action == action && skip-- == 0)
                {
                    return shot;
                }
            }

            Assert.Fail($"the opening has no {action} shot");
            return default;
        }

        [Test]
        public void NoShotOfTheOpening_CuesTheBossAlarm()
        {
            foreach (CutsceneShot shot in Opening().Shots)
            {
                foreach (CutsceneCue cue in shot.Cues)
                {
                    Assert.AreNotEqual(CutsceneSound.BossAlarm, cue.Sound, $"{shot.Action} cues the boss alarm (Remco: no need in the intro)");
                }

                foreach (CutsceneLayer layer in shot.Layers)
                {
                    Assert.AreNotEqual(CutsceneSound.BossAlarm, layer.Sound, $"{shot.Action} holds the boss alarm");
                }
            }
        }

        [Test]
        public void TheQuakeShots_HoldNoWindBed()
        {
            foreach (CutsceneShot shot in new[] { ShotOf(CutsceneAction.GroundShivers), ShotOf(CutsceneAction.GroundShivers, 1), ShotOf(CutsceneAction.SwarmPours) })
            {
                foreach (CutsceneLayer layer in shot.Layers)
                {
                    Assert.AreNotEqual(CutsceneSound.WindPlain, layer.Sound, $"{shot.Action} still holds the wind bed under the quake");
                }
            }
        }

        [Test]
        public void TheQuakeRumble_IsLiftedAboveItsMeasuredGain_AndPeaksNearFull()
        {
            CutsceneSoundBank bank = Opening().Sounds;
            int slot = bank.IndexOf(CutsceneSound.QuakeLoop);
            Assert.GreaterOrEqual(slot, 0, "the bank has no quake rumble");
            CutsceneSoundSlot rumble = bank.Slots[slot];
            Assert.Greater(rumble.TrimOrOne, 1f, "the quake rumble keeps its measured gain only (Remco: only a faint rumble)");
            float full = rumble.Clips.Gain * rumble.TrimOrOne;
            Assert.That(full, Is.InRange(0.9f, 1.05f), "at full level the rumble should sit just at the source's ceiling");
            Assert.AreEqual(1f, ShotOf(CutsceneAction.GroundShivers).QuakeHold, 1e-4f, "the first shiver shot holds the rumble at full");
        }

        [Test]
        public void TheShiver_IsATremor_AndSpencerBraces()
        {
            CutsceneCast cast = Opening().Cast;
            ShiverTremor tremor = cast.ShiverTremor;
            Assert.LessOrEqual(tremor.BuzzHeight + tremor.HopHeight, MaxStoneLift, "the stones jump like an extreme earthquake");
            Assert.Greater(tremor.LensRumble, 0f, "the lens does not feel the tremor");
            QuakeBrace brace = cast.Brace;
            Assert.Greater(brace.KneeDegrees, 0f, "his knees do not give");
            Assert.Greater(brace.SwayDegrees, 0f, "he does not sway");
            Assert.Greater(brace.ArmOutDegrees, 0f, "his arms stay at his sides");
            for (float t = 0f; t < 6f; t += Frame)
            {
                float step = Mathf.Abs(brace.Sway(t + Frame) - brace.Sway(t)) * brace.SwayDegrees;
                Assert.Less(step, 1f, $"the sway jerks {step:0.00} deg in one frame at {t:0.00} s");
            }
        }

        [Test]
        public void SpencersAngryLine_FollowsHisStartle_InHisOwnVoice()
        {
            CutsceneShot reaction = ShotOf(CutsceneAction.MercerReacts);
            CutsceneCast cast = Opening().Cast;
            Assert.IsNotNull(reaction.Dialogue, "the reaction shot has no angry line");
            Assert.AreEqual(1, reaction.Dialogue.Lines.Count);
            DialogueLine line = reaction.Dialogue.Lines[0];
            Assert.AreEqual("Speaker_Mercer", line.Speaker.name, "the angry line must be Spencer's");
            Assert.LessOrEqual(line.CharacterCount, 70, "keep the line short");
            Assert.LessOrEqual(reaction.Seconds - ReactionBefore, MaxAngryAdds + 1e-4f, "the angry beat adds more than 2.5 s");
            Assert.AreEqual(ReactionBefore + cast.AngrySeconds, reaction.Seconds, 1e-4f, "the shot does not make room for the angry beat");
            Assert.Greater(cast.AngrySeconds, 0f);
            Assert.That(reaction.DialogueDelay, Is.InRange(3.5f, 3.6f + cast.AngrySeconds * 0.5f), "the line should start as he points at them");
        }

        [Test]
        public void TheQueen_ClimbsOutOfHerBurrow_SmoothlyAndFromOutOfSight()
        {
            QueenBurrow burrow = Opening().Cast.QueenBurrow;
            burrow.Climb(0f, out float depth0, out float forward0, out float pitch0);
            burrow.Climb(1f, out float depth1, out float forward1, out float pitch1);
            Assert.AreEqual(burrow.ClimbDepth, depth0, 1e-4f, "she must start deep in the maw");
            Assert.Greater(pitch0, 20f, "she must start nose up, clawing out");
            Assert.AreEqual(0f, depth1, 1e-4f, "she must end on the ground");
            Assert.AreEqual(0f, pitch1, 1e-3f, "she must end level");
            Assert.AreEqual(burrow.ClimbForward, forward1, 1e-4f);
            float du = Frame / burrow.ClimbSeconds;
            for (float u = 0f; u < 1f; u += du)
            {
                burrow.Climb(u, out float d0, out float f0, out float p0);
                burrow.Climb(u + du, out float d1, out float f1, out float p1);
                Assert.LessOrEqual(d1, d0 + 1e-5f, $"she sinks back at {u:0.00}");
                Assert.Less(Mathf.Abs(d1 - d0) + Mathf.Abs(f1 - f0), 0.06f, $"she jumps at {u:0.00}");
                Assert.Less(Mathf.Abs(p1 - p0), 2f, $"her pitch snaps at {u:0.00}");
            }

            float burst = burrow.OpenAt + FieldSwarm.BurstAt;
            bool cued = false;
            foreach (CutsceneCue cue in ShotOf(CutsceneAction.SwarmPours).Cues)
            {
                cued |= cue.Sound == CutsceneSound.GroundTear && Mathf.Abs(cue.At - burst) < 0.05f;
            }

            Assert.IsTrue(cued, "her burrow bursts without the ground tearing");
        }

        [Test]
        public void TheBase_StandsOnLevelledGround_InEveryLevel()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            foreach (MapNode node in catalog.Nodes)
            {
                LevelDefinition level = node.Level;
                BaseArtDefinition art = level != null && level.Theme != null ? level.Theme.BaseArt : null;
                if (art == null || art.Model == null)
                {
                    continue;
                }

                var field = new TerrainField(level, new RoadShape(level));
                Vector3 centre = LevelVisualBuilder.BasePosition(level);
                Quaternion facing = LevelVisualBuilder.BaseFacing(level);
                for (float x = art.FootprintMin.x; x <= art.FootprintMax.x; x += 0.25f)
                {
                    for (float z = art.FootprintMin.y; z <= art.FootprintMax.y; z += 0.25f)
                    {
                        Vector3 point = centre + facing * new Vector3(x, 0f, z);
                        float above = field.HeightAt(point.x, point.z) - centre.y;
                        Assert.LessOrEqual(above, PadTolerance, $"{level.name}: the ground stands {above:0.00} m over the base's plinth at ({x:0.0}, {z:0.0})");
                    }
                }
            }
        }

        [Test]
        public void TheShuttle_ShrinksAwayOnItsClimb_AndRoars()
        {
            var art = AssetDatabase.LoadAssetAtPath<BaseArtDefinition>(BaseArtPath);
            Assert.IsNotNull(art.ShuttleRoar, "the shuttle launches in silence");
            Assert.Greater(art.ShuttleSpoolSeconds, 0f, "no spool-up before lift-off");
            Assert.Greater(Mathf.Abs(art.ShuttleTurnDegrees), 0f, "it climbs straight up instead of turning away");
            Assert.Greater(art.ShuttleShrinkShare, 0.3f, "a shrink over a short share of the climb reads as vanishing");
            Assert.Greater(art.ShuttleClimb, 10f, "it never gets far enough to leave");
            Assert.Greater(art.ShuttleFlightSeconds, 5f, "the climb is too quick to read as a launch");
        }
    }
}
