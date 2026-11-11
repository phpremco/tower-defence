using System.Collections.Generic;
using Game.Core;
using Game.Editor;
using Game.Hero;
using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 40, Spencer's acting pass for the opening: the six acting clips arrive by role with their root offsets and
    /// face tracks from the Blender report, the face is driven from code after the sampled clip (the FBX writes zero shape
    /// curves), the landed capsule is shown at the scale the exit clip was keyed for, and the slow lower ends resolved.
    /// </summary>
    public sealed class SpencerActingTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string ReportPath = "Assets/_Project/Art/Models/Blender/Mercer/Mercer_report.json";
        private const string FaceMesh = "Mercer_Face";
        private const string FarFaceMesh = "Mercer_Face_Far";
        private const float Tolerance = 0.001f;
        private static readonly string[] FaceShapes = { "Blink", "Fear", "Surprise", "Determined", "Breath", "LookL", "LookR" };

        private static readonly ModelClipRole[] ActingRoles =
        {
            ModelClipRole.CapsuleExit, ModelClipRole.StartleBackstep, ModelClipRole.DrawAimNervous,
            ModelClipRole.AimHoldNervous, ModelClipRole.TurnLookLeft, ModelClipRole.TurnLookRight,
        };

        private CutsceneCast _cast;
        private readonly List<Object> _made = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            _cast = catalog.Story.OpeningCutscene.Cast;
            Assume.That(_cast.Hero != null && _cast.Hero.Art != null && _cast.Hero.Art.Body != null, "no imported commander");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                if (made != null)
                {
                    Object.DestroyImmediate(made);
                }
            }

            _made.Clear();
        }

        [Test]
        public void AFaceCurve_EasesBetweenItsKeys_AndHoldsBeyondThem()
        {
            var curve = new FaceCurve("Fear", new Vector2(1f, 0f), new Vector2(2f, 1f));
            Assert.AreEqual(0f, curve.Evaluate(0f), Tolerance, "before the first key it holds the first weight");
            Assert.AreEqual(0.5f, curve.Evaluate(1.5f), Tolerance, "halfway the smoothstep is at half");
            Assert.Less(curve.Evaluate(1.1f), 0.1f, "it eases in, not linearly");
            Assert.AreEqual(1f, curve.Evaluate(5f), Tolerance, "after the last key it holds the last weight");
            Assert.IsTrue(curve.IsValid());
            Assert.IsFalse(new FaceCurve("Fear", new Vector2(1f, 0f), new Vector2(0.5f, 1.2f)).IsValid(), "keys out of order or above 1 must be flagged");
        }

        [Test]
        public void TheActingClips_ArriveByRole_WithTheirRootOffsetsAndFaces()
        {
            HeroArtDefinition art = _cast.Hero.Art;
            foreach (ModelClipRole role in ActingRoles)
            {
                Assert.GreaterOrEqual(art.Body.IndexOf(role), 0, $"the body has no {role} clip");
                HeroActingClip acting = art.ActingFor(role);
                Assert.IsNotNull(acting, $"no acting data for {role}");
                Assert.Greater(acting.Face.Length, 0, $"{role} has no face tracks");
                foreach (FaceCurve curve in acting.Face)
                {
                    Assert.IsTrue(System.Array.IndexOf(FaceShapes, curve.Shape) >= 0, $"{role} drives an unknown shape {curve.Shape}");
                    Assert.IsTrue(curve.IsValid(), $"{role} {curve.Shape} has bad keys");
                }
            }

            ModelClip hold = art.Body.Clips[art.Body.IndexOf(ModelClipRole.AimHoldNervous)];
            Assert.IsTrue(hold.Clip.isLooping, "the nervous hold must loop while the gate line stays open");
            Assert.AreEqual(0.5f, art.ActingFor(ModelClipRole.StartleBackstep).StartBack, 0.01f, "the startle backs 0.5 m away");
            Assert.AreEqual(-90f, art.ActingFor(ModelClipRole.TurnLookLeft).StartTurn, 0.01f, "the left turn is 90 degrees to his left");
            Assert.AreEqual(90f, art.ActingFor(ModelClipRole.TurnLookRight).StartTurn, 0.01f);
            Assert.Greater(art.Body.Clips[art.Body.IndexOf(ModelClipRole.DrawAimNervous)].EventSeconds, 0f, "the draw has no grab moment for the pistol");
        }

        [Test]
        public void BothFaces_CarryEveryShape()
        {
            int faces = 0;
            foreach (GameObject model in new[] { _cast.Hero.Art.Body.Model })
            {
                foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.name != FaceMesh && renderer.name != FarFaceMesh)
                    {
                        continue;
                    }

                    faces++;
                    foreach (string shape in FaceShapes)
                    {
                        Assert.GreaterOrEqual(renderer.sharedMesh.GetBlendShapeIndex(shape), 0, $"{renderer.name} lacks {shape}");
                    }
                }
            }

            Assert.AreEqual(2, faces, "the near face and its far LOD should both be there");
        }

        [Test]
        public void TheFace_IsWrittenAfterTheSampledClip_AndBlinksOnTheShotClock()
        {
            var parent = new GameObject("ActingTest");
            _made.Add(parent);
            var rig = new CutsceneHeroRig(parent.transform, _cast.Hero);
            SkinnedMeshRenderer face = Face(parent);
            int fear = face.sharedMesh.GetBlendShapeIndex("Fear");
            int blink = face.sharedMesh.GetBlendShapeIndex("Blink");
            float quiet = QuietMoment(rig, face, blink);

            // The startle's fear peaks (1.0) at 0.67 s; the clip writes its zero curve first, the face goes on after it.
            const float peak = 0.667f;
            rig.PoseClip(Vector3.zero, 0f, ModelClipRole.StartleBackstep, peak, false, false);
            rig.Face(ModelClipRole.StartleBackstep, peak);
            rig.ApplyFace(quiet);
            Assert.AreEqual(Track(ModelClipRole.StartleBackstep, "Fear").Evaluate(peak) * 100f, face.GetBlendShapeWeight(fear), 0.5f, "the fear track does not reach the face");

            // A pose without tracks leaves a neutral face, and somewhere in every blink gap the eyes close.
            rig.PoseClip(Vector3.zero, 0f, ModelClipRole.Idle, 0f, true, false);
            rig.ApplyFace(quiet);
            Assert.AreEqual(0f, face.GetBlendShapeWeight(fear), Tolerance, "the face kept the last pose's fear");
            float closed = 0f;
            Vector2 every = _cast.Hero.Art.BlinkEvery;
            for (float t = 0f; t < (every.x + every.y) * 0.5f + 0.1f; t += 0.02f)
            {
                rig.ApplyFace(t);
                closed = Mathf.Max(closed, face.GetBlendShapeWeight(blink));
            }

            Assert.Greater(closed, 50f, "no blink in a whole blink gap");
        }

        [Test]
        public void TheLandedCapsule_IsShownAtTheScaleTheExitClipWasKeyedFor()
        {
            Dictionary<string, object> hatch = BlenderJson.Object(BlenderJson.Field(BlenderJson.Object(BlenderJson.Field(BlenderReport.Load(ReportPath).Root(), "acting")), "hatch"));
            Assert.IsNotNull(hatch, "the Mercer report has no acting hatch block");
            float keyedFor = BlenderJson.Number(BlenderJson.Field(hatch, "capsule_scale"), 1f);
            Assert.AreEqual(keyedFor, _cast.LandedCapsuleScale, 0.001f, "Capsule_Exit was keyed for another capsule scale (build_mercer_acting.py CAPSULE_SCALE)");

            // The sill the clip stands him on is the capsule's ramp hinge at that scale.
            var parent = new GameObject("CapsuleTest");
            _made.Add(parent);
            parent.transform.localScale = Vector3.one * _cast.LandedCapsuleScale;
            ArtModel capsule = ArtModel.CreateStill(parent.transform, _cast.CapsuleArt);
            Transform ramp = capsule.Find("Ramp");
            Assert.IsNotNull(ramp, "the capsule has no Ramp bone");
            Assert.AreEqual(BlenderJson.Number(BlenderJson.Field(hatch, "sill_height_m")), ramp.position.y, 0.02f, "the clip's sill does not meet the capsule's hatch");
        }

        [Test]
        public void TheSlowLower_DrainsTheFear_AndEndsResolved()
        {
            Assert.That(_cast.LowerSpeed, Is.InRange(0.3f, 0.9f), "he should lower the weapon slower than the clip's own pace");
            Assert.Greater(_cast.AimFadeSeconds, 0f, "the aim must cross-fade into the lower, not pop");
            float fear = 0f, resolve = 0f;
            foreach (FaceCurve curve in _cast.LowerFace)
            {
                Assert.IsTrue(curve.IsValid(), $"lower face {curve.Shape} has bad keys");
                fear = curve.Shape == "Fear" ? curve.Evaluate(10f) : fear;
                resolve = curve.Shape == "Determined" ? curve.Evaluate(10f) : resolve;
            }

            Assert.AreEqual(0f, fear, Tolerance, "the fear should be gone once he has lowered the weapon");
            Assert.Greater(resolve, 0.5f, "he should end the opening resolved");
        }

        private static SkinnedMeshRenderer Face(GameObject root)
        {
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == FaceMesh)
                {
                    return renderer;
                }
            }

            Assert.Fail("the cutscene figure has no face mesh");
            return null;
        }

        private FaceCurve Track(ModelClipRole role, string shape)
        {
            foreach (FaceCurve curve in _cast.Hero.Art.ActingFor(role).Face)
            {
                if (curve.Shape == shape)
                {
                    return curve;
                }
            }

            Assert.Fail($"{role} has no {shape} track");
            return null;
        }

        // A shot moment with the eyes open (no blink falls on it), so a weight read then is the tracks' alone.
        private static float QuietMoment(CutsceneHeroRig rig, SkinnedMeshRenderer face, int blink)
        {
            for (float t = 0f; t < 10f; t += 0.05f)
            {
                rig.ApplyFace(t);
                if (face.GetBlendShapeWeight(blink) < 0.01f)
                {
                    return t;
                }
            }

            Assert.Fail("the eyes never stay open");
            return 0f;
        }
    }
}
