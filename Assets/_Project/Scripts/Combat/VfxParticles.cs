using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    /// <summary>
    /// Burst-only particle systems that wear a <see cref="VfxSheet"/>: built once, then pointed at a sheet per use
    /// (material, atlas grid, how the frames play). They pass the frame blend and the heat curve to the effect shaders
    /// through custom vertex streams. Setting a sheet changes module values only, so a pooled effect allocates nothing.
    /// </summary>
    public static class VfxParticles
    {
        /// <summary>How the cells of a sheet are used.</summary>
        public enum Frames
        {
            /// <summary>Play the flipbook once over the particle's life.</summary>
            OverLife,

            /// <summary>One random cell per particle (shards).</summary>
            RandomCell,

            /// <summary>One random row per particle, stretched along its flight (spark streaks).</summary>
            RandomRow,
        }

        // Position, colour, this frame's uv and the next one's, the blend between them, and Custom1.x (the heat).
        private static readonly List<ParticleSystemVertexStream> Streams = new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.UV2,
            ParticleSystemVertexStream.AnimBlend,
            ParticleSystemVertexStream.Custom1X,
        };

        private static readonly AnimationCurve Rising = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        private const float LastCell = 0.9999f;

        public static ParticleSystem Create(Transform parent, string name, int maxParticles)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = Fade(0.05f, 0.7f);

            ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
            sheet.mode = ParticleSystemAnimationMode.Grid;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.SetActiveVertexStreams(Streams);
            return system;
        }

        /// <summary>A gradient that fades in over <paramref name="fadeIn"/> of the life and out from <paramref name="fadeOutFrom"/>.</summary>
        public static Gradient Fade(float fadeIn, float fadeOutFrom)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(fadeIn > 0f ? 0f : 1f, 0f), new GradientAlphaKey(1f, Mathf.Max(0.001f, fadeIn)), new GradientAlphaKey(1f, fadeOutFrom), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        /// <summary>Points a system at a sheet; without a sheet it keeps the plain particle material and no atlas.</summary>
        public static void Use(ParticleSystem system, VfxSheet sheet, bool low, Frames frames = Frames.OverLife)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            ParticleSystem.TextureSheetAnimationModule animation = system.textureSheetAnimation;
            if (sheet == null)
            {
                animation.enabled = false;
                return;
            }

            renderer.sharedMaterial = sheet.MaterialFor(low);
            renderer.pivot = new Vector3(0.5f - sheet.Pivot.x, 0.5f - sheet.Pivot.y, 0f);
            animation.enabled = sheet.Columns * sheet.Rows > 1;
            animation.numTilesX = sheet.Columns;
            animation.numTilesY = sheet.Rows;
            animation.cycleCount = 1;
            float used = Mathf.Clamp01(sheet.Frames / (float)(sheet.Columns * sheet.Rows));
            switch (frames)
            {
                case Frames.RandomCell:
                    animation.animation = ParticleSystemAnimationType.WholeSheet;
                    animation.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                    animation.startFrame = new ParticleSystem.MinMaxCurve(0f, used * LastCell);
                    break;
                case Frames.RandomRow:
                    animation.animation = ParticleSystemAnimationType.SingleRow;
                    animation.rowMode = ParticleSystemAnimationRowMode.Random;
                    animation.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                    animation.startFrame = new ParticleSystem.MinMaxCurve(0f);
                    break;
                default:
                    animation.animation = ParticleSystemAnimationType.WholeSheet;
                    animation.frameOverTime = new ParticleSystem.MinMaxCurve(used, Rising);
                    animation.startFrame = new ParticleSystem.MinMaxCurve(0f);
                    break;
            }
        }

        /// <summary>Feeds the heat curve to the shader through Custom1.x (a constant 1 without a curve).</summary>
        public static void Heat(ParticleSystem system, AnimationCurve heat)
        {
            ParticleSystem.CustomDataModule data = system.customData;
            data.enabled = true;
            data.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
            data.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1);
            data.SetVector(ParticleSystemCustomData.Custom1, 0, heat != null && heat.length > 0 ? new ParticleSystem.MinMaxCurve(1f, heat) : new ParticleSystem.MinMaxCurve(1f));
        }
    }
}
