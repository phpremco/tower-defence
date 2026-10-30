using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>Slow drifting motes over the field (spores, embers, dust), coloured and paced by the level's theme.</summary>
    public static class AmbientParticlesBuilder
    {
        private const float Height = 3.5f;
        private const float MarginCells = 3f;
        private const float MinLifetime = 6f;
        private const float MaxLifetime = 11f;
        private const float MinSize = 0.04f;
        private const float MaxSize = 0.1f;
        private const float DriftStrength = 0.35f;
        private const float Brightness = 1.6f;
        private const float Alpha = 0.8f;
        private const int MaxParticles = 400;

        public static void Build(LevelDefinition level, VisualTheme theme, Transform parent, float particleScale = 1f)
        {
            if (theme.AmbientRate <= 0f || particleScale <= 0f)
            {
                return;
            }

            var go = new GameObject("AmbientMotes");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, Height * 0.5f, 0f);

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(MinLifetime, MaxLifetime);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(MinSize, MaxSize);
            Color glow = theme.AccentColor * Brightness;
            glow.a = Alpha;
            main.startColor = glow;
            main.maxParticles = MaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.rateOverTime = theme.AmbientRate * particleScale;

            Vector2 size = level.WorldSize;
            float margin = MarginCells * level.CellSize;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x + margin * 2f, Height, size.y + margin * 2f);

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = theme.AmbientRise;
            velocity.z = 0f;

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = DriftStrength;
            noise.frequency = 0.3f;

            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            system.Play();
        }
    }
}
