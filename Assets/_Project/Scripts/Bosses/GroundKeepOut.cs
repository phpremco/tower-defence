using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Circles of ground a boss mark must leave intact (Remco 2026-11-05: the Sealed One's furrow ran under the gates and the
    /// hero, who then stood on dug-out ground). A signature fills them each frame with the structures' footprints plus a
    /// margin and the hero, and <c>Game/BossDecal</c> (on materials with <see cref="EnabledId"/> set) tapers a mark to a torn
    /// end before each circle. Shader globals, so the decal materials stay SRP-batcher compatible.
    /// </summary>
    public sealed class GroundKeepOut
    {
        /// <summary>The most circles the shader reads; the nearest ones are filled first by the caller.</summary>
        public const int Capacity = 48;

        /// <summary>Material switch: 1 lets a mark break off round the circles.</summary>
        public static readonly int EnabledId = Shader.PropertyToID("_KeepOut");

        private static readonly int CirclesId = Shader.PropertyToID("_GroundKeepOut");
        private static readonly int CountId = Shader.PropertyToID("_GroundKeepOutCount");

        private readonly Vector4[] _circles = new Vector4[Capacity];
        private int _count;

        public int Count => _count;

        public void Clear() => _count = 0;

        /// <summary>Adds a circle (centre on the ground, radius of the intact ground, metres over which the mark tapers in).</summary>
        public bool Add(Vector3 centre, float radius, float taper)
        {
            if (_count >= Capacity)
            {
                return false;
            }

            _circles[_count++] = new Vector4(centre.x, centre.z, radius, Mathf.Max(0.05f, taper));
            return true;
        }

        public Vector4 this[int index] => _circles[index];

        /// <summary>Hands the circles to the shaders.</summary>
        public void Publish()
        {
            Shader.SetGlobalVectorArray(CirclesId, _circles);
            Shader.SetGlobalFloat(CountId, _count);
        }

        /// <summary>No circles any more (the fight ended).</summary>
        public static void Withdraw() => Shader.SetGlobalFloat(CountId, 0f);
    }
}
