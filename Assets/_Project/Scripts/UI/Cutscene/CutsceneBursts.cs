using Game.Combat;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Particle systems for cutscene sets that wear the Blender effect sheets (dust, debris, smoke) and stay true to a
    /// seek: a burst is scheduled in the system's own emission at a time of the shot, so the player's particle seek
    /// (restart, simulate to the shot time) brings it back exactly; a stream emits at a rate the set changes over time.
    /// </summary>
    public static class CutsceneBursts
    {
        // Longer than any shot, so a scheduled burst is never cut off by the system's own end.
        private const float Duration = 120f;
        private const float GrowTo = 2.2f;

        /// <summary>
        /// A burst of <paramref name="count"/> particles <paramref name="at"/> seconds after the system is played (the shot
        /// start). Particles fly up and out of a half sphere; negative gravity lets smoke rise.
        /// </summary>
        public static ParticleSystem Burst(Transform parent, string name, Vector3 localPosition, VfxSheet sheet, bool low, int count, float at, Vector2 size, Vector2 speed, float gravity, Color colour, float radius, VfxParticles.Frames frames)
        {
            ParticleSystem system = Create(parent, name, localPosition, sheet, low, count, size, speed, gravity, colour, radius, frames);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(at, (short)count) });
            return system;
        }

        /// <summary>A stream the owner opens with <see cref="ParticleKit.SetRate"/>; it plays from the start.</summary>
        public static ParticleSystem Stream(Transform parent, string name, Vector3 localPosition, VfxSheet sheet, bool low, int max, Vector2 size, Vector2 speed, float gravity, Color colour, float radius)
        {
            ParticleSystem system = Create(parent, name, localPosition, sheet, low, max, size, speed, gravity, colour, radius, VfxParticles.Frames.OverLife);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            system.Play(true);
            return system;
        }

        private static ParticleSystem Create(Transform parent, string name, Vector3 localPosition, VfxSheet sheet, bool low, int max, Vector2 size, Vector2 speed, float gravity, Color colour, float radius, VfxParticles.Frames frames)
        {
            ParticleSystem system = VfxParticles.Create(parent, name, Mathf.Max(1, max));
            system.transform.localPosition = localPosition;
            VfxParticles.Use(system, sheet, low, frames);
            VfxParticles.Heat(system, null);
            ParticleSystem.MainModule main = system.main;
            main.duration = Duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(sheet != null ? sheet.Lifetime * 0.8f : 1f, sheet != null ? sheet.Lifetime * 1.2f : 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.gravityModifier = gravity;
            main.startColor = colour;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = radius;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            if (gravity <= 0f)
            {
                // Dust and smoke swell as they drift; debris keeps its size.
                ParticleSystem.SizeOverLifetimeModule grow = system.sizeOverLifetime;
                grow.enabled = true;
                grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, GrowTo));
            }

            return system;
        }
    }
}
