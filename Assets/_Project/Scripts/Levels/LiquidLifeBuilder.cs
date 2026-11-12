using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// The motion that makes a pool read as alive from the fixed camera: lava bubbles that swell and burst into
    /// sparks under a slow drift of smoke, rings and glints on the water, and steam hanging over the bog. All of it
    /// is particles on the stock particle material, so it costs no shader and scales with the platform's budget.
    /// </summary>
    public static class LiquidLifeBuilder
    {
        private const float Lift = 0.08f;
        // Drawn after every sheet and mist layer in the transparent queue, so a ring never sinks into the water.
        private const int QueueAfterSheets = (int)RenderQueue.Transparent + 50;

        private const float BubbleRatePerCell = 0.7f;
        private const float BubbleMinLife = 0.9f;
        private const float BubbleMaxLife = 1.8f;
        // Bubbles, rings and puffs are sized in cells and spawn on the inner part of each cell, so they never cross the bank.
        private const float BubbleMinSize = 0.2f;
        private const float BubbleMaxSize = 0.42f;
        private const float BubbleGlow = 2.4f;
        private const int PopSparks = 7;
        private const float SparkMinSpeed = 1.2f;
        private const float SparkMaxSpeed = 2.6f;
        private const float SparkGlow = 3f;
        private const float SmokeRatePerCell = 0.22f;
        private const float SmokeAlpha = 0.38f;

        private const float RingRatePerCell = 0.5f;
        private const float RingSize = 0.6f;
        private const float RingAlpha = 0.85f;
        private const float GlintRatePerCell = 1.1f;
        private const float GlintGlow = 4f;

        private const float SteamRatePerCell = 0.3f;
        private const float SteamAlpha = 0.4f;

        private const float DustRatePerCell = 0.3f;
        private const float DustAlpha = 0.35f;
        private const float SinkRatePerCell = 0.25f;
        private const float SinkSize = 0.55f;
        private const float SinkAlpha = 0.45f;

        private const float FrostMistRatePerCell = 0.2f;
        private const float FrostMistAlpha = 0.3f;
        private const float FrostGlintRatePerCell = 0.5f;
        private const float FrostGlintGlow = 3.5f;

        public static void Build(TerrainKind kind, Mesh emitter, Mesh inner, VisualTheme theme, float cellSize, float surface, float area, float particleScale, Transform parent)
        {
            if (particleScale <= 0f)
            {
                return;
            }

            float budget = area * particleScale;
            switch (kind)
            {
                case TerrainKind.Lava:
                    BuildLavaBubbles(inner, theme, cellSize, surface, budget, parent);
                    BuildLavaSmoke(emitter, theme, cellSize, surface, budget, parent);
                    break;
                case TerrainKind.Water:
                    BuildWaterRings(inner, theme, cellSize, surface, budget, parent);
                    BuildWaterGlints(inner, theme, surface, budget, parent);
                    break;
                case TerrainKind.Bog:
                    BuildSteam(emitter, theme, cellSize, surface, budget, parent);
                    break;
                case TerrainKind.Quicksand:
                    BuildSandDust(emitter, theme, cellSize, surface, budget, parent);
                    BuildSinkRings(inner, theme, cellSize, surface, budget, parent);
                    break;
                case TerrainKind.Ice:
                    BuildFrostMist(emitter, theme, cellSize, surface, budget, parent);
                    BuildFrostGlints(inner, theme, surface, budget, parent);
                    break;
            }
        }

        // A dome that swells on the crust, bursts and throws sparks: the burst is a sub-emitter fired on death.
        private static void BuildLavaBubbles(Mesh emitter, VisualTheme theme, float cellSize, float surface, float budget, Transform parent)
        {
            ParticleSystem bubbles = Surface(parent, "LavaBubbles", emitter, surface, Color.white, 1f, budget * BubbleRatePerCell, BubbleMinLife, BubbleMaxLife, BubbleMinSize * cellSize, BubbleMaxSize * cellSize, ParticleSystemRenderMode.HorizontalBillboard);
            ParticleSystem.SizeOverLifetimeModule size = bubbles.sizeOverLifetime;
            size.enabled = true;
            var swell = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.75f, 0.7f), new Keyframe(0.96f, 1f), new Keyframe(1f, 0f));
            size.size = new ParticleSystem.MinMaxCurve(1f, swell);
            // A dome of melt swells through the dark crust, then flashes white-hot the moment it tears open.
            Tint(bubbles, Color.Lerp(theme.LavaColor, Color.white, 0.15f) * BubbleGlow, Color.Lerp(theme.LavaColor, Color.white, 0.6f) * BubbleGlow * 1.5f);

            var sparks = new GameObject("Pop").AddComponent<ParticleSystem>();
            sparks.transform.SetParent(bubbles.transform, false);
            sparks.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.MainModule main = sparks.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(SparkMinSpeed, SparkMaxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startColor = Color.Lerp(theme.LavaColor, Color.white, 0.5f) * SparkGlow;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(budget * BubbleRatePerCell * PopSparks) + 16;
            ParticleSystem.EmissionModule emission = sparks.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, PopSparks) });
            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.02f;
            Fade(sparks, 1f, 0.8f, 0f);
            var sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.sharedMaterial = ParticleMaterial(null);
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparkRenderer.lengthScale = 2.5f;

            ParticleSystem.SubEmittersModule sub = bubbles.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(sparks, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
        }

        // Thin smoke lit from below, rising and swelling until it thins out.
        private static void BuildLavaSmoke(Mesh emitter, VisualTheme theme, float cellSize, float surface, float budget, Transform parent)
        {
            Color smoke = Color.Lerp(theme.LavaColor, new Color(0.5f, 0.45f, 0.42f), 0.6f);
            ParticleSystem system = Surface(parent, "LavaSmoke", emitter, surface, smoke, SmokeAlpha, budget * SmokeRatePerCell, 2.5f, 4.5f, 0.7f * cellSize, 1.2f * cellSize, ParticleSystemRenderMode.Billboard);
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(SpriteKit.Puff());
            Rise(system, 0.12f, 0.28f, 0.25f);
            Grow(system, 1f, 2.4f);
            Fade(system, 0f, 1f, 0f);
        }

        // Rings that open from a point and dissolve, as if something broke the surface.
        private static void BuildWaterRings(Mesh emitter, VisualTheme theme, float cellSize, float surface, float budget, Transform parent)
        {
            Color tint = Color.Lerp(theme.WaterColor, Color.white, 0.92f);
            ParticleSystem system = Surface(parent, "WaterRings", emitter, surface, tint, RingAlpha, budget * RingRatePerCell, 1.2f, 1.9f, RingSize * cellSize, RingSize * cellSize, ParticleSystemRenderMode.HorizontalBillboard);
            Grow(system, 0.06f, 1f);
            Fade(system, 1f, 0.5f, 0f);
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(SpriteKit.Ring());
        }

        // Short bright points that flare and vanish: the sun catching the moving water.
        private static void BuildWaterGlints(Mesh emitter, VisualTheme theme, float surface, float budget, Transform parent)
        {
            Color glint = Color.Lerp(theme.WaterColor, Color.white, 0.85f) * GlintGlow;
            ParticleSystem system = Surface(parent, "WaterGlints", emitter, surface, glint, 1f, budget * GlintRatePerCell, 0.3f, 0.7f, 0.14f, 0.26f, ParticleSystemRenderMode.Billboard);
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)));
        }

        // Pale steam hanging low over the mire, drifting sideways more than it rises.
        private static void BuildSteam(Mesh emitter, VisualTheme theme, float cellSize, float surface, float budget, Transform parent)
        {
            Color steam = Color.Lerp(Color.Lerp(theme.BogColor, theme.AccentColor, 0.3f), Color.white, 0.65f);
            ParticleSystem system = Surface(parent, "BogSteam", emitter, surface, steam, SteamAlpha, budget * SteamRatePerCell, 3f, 5.5f, 0.9f * cellSize, 1.6f * cellSize, ParticleSystemRenderMode.Billboard);
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(SpriteKit.Puff());
            Rise(system, 0.06f, 0.16f, 0.35f);
            Grow(system, 1f, 1.8f);
            Fade(system, 0f, 1f, 0f);
        }

        // Dry dust lifted off the churning sand, drifting low and sideways.
        private static void BuildSandDust(Mesh emitter, VisualTheme theme, float cellSize, float surface, float budget, Transform parent)
        {
            Color dust = Color.Lerp(theme.SandColor, Color.white, 0.25f);
            ParticleSystem system = Surface(parent, "SandDust", emitter, surface, dust, DustAlpha, budget * DustRatePerCell, 2.5f, 4f, 0.7f * cellSize, 1.2f * cellSize, ParticleSystemRenderMode.Billboard);
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(SpriteKit.Puff());
            Rise(system, 0.04f, 0.1f, 0.4f);
            Grow(system, 1f, 1.7f);
            Fade(system, 0f, 1f, 0f);
        }

        // Rings that close in on a point and vanish: the sand swallowing something.
        private static void BuildSinkRings(Mesh emitter, VisualTheme theme, float cellSize, float surface, float budget, Transform parent)
        {
            Color tint = Color.Lerp(theme.SandColor, Color.black, 0.35f);
            ParticleSystem system = Surface(parent, "SandSinks", emitter, surface, tint, SinkAlpha, budget * SinkRatePerCell, 1.8f, 2.8f, SinkSize * cellSize, SinkSize * cellSize, ParticleSystemRenderMode.HorizontalBillboard);
            Grow(system, 1f, 0.1f);
            Fade(system, 0f, 1f, 0f);
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(SpriteKit.Ring());
        }

        // Cold air lying on the sheet: pale wisps that barely rise and drift with the wind.
        private static void BuildFrostMist(Mesh emitter, VisualTheme theme, float cellSize, float surface, float budget, Transform parent)
        {
            Color mist = Color.Lerp(theme.IceColor, Color.white, 0.5f);
            ParticleSystem system = Surface(parent, "FrostMist", emitter, surface, mist, FrostMistAlpha, budget * FrostMistRatePerCell, 3f, 5f, 1f * cellSize, 1.8f * cellSize, ParticleSystemRenderMode.Billboard);
            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial(SpriteKit.Puff());
            Rise(system, 0.02f, 0.06f, 0.3f);
            Grow(system, 1f, 1.5f);
            Fade(system, 0f, 1f, 0f);
        }

        // Sharp white sparks of frost that catch the light for a moment.
        private static void BuildFrostGlints(Mesh emitter, VisualTheme theme, float surface, float budget, Transform parent)
        {
            Color glint = Color.Lerp(theme.IceColor, Color.white, 0.9f) * FrostGlintGlow;
            ParticleSystem system = Surface(parent, "FrostGlints", emitter, surface, glint, 1f, budget * FrostGlintRatePerCell, 0.4f, 0.9f, 0.08f, 0.16f, ParticleSystemRenderMode.Billboard);
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)));
        }

        // A looping system that spawns on the pool cells just above the surface and plays from the first frame.
        private static ParticleSystem Surface(Transform parent, string name, Mesh emitter, float surface, Color color, float alpha, float rate, float minLife, float maxLife, float minSize, float maxSize, ParticleSystemRenderMode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, surface + Lift, 0f);
            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            color.a = alpha;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.CeilToInt(rate * maxLife) + 8;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.mesh = emitter;
            shape.alignToDirection = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMaterial(null);
            renderer.renderMode = mode;
            system.Play();
            return system;
        }

        private static Material ParticleMaterial(Texture2D sprite)
        {
            Material material = ParticleKit.SpriteMaterial(sprite);
            if (material != null)
            {
                material.renderQueue = QueueAfterSheets;
            }

            return material;
        }

        private static void Rise(ParticleSystem system, float minSpeed, float maxSpeed, float drift)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.alignToDirection = false;
            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = drift;
            noise.frequency = 0.4f;
        }

        // Colour over life from one tint to another, held on the first until the last tenth of the life.
        private static void Tint(ParticleSystem system, Color start, Color end)
        {
            ParticleSystem.ColorOverLifetimeModule tint = system.colorOverLifetime;
            tint.enabled = true;
            var gradient = new Gradient { mode = GradientMode.Blend };
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(start, 0.86f), new GradientColorKey(end, 0.94f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            tint.color = gradient;
        }

        private static void Grow(ParticleSystem system, float start, float end)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, start, 1f, end));
        }

        private static void Fade(ParticleSystem system, float start, float middle, float end)
        {
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(start, 0f), new GradientAlphaKey(middle, 0.4f), new GradientAlphaKey(end, 1f) });
            fade.color = gradient;
        }
    }
}
