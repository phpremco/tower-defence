using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 55: the Shattered and its ruin gate, both measured from the art. The gate's opening (open, at its data scale)
    /// clears the giant's walking body with margin, so it could have come through it; and the gate stands far enough behind
    /// the spot the giant appears on that its whole body, roaring, stays clear of the iris while the petals still fold. Round 55b: in its arena the gate faces the camera and no tower
    /// can stand where it rises.
    /// </summary>
    public sealed class RuinGateFitTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string GiantPath = "Assets/_Project/Data/Enemies/Shattered.asset";
        private const string ArenaPath = "Assets/_Project/Data/Levels/Arena/Arena_Ruins.asset";
        // How far the gate may turn from facing the camera (round 55b: side on it read as a slab), and the longest straight walk.
        private const float MaxGateTurnFromCamera = 20f;
        private const int MaxLegCells = 4;
        private const int SamplesPerClip = 16;
        // Headroom over its tallest walking pose and room each side of its widest, in metres.
        private const float HeadroomShare = 1.15f;
        private const float SideRoom = 0.1f;
        // Height bands the opening's width is checked in, and the lowest one (the threshold and its groove lie below it).
        private const float Band = 0.25f;
        private const float LowestBand = 0.3f;
        // Between the giant's tail and the iris (the petals fold in the gate's middle plane and have some thickness).
        private const float IrisClearance = 0.3f;
        // A start point for the roof ray: above the threshold, below any soffit.
        private const float RoofRayStart = 1f;

        [Test]
        public void Opening_ClearsTheWalkingGiant_WithMargin()
        {
            (BossFxArt art, EnemyDefinition giant) = Load();
            List<Vector3> body = GiantPoints(giant, ModelClipRole.Move);
            List<Vector3> gate = GateTriangles(art);
            float height = 0f;
            foreach (Vector3 point in body)
            {
                height = Mathf.Max(height, point.y);
            }

            float roof = RoofRayStart + Cast(gate, new Vector3(0f, RoofRayStart, 0f), Vector3.up);
            TestContext.WriteLine($"opening roof {roof:0.00} m, walking giant {height:0.00} m");
            Assert.GreaterOrEqual(roof, height * HeadroomShare, $"the opening ({roof:0.00} m) is not {HeadroomShare:0.00}x the walking giant ({height:0.00} m)");
            for (float low = LowestBand; low < height; low += Band)
            {
                float halfWidth = 0f;
                foreach (Vector3 point in body)
                {
                    if (point.y >= low && point.y < low + Band)
                    {
                        halfWidth = Mathf.Max(halfWidth, Mathf.Abs(point.x));
                    }
                }

                foreach (float y in new[] { low, low + Band })
                {
                    float clear = Mathf.Min(Cast(gate, new Vector3(0f, y, 0f), Vector3.left), Cast(gate, new Vector3(0f, y, 0f), Vector3.right));
                    TestContext.WriteLine($"at {y:0.00} m: opening {clear:0.00} m each side, giant {halfWidth:0.00} m");
                    Assert.GreaterOrEqual(clear, halfWidth + SideRoom, $"at {y:0.00} m the opening is {clear:0.00} m each side, the giant {halfWidth:0.00} m");
                }
            }
        }

        [Test]
        public void GateSetBack_KeepsTheRoaringGiantClearOfTheIris()
        {
            (_, EnemyDefinition giant) = Load();
            float rear = 0f;
            foreach (ModelClipRole role in new[] { ModelClipRole.Roar, ModelClipRole.Idle, ModelClipRole.Move })
            {
                foreach (Vector3 point in GiantPoints(giant, role))
                {
                    // The export's head looks down -Z (EnemyView turns the model root onto its heading), so behind is +Z.
                    rear = Mathf.Max(rear, point.z);
                }
            }

            float setBack = giant.Boss.Split.GateSetBack;
            TestContext.WriteLine($"tail reach {rear:0.00} m, set-back {setBack:0.00} m");
            Assert.GreaterOrEqual(setBack - rear, IrisClearance, $"its tail reaches {rear:0.00} m behind it; the gate stands {setBack:0.00} m back");
        }

        // Round 55b: the giant walks straight out of the gate towards the camera (so its opening is seen, not its side), and no
        // build cell lies where the gate will rise.
        [Test]
        public void ArenaRuins_GateFacesTheCamera_AndNoTowerStandsWhereItRises()
        {
            (BossFxArt art, EnemyDefinition giant) = Load();
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(ArenaPath);
            Assert.IsNotNull(level, "no Arena_Ruins");
            Vector2Int entrance = level.BossEntrance.Cell;
            Vector2Int exit = GroundEntranceRules.TowardCamera(level.Camera.YawDegrees);
            float yaw = level.Camera.YawDegrees * Mathf.Deg2Rad;
            float off = Vector2.Angle(exit, new Vector2(-Mathf.Sin(yaw), -Mathf.Cos(yaw)));
            Assert.LessOrEqual(off, MaxGateTurnFromCamera, $"the gate faces {off:0} degrees away from the camera");
            int leg = 1;
            while (leg <= MaxLegCells && !level.Road.IsRoad(entrance + exit * leg))
            {
                leg++;
            }

            Assert.LessOrEqual(leg, MaxLegCells, $"no straight walk from {entrance} towards the camera reaches the road");
            Vector2 halfSize = GroundEntranceRules.GateHalfSize(art.RuinGate.Model.transform, art.RuinGateScale);
            var build = new BuildGrid(level);
            foreach (Vector2Int cell in GroundEntranceRules.GateCells(level.Grid, entrance, exit, giant.Boss.Split.GateSetBack, halfSize))
            {
                Assert.IsFalse(build.CanBuild(cell), $"{cell} under the gate is a build cell");
            }
        }

        private static (BossFxArt Art, EnemyDefinition Giant) Load()
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            Assert.IsNotNull(library?.BossFx, "no boss FX art");
            Assert.IsNotNull(library.BossFx.RuinGate, "no ruin gate");
            var giant = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(GiantPath);
            Assert.IsNotNull(giant?.Art, "no Shattered art");
            Assert.IsNotNull(giant.Boss?.Split, "the Shattered has no split power");
            return (library.BossFx, giant);
        }

        // Every skinned vertex of every sampled pose of a clip, at the giant's play scale, in the model's own frame.
        private static List<Vector3> GiantPoints(EnemyDefinition giant, ModelClipRole role)
        {
            ModelArtDefinition art = giant.Art;
            int index = art.IndexOf(role);
            Assert.GreaterOrEqual(index, 0, $"the Shattered has no {role} clip");
            AnimationClip clip = art.Clips[index].Clip;
            var points = new List<Vector3>();
            GameObject probe = Object.Instantiate(art.Model);
            var baked = new Mesh();
            try
            {
                probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                probe.transform.localScale = Vector3.one * giant.Scale * art.Scale;
                SkinnedMeshRenderer[] renderers = probe.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (int i = 0; i < SamplesPerClip; i++)
                {
                    clip.SampleAnimation(probe, clip.length * i / SamplesPerClip);
                    foreach (SkinnedMeshRenderer renderer in renderers)
                    {
                        renderer.BakeMesh(baked, true);
                        Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
                        foreach (Vector3 vertex in baked.vertices)
                        {
                            points.Add(toWorld.MultiplyPoint3x4(vertex));
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(baked);
                Object.DestroyImmediate(probe);
            }

            return points;
        }

        // The open gate (Opened_Idle) at its data scale as world triangles, its threshold centre at the origin.
        private static List<Vector3> GateTriangles(BossFxArt art)
        {
            ModelArtDefinition gate = art.RuinGate;
            AnimationClip idle = gate.Clips[gate.IndexOf(ModelClipRole.Idle)].Clip;
            var triangles = new List<Vector3>();
            GameObject probe = Object.Instantiate(gate.Model);
            var baked = new Mesh();
            try
            {
                probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                probe.transform.localScale = Vector3.one * art.RuinGateScale;
                idle.SampleAnimation(probe, 0f);
                foreach (SkinnedMeshRenderer renderer in probe.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.BakeMesh(baked, true);
                    Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
                    Vector3[] vertices = baked.vertices;
                    foreach (int corner in baked.triangles)
                    {
                        triangles.Add(toWorld.MultiplyPoint3x4(vertices[corner]));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(baked);
                Object.DestroyImmediate(probe);
            }

            return triangles;
        }

        // Distance along a ray to the nearest triangle (Moller-Trumbore), or infinity when it hits none.
        private static float Cast(List<Vector3> triangles, Vector3 origin, Vector3 direction)
        {
            const float parallel = 1e-7f;
            float best = float.PositiveInfinity;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 e1 = triangles[i + 1] - triangles[i];
                Vector3 e2 = triangles[i + 2] - triangles[i];
                Vector3 h = Vector3.Cross(direction, e2);
                float a = Vector3.Dot(e1, h);
                if (a > -parallel && a < parallel)
                {
                    continue;
                }

                float f = 1f / a;
                Vector3 s = origin - triangles[i];
                float u = f * Vector3.Dot(s, h);
                if (u < 0f || u > 1f)
                {
                    continue;
                }

                Vector3 q = Vector3.Cross(s, e1);
                float v = f * Vector3.Dot(direction, q);
                if (v < 0f || u + v > 1f)
                {
                    continue;
                }

                float distance = f * Vector3.Dot(e2, q);
                if (distance > 0f && distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }
    }
}
