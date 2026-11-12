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
    /// that builds a particle system of its own reaches for the default material. Round 50 adds lines and trails (the beam
    /// profile from the atlas) and bans every URP default material from runtime code.
    /// </summary>
    public sealed class ParticleMaterialTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string ScriptsFolder = "Assets/_Project/Scripts";
        private const string BuildsParticles = "AddComponent<ParticleSystem>()";
        private const string DefaultParticleMaterial = "defaultParticleMaterial";
        private const string KitFile = nameof(ParticleKit) + ".cs";
        private const string EditorFolder = "Editor";
        private static readonly string[] UrpDefaults = { "defaultParticleMaterial", "defaultMaterial", "defaultLineMaterial" };
        private static readonly int[] LitStockProperties = { Shader.PropertyToID("_BaseColor"), Shader.PropertyToID("_Color"), Shader.PropertyToID("_SpecColor") };

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

        // Round 50: lines and trails drew with the default particle material too (flat strips in the game view, null in a build).
        [Test]
        public void CodeBuiltLinesAndTrails_WearTheBeamProfile_FromTheAtlas()
        {
            MarkerArt art = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath).Markers;
            Assert.IsNotNull(art, "no marker art in the VFX library");
            bool had = Services.TryGet(out MarkerArt before);
            Services.Register(art);
            var root = new GameObject("LineMaterialTest");
            Material additive = null;
            Material picture = null;
            try
            {
                LineRenderer line = LineKit.UseBeam(root.AddComponent<LineRenderer>());
                var trailHolder = new GameObject("Trail");
                trailHolder.transform.SetParent(root.transform, false);
                TrailRenderer trail = LineKit.UseBeam(trailHolder.AddComponent<TrailRenderer>());
                foreach (Renderer renderer in new Renderer[] { line, trail })
                {
                    Material material = renderer.sharedMaterial;
                    Assert.IsNotNull(material, $"{renderer.GetType().Name}: no material");
                    Assert.AreNotSame(ParticleKit.FallbackMaterial, material, $"{renderer.GetType().Name}: still on the default particle material");
                    Assert.AreSame(art.ParticleMaterial(false).shader, material.shader, $"{renderer.GetType().Name}: should use the atlas's particle shader");
                    Assert.AreSame(SpriteKit.Beam(), material.mainTexture, $"{renderer.GetType().Name}: should wear the beam profile");
                }

                Assert.AreEqual(LineTextureMode.Stretch, line.textureMode, "the profile runs once along the line");
                Assert.AreEqual(LineTextureMode.Stretch, trail.textureMode, "the profile runs once along the trail");
                Assert.AreSame(line.sharedMaterial, trail.sharedMaterial, "lines and trails share one beam material");

                additive = ParticleKit.AdditiveSprite(SpriteKit.Beam(), Color.white);
                picture = ParticleKit.Picture(Texture2D.whiteTexture);
                Assert.AreSame(art.ParticleMaterial(false).shader, additive.shader, "glow sprites should use the atlas's particle shader");
                Assert.AreSame(art.ParticleMaterial(true).shader, picture.shader, "pictures should use the atlas's particle shader");
            }
            finally
            {
                Object.DestroyImmediate(root);
                DestroyIfAny(additive);
                DestroyIfAny(picture);
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
        public void NoRuntimeScript_ReadsAUrpDefaultMaterial()
        {
            foreach (string file in Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == KitFile || file.Replace('\\', '/').Contains("/" + EditorFolder + "/"))
                {
                    continue;
                }

                string source = File.ReadAllText(file);
                foreach (string name in UrpDefaults)
                {
                    Assert.IsFalse(source.Contains(name), $"{file} reads the URP {name}, which is null in a player build; use LineKit, ParticleKit or PipelineMaterials");
                }
            }
        }

        [Test]
        public void PipelineLit_LooksLikeTheStockMaterial()
        {
            Material stock = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultMaterial;
            Material lit = PipelineMaterials.Lit();
            try
            {
                Assert.AreSame(stock.shader, lit.shader, "the lit shader should be the stock material's");
                foreach (int id in LitStockProperties)
                {
                    Assert.AreEqual(stock.GetColor(id), lit.GetColor(id), $"property {id} differs from the stock material");
                }
            }
            finally
            {
                Object.DestroyImmediate(lit);
            }
        }

        private static void DestroyIfAny(Object asset)
        {
            if (asset != null)
            {
                Object.DestroyImmediate(asset);
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
