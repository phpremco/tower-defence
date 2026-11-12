using System.IO;
using Game.Combat;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 49 smoke material fix: the URP default particle material drew nothing in the game view and is null in a player
    /// build, so every code-built particle system is dressed from the Blender particle atlas instead: the kit's systems glow
    /// by default, smoke and dust cover, a sprite system gets a material built from the atlas's covering one, and no script
    /// that builds a particle system of its own reaches for the default material.
    /// </summary>
    public sealed class ParticleMaterialTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string ScriptsFolder = "Assets/_Project/Scripts";
        private const string BuildsParticles = "AddComponent<ParticleSystem>()";
        private const string DefaultParticleMaterial = "defaultParticleMaterial";
        private const string KitFile = nameof(ParticleKit) + ".cs";

        [Test]
        public void KitSystems_WearTheAtlas_GlowByDefault_AndSmokeCovers()
        {
            MarkerArt art = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath).Markers;
            Assert.IsNotNull(art, "no marker art in the VFX library");
            bool had = Services.TryGet(out MarkerArt before);
            Services.Register(art);
            var root = new GameObject("ParticleMaterialTest");
            Material sprite = null;
            try
            {
                ParticleSystem spark = ParticleKit.Create(root.transform, "Spark", Vector3.zero, Color.white, 0.2f, 0.4f, 0.02f, 0.04f, 1f, 2f, 1f, 8);
                ParticleSystem dust = ParticleKit.UseCoveringCell(
                    ParticleKit.Create(root.transform, "Dust", Vector3.zero, Color.grey, 1f, 2f, 0.2f, 0.4f, 0.2f, 0.5f, -0.1f, 8), MarkerArt.ParticleCell.Mote);
                ParticleSystem smoke = ParticleKit.UseSmoke(ParticleKit.Create(root.transform, "Smoke", Vector3.zero, Color.grey, 1f, 2f, 0.2f, 0.4f, 0.2f, 0.5f, -0.1f, 8));
                AssertDressed(spark, art.ParticleMaterial(false));
                AssertDressed(dust, art.ParticleMaterial(true));
                Material puff = smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                Assert.AreSame(art.ParticleMaterial(true).shader, puff.shader, "smoke should be drawn with the atlas's particle shader");
                Assert.AreSame(SpriteKit.Puff(), puff.mainTexture, "smoke should wear the vapour puff");
                Assert.IsFalse(smoke.textureSheetAnimation.enabled, "the puff is a whole sprite, not an atlas cell");

                sprite = ParticleKit.SpriteMaterial(SpriteKit.Puff());
                Assert.IsNotNull(sprite, "no sprite material");
                Assert.AreSame(art.ParticleMaterial(true).shader, sprite.shader, "the sprite material should use the atlas's particle shader");
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (sprite != null)
                {
                    Object.DestroyImmediate(sprite);
                }

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

        [Test]
        public void NoScriptBuildingAParticleSystem_DrawsItWithTheDefaultParticleMaterial()
        {
            foreach (string file in Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == KitFile)
                {
                    continue;
                }

                string source = File.ReadAllText(file);
                if (source.Contains(BuildsParticles))
                {
                    Assert.IsFalse(source.Contains(DefaultParticleMaterial), $"{file} builds a particle system and uses the default particle material; dress it through ParticleKit");
                }
            }
        }

        private static void AssertDressed(ParticleSystem system, Material expected)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            Assert.AreSame(expected, renderer.sharedMaterial, $"{system.name}: material");
            Assert.AreNotSame(ParticleKit.FallbackMaterial, renderer.sharedMaterial, $"{system.name}: still on the default particle material");
            Assert.IsTrue(system.textureSheetAnimation.enabled, $"{system.name}: should pick a cell of the atlas");
        }
    }
}
