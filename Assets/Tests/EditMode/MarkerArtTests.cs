using System;
using Game.Combat;
using Game.Core;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The Blender markers, bars and projectiles (README round 25b) and the UI art (round 25) are complete and lie in the
    /// axes the views place them in: flat decals in XZ, bars facing the camera with the fill cut from the right,
    /// projectiles and beams along +Z; every icon type has its sprite and the skin wears the kit.
    /// </summary>
    public sealed class MarkerArtTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string SkinPath = "Assets/_Project/Settings/UiSkin.asset";
        private const float Flat = 0.001f;

        private static MarkerArt Art => AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath).Markers;

        [Test]
        public void Library_WearsEveryPiece()
        {
            Assert.IsNotNull(Art, "run Tools/Generate/Blender Markers");
            foreach (MarkerKind kind in (MarkerKind[])Enum.GetValues(typeof(MarkerKind)))
            {
                Assert.IsTrue(Art.Has(kind), $"no {kind}");
            }

            Assert.IsTrue(Art.Missile.IsComplete && Art.Scout.IsComplete, "missile or scout missing");
            Assert.Greater(Art.DebrisCount, Art.HotChunks);
            Assert.IsNotNull(Art.Particles(MarkerArt.ParticleCell.Mote));
            Assert.IsNotNull(Art.Particles(MarkerArt.ParticleCell.Drip));
        }

        [TestCase(MarkerKind.GlowPool)]
        [TestCase(MarkerKind.PlacementCursor)]
        [TestCase(MarkerKind.FreeCell)]
        [TestCase(MarkerKind.RangeBand)]
        [TestCase(MarkerKind.SlowField)]
        [TestCase(MarkerKind.RoadCue)]
        [TestCase(MarkerKind.HeroSpot)]
        public void Decals_LieFlatOnTheGround(MarkerKind kind)
        {
            Bounds bounds = Art.Mesh(kind).bounds;
            Assert.Less(bounds.size.y, Flat, $"{kind} is not flat in XZ");
            Assert.Greater(bounds.size.x, Flat);
            Assert.Greater(bounds.size.z, Flat);
        }

        [Test]
        public void Bars_FaceTheCameraAndShrinkFromTheRight()
        {
            Mesh fill = Art.Mesh(MarkerKind.BarFill);
            Assert.Less(fill.bounds.size.z, Flat, "the bar is not upright");
            Assert.Less(fill.normals[0].z, 0f, "the bar faces away from the camera");
            Vector3[] vertices = fill.vertices;
            Vector2[] along = fill.uv2;
            for (int i = 0; i < vertices.Length; i++)
            {
                Assert.AreEqual(vertices[i].x > 0f ? 1f : 0f, along[i].x, 0.01f, "UV2.x must run left (0) to right (1)");
            }

            Assert.Greater(Art.Mesh(MarkerKind.BarPlate).bounds.center.z, fill.bounds.center.z, "the plate must sit behind the line");
        }

        [Test]
        public void Projectiles_PointAlongPlusZ()
        {
            Assert.Greater(Art.Missile.SocketB.z, Art.Missile.SocketA.z, "the missile's nose is behind its exhaust");
            Assert.Greater(Art.Scout.SocketB.z, Art.Scout.SocketA.z, "the scout's lamp is behind its trail");
            Assert.AreEqual(0f, Art.Mesh(MarkerKind.ToolBeam).bounds.min.z, 0.02f, "the beam cone starts at its tip");
            Assert.Greater(Art.Mesh(MarkerKind.ToolBeam).bounds.max.z, 0.9f, "the beam cone runs along +Z");
            Mesh glow = Art.Mesh(MarkerKind.BoltGlow);
            for (int i = 0; i < glow.vertexCount; i++)
            {
                // README: U runs along the shell with the bright head at +U, which must lie forward (+Z).
                Assert.AreEqual(glow.vertices[i].z > 0f, glow.uv[i].x > 0.5f, "the glow's head points backwards");
            }
        }

        [Test]
        public void Skin_WearsTheBlenderKitAndEveryIcon()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            Assert.IsNotNull(skin.Art, "run Tools/Generate/Blender UI");
            Assert.AreEqual(skin.Art.Sprite("Panel"), skin.Panel);
            Assert.AreEqual(skin.Art.Sprite("Bar_Back"), skin.BarBack);
            Assert.Greater(skin.Panel.border.x, 0f, "the panel is not nine-sliced");
            foreach (IconType icon in (IconType[])Enum.GetValues(typeof(IconType)))
            {
                Assert.IsNotNull(skin.Art.Sprite(icon.ToString()), $"no icon for {icon}");
            }

            Assert.IsNotNull(skin.Art.Starfield);
            Assert.IsNotNull(skin.Art.Sprite("Dialogue_Panel"));
        }
    }
}
