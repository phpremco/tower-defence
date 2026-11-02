using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A travelling wave that the unit shader runs through a mesh: a fin undulating, a membrane fluttering, a slug's
    /// foot creeping. Everything is in the part's own space. Vertices move along <see cref="Direction"/> by
    /// amplitude times an envelope times a sine that travels along <see cref="Vector"/> at <see cref="Speed"/>
    /// radians a second. The envelope is a clamped linear ramp over the part, squared, so one end stays put and the
    /// other end carries the motion. Applied through a property block, so it survives material swaps and costs no
    /// CPU time per frame.
    /// </summary>
    public readonly struct SurfaceWave
    {
        public static readonly int AmplitudeId = Shader.PropertyToID("_WaveAmplitude");
        public static readonly int VectorId = Shader.PropertyToID("_WaveVector");
        public static readonly int RampId = Shader.PropertyToID("_WaveRamp");
        public static readonly int DirectionId = Shader.PropertyToID("_WaveDirection");

        /// <summary>How far the moving end travels, in the part's units.</summary>
        public readonly float Amplitude;

        /// <summary>Spatial frequency in radians per unit; the crests move towards this direction.</summary>
        public readonly Vector3 Vector;

        /// <summary>Radians per second.</summary>
        public readonly float Speed;

        /// <summary>The envelope ramp: the wave is scaled by saturate(dot(position, Ramp.xyz) + Ramp.w) squared.</summary>
        public readonly Vector4 Ramp;

        /// <summary>Which way the vertices move.</summary>
        public readonly Vector3 Direction;

        public SurfaceWave(float amplitude, Vector3 vector, float speed, Vector4 ramp, Vector3 direction)
        {
            Amplitude = amplitude;
            Vector = vector;
            Speed = speed;
            Ramp = ramp;
            Direction = direction;
        }

        public bool IsActive => Amplitude > 0f;

        /// <summary>A ramp that is zero at <paramref name="from"/> and one at <paramref name="to"/> along the line between them.</summary>
        public static Vector4 RampBetween(Vector3 from, Vector3 to)
        {
            Vector3 span = to - from;
            Vector3 slope = span / Mathf.Max(span.sqrMagnitude, 0.0001f);
            return new Vector4(slope.x, slope.y, slope.z, -Vector3.Dot(slope, from));
        }

        /// <summary>Writes the wave into a property block; <paramref name="phase"/> gives each individual its own moment in the cycle.</summary>
        public void Apply(MaterialPropertyBlock block, float phase)
        {
            block.SetFloat(AmplitudeId, Amplitude);
            block.SetVector(VectorId, new Vector4(Vector.x, Vector.y, Vector.z, Speed));
            block.SetVector(RampId, Ramp);
            block.SetVector(DirectionId, new Vector4(Direction.x, Direction.y, Direction.z, phase));
        }
    }
}
