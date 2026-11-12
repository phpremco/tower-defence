using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>Small particle systems built in code for view components: bursts of sparks, puffs of smoke and the like.</summary>
    public static class ParticleKit
    {
        /// <summary>
        /// A system that emits nothing on its own; call <see cref="ParticleSystem.Emit(int)"/> for a burst or
        /// <see cref="SetRate"/> for a steady stream. Negative gravity makes particles rise and swell, like smoke.
        /// </summary>
        public static ParticleSystem Create(Transform parent, string name, Vector3 localPosition, Color color, float minLife, float maxLife, float minSize, float maxSize, float minSpeed, float maxSpeed, float gravity, int max, float radius = 0.2f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, gravity < 0f ? 2.5f : 0.2f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return system;
        }

        /// <summary>
        /// Dresses a system in one cell of the Blender particle atlas (README round 25b): additive for motes, embers, frost and
        /// chrono motes, covering for drips, splats and goo. Keeps the default look when the art is not registered.
        /// </summary>
        public static ParticleSystem UseCell(ParticleSystem system, MarkerArt.ParticleCell cell)
        {
            MarkerArt art = MarkerArt.Active;
            return UseCell(system, cell, art != null ? art.Particles(cell) : null);
        }

        /// <summary>Dresses a system in a cell of the atlas drawn with the covering material (smoke from a soft mote).</summary>
        public static ParticleSystem UseCoveringCell(ParticleSystem system, MarkerArt.ParticleCell cell)
        {
            MarkerArt art = MarkerArt.Active;
            return UseCell(system, cell, art != null ? art.ParticleMaterial(true) : null);
        }

        private static ParticleSystem UseCell(ParticleSystem system, MarkerArt.ParticleCell cell, Material material)
        {
            if (material == null)
            {
                return system;
            }

            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = MarkerArt.ParticleColumns;
            sheet.numTilesY = MarkerArt.ParticleRows;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            // The grid counts frames from the top-left like the atlas, so the cell index is the frame.
            float frame = ((int)cell + 0.5f) / (MarkerArt.ParticleColumns * MarkerArt.ParticleRows);
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(frame);
            sheet.cycleCount = 1;
            return system;
        }

        public static void SetRate(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = rate > 0f;
            emission.rateOverTime = rate;
        }
    }
}
