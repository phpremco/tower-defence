using System.IO;
using Game.Combat;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 51: no effect may start or end with a hard edge. Every line and trail dressed by <see cref="LineKit"/> tapers from
    /// nothing at its source to nothing at its far end, with round caps; a line laid by the kit really is drawn that way (its
    /// baked mesh has no width at either end); the beam profile fades out along its length and the flare sprite before its
    /// rim; and every runtime script that builds a line or trail of its own goes through the kit.
    /// </summary>
    public sealed class LineEndsTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string ScriptsFolder = "Assets/_Project/Scripts";
        private const string EditorFolder = "Editor";
        private const float NearZero = 0.02f;
        // The profile fades over its first and last few texels, so the outermost texel keeps a trace of light.
        private const float EdgeTexel = 0.1f;
        private const float BeamLength = 10f;
        private const float BeamWidth = 0.3f;
        // Lines that are not beams: a closed range ring has no ends, and the cutscene's lines wear their own painted sprites.
        private static readonly string[] Exempt = { "RangeIndicator.cs", "SpaceKit.cs" };

        [Test]
        public void EndsCurve_StartsAndEndsAtNothing_AndIsFullInTheMiddle()
        {
            AnimationCurve curve = LineKit.EndsCurve(0.1f, 0.2f);
            Assert.AreEqual(0f, curve.Evaluate(0f), NearZero, "a line must grow from nothing at its source");
            Assert.AreEqual(0f, curve.Evaluate(1f), NearZero, "a line must thin to nothing at its far end");
            Assert.AreEqual(1f, curve.Evaluate(0.5f), NearZero, "a line must have its full width between the tapers");
            for (float t = 0f; t <= 0.1f; t += 0.01f)
            {
                Assert.LessOrEqual(curve.Evaluate(t), curve.Evaluate(t + 0.01f) + 1e-4f, $"the source taper must only grow (t {t:0.00})");
            }
        }

        [Test]
        public void KitLinesAndTrails_TaperAtBothEnds_WithRoundCaps()
        {
            WithArt(root =>
            {
                LineRenderer line = LineKit.UseBeam(root.AddComponent<LineRenderer>());
                var holder = new GameObject("Trail");
                holder.transform.SetParent(root.transform, false);
                TrailRenderer trail = LineKit.UseBeam(holder.AddComponent<TrailRenderer>());
                AssertTapered(line.widthCurve, "line");
                AssertTapered(trail.widthCurve, "trail");
                Assert.GreaterOrEqual(line.numCapVertices, LineKit.CapVertices, "a line with width needs round caps");
                Assert.GreaterOrEqual(trail.numCapVertices, LineKit.CapVertices, "a trail with width needs round caps");
            });
        }

        [Test]
        public void StraightLine_IsDrawnWithoutWidthAtEitherEnd()
        {
            WithArt(root =>
            {
                var eye = new GameObject("Eye").AddComponent<Camera>();
                eye.transform.SetParent(root.transform, false);
                eye.transform.SetPositionAndRotation(new Vector3(BeamLength * 0.5f, 10f, 0f), Quaternion.LookRotation(Vector3.down, Vector3.forward));
                eye.orthographic = true;
                LineRenderer line = LineKit.UseBeam(root.AddComponent<LineRenderer>());
                line.useWorldSpace = true;
                line.widthMultiplier = BeamWidth;
                LineKit.Straight(line, Vector3.zero, Vector3.right * BeamLength);
                Assert.AreEqual(LineKit.LinePoints, line.positionCount, "a straight beam is laid through the kit's points so it can taper");

                var mesh = new Mesh();
                try
                {
                    line.BakeMesh(mesh, eye, false);
                    float startWidth = 0f, endWidth = 0f, middleWidth = 0f, taperWidth = 0f;
                    float inTaper = LineKit.SourceTaperWidths * BeamWidth * 0.5f;
                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        float across = Mathf.Abs(vertex.z) * 2f;
                        if (vertex.x < NearZero)
                        {
                            startWidth = Mathf.Max(startWidth, across);
                        }
                        else if (vertex.x > BeamLength - NearZero)
                        {
                            endWidth = Mathf.Max(endWidth, across);
                        }
                        else if (vertex.x < inTaper)
                        {
                            taperWidth = Mathf.Max(taperWidth, across);
                        }
                        else if (Mathf.Abs(vertex.x - BeamLength * 0.5f) < BeamLength * 0.2f)
                        {
                            middleWidth = Mathf.Max(middleWidth, across);
                        }
                    }

                    Assert.Less(startWidth, BeamWidth * 0.1f, "the beam starts square at its source");
                    Assert.Less(endWidth, BeamWidth * 0.1f, "the beam ends square at its target");
                    // The taper is measured along the line's length, not by point index: half way into it the beam is not yet full.
                    Assert.Greater(taperWidth, 0f, "no point inside the source taper");
                    Assert.Less(taperWidth, BeamWidth * 0.95f, "the source taper ends too soon: the width curve is not read along the length");
                    Assert.AreEqual(BeamWidth, middleWidth, BeamWidth * 0.1f, "the beam should have its full width in the middle");
                }
                finally
                {
                    Object.DestroyImmediate(mesh);
                }
            });
        }

        [Test]
        public void FlaredBeam_StartsAtHalfWidth_AtFullStrength_UnderItsGlows()
        {
            WithArt(root =>
            {
                LineRenderer line = LineKit.UseFlaredBeam(root.AddComponent<LineRenderer>());
                line.widthMultiplier = BeamWidth;
                LineKit.Straight(line, Vector3.zero, Vector3.right * BeamLength, LineKit.FlaredEndWidth);
                AnimationCurve width = line.widthCurve;
                Assert.AreEqual(LineKit.FlaredEndWidth, width.Evaluate(0f), NearZero, "a flared beam starts at half width under its source glow");
                Assert.AreEqual(LineKit.FlaredEndWidth, width.Evaluate(1f), NearZero, "a flared beam ends at half width under its impact glow");
                Assert.AreEqual(1f, width.Evaluate(0.5f), NearZero, "a flared beam should be full width in the middle");
                Assert.AreSame(SpriteKit.FlaredBeam(), line.sharedMaterial.mainTexture, "a flared beam wears the profile without end fades");
            });

            Texture2D flared = SpriteKit.FlaredBeam();
            int middleRow = flared.height / 2;
            Assert.Greater(flared.GetPixel(0, middleRow).a, 0.4f, "the flared profile must not fade in from nothing, or a dark gap opens after the glow");
            Assert.Greater(flared.GetPixel(3, middleRow).a, 0.9f, "the flared profile should be full a few texels in");
        }

        [Test]
        public void BeamProfile_FadesOutAlongItsLength_AndTheFlareBeforeItsRim()
        {
            Texture2D beam = SpriteKit.Beam();
            int middleRow = beam.height / 2;
            Assert.Less(beam.GetPixel(0, middleRow).a, EdgeTexel, "the beam profile is square at its start");
            Assert.Less(beam.GetPixel(beam.width - 1, middleRow).a, EdgeTexel, "the beam profile is square at its end");
            Assert.Less(beam.GetPixel(0, middleRow).a, beam.GetPixel(2, middleRow).a, "the beam profile should grow from its start");
            Assert.Greater(beam.GetPixel(beam.width / 2, middleRow).a, 0.9f, "the beam profile should be full in the middle");

            Texture2D glow = SpriteKit.Glow();
            Assert.Greater(glow.GetPixel(glow.width / 2, glow.height / 2).r, 0.9f, "the flare should have a white-hot point");
            for (int i = 0; i < glow.width; i++)
            {
                Assert.Less(glow.GetPixel(i, 0).a, NearZero, "the flare shows a rim at its top edge");
                Assert.Less(glow.GetPixel(0, i).a, NearZero, "the flare shows a rim at its left edge");
            }
        }

        [Test]
        public void EveryRuntimeLineOrTrail_IsShapedByTheLineKit()
        {
            foreach (string file in Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (file.Replace('\\', '/').Contains("/" + EditorFolder + "/") || System.Array.IndexOf(Exempt, name) >= 0)
                {
                    continue;
                }

                string source = File.ReadAllText(file);
                bool buildsLine = source.Contains("AddComponent<LineRenderer>()") || source.Contains("AddComponent<TrailRenderer>()");
                if (buildsLine)
                {
                    Assert.IsTrue(source.Contains("LineKit.UseBeam") || source.Contains("LineKit.UseFlaredBeam") || source.Contains("LineKit.ShapeEnds"), $"{name} builds a line or trail without LineKit's tapered ends");
                }
            }
        }

        private static void AssertTapered(AnimationCurve curve, string what)
        {
            Assert.AreEqual(0f, curve.Evaluate(0f), NearZero, $"the {what} starts square");
            Assert.AreEqual(0f, curve.Evaluate(1f), NearZero, $"the {what} ends square");
            Assert.AreEqual(1f, curve.Evaluate(0.5f), NearZero, $"the {what} should be full width in the middle");
        }

        private static void WithArt(System.Action<GameObject> test)
        {
            MarkerArt art = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath).Markers;
            Assert.IsNotNull(art, "no marker art in the VFX library");
            bool had = Services.TryGet(out MarkerArt before);
            Services.Register(art);
            var root = new GameObject("LineEndsTest");
            try
            {
                test(root);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (had)
                {
                    Services.Register(before);
                }
                else
                {
                    Services.Unregister<MarkerArt>();
                }
            }
        }
    }
}
