using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Bosses
{
    /// <summary>
    /// A 4 x 4 boss flipbook (<see cref="BossFxArt"/> sheets: spore cloud, vine burn, shard flash, dive wakes) as a particle
    /// system that plays one sheet per particle, facing the camera or lying flat on a surface. <see cref="Play"/> puts one
    /// sheet down at a point; a looping wake keeps emitting while it is on. Null when the art is not registered, so callers
    /// keep their stand-in.
    /// </summary>
    public sealed class FlipbookFx
    {
        private const int Frames = BossFxArt.SheetColumns * BossFxArt.SheetRows;

        private readonly ParticleSystem _system;
        private readonly float _lifetime;

        private FlipbookFx(ParticleSystem system, float lifetime)
        {
            _system = system;
            _lifetime = lifetime;
        }

        public ParticleSystem System => _system;

        /// <param name="flat">Lies on the ground or a liquid (a wake) instead of facing the camera.</param>
        /// <param name="backwards">Plays the sheet from its last frame to its first (the Shattered's rejoin).</param>
        public static FlipbookFx Create(Transform parent, string name, BossFxArt.Sheet sheet, bool flat, bool backwards = false, int max = 16)
        {
            BossFxArt art = BossFxArt.Active;
            Material material = art != null ? art.Material(sheet) : null;
            if (material == null)
            {
                return null;
            }

            float lifetime = Frames / art.FramesPerSecond(sheet);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = 1f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystem.TextureSheetAnimationModule frames = system.textureSheetAnimation;
            frames.enabled = true;
            frames.numTilesX = BossFxArt.SheetColumns;
            frames.numTilesY = BossFxArt.SheetRows;
            frames.animation = ParticleSystemAnimationType.WholeSheet;
            frames.frameOverTime = backwards ? new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.999f, 1f, 0f)) : new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 0.999f));
            frames.cycleCount = 1;

            if (flat)
            {
                // Overlapping loops (a wake) fade in and out, so the hand-over from one sheet to the next never pops.
                ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
                fade.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
                fade.color = gradient;
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = flat ? ParticleSystemRenderMode.HorizontalBillboard : ParticleSystemRenderMode.Billboard;
            renderer.alignment = flat ? ParticleSystemRenderSpace.World : ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = flat ? 10f : 0f;
            system.Play();
            return new FlipbookFx(system, lifetime);
        }

        public float Lifetime => _lifetime;

        /// <summary>Puts one sheet down at a point, size metres across, turned by a yaw (flat sheets) and tinted.</summary>
        public void Play(Vector3 at, float size, float yawDegrees, Color colour)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = at,
                startSize = size,
                startColor = colour,
                rotation = yawDegrees,
                startLifetime = _lifetime,
                applyShapeToPosition = false,
            };
            _system.Emit(emit, 1);
        }

        /// <summary>Keeps putting sheets down at the system's own position, overlapping so a looping sheet never gaps.</summary>
        public void Loop(bool on, float size)
        {
            ParticleSystem.MainModule main = _system.main;
            main.startSize = size;
            ParticleSystem.EmissionModule emission = _system.emission;
            emission.rateOverTime = on ? 2f / _lifetime : 0f;
        }
    }
}
