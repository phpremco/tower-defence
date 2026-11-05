using System.Collections.Generic;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The glowing lava a boss leaves where it walks (Docs/BOSSES.md: "her path leaves glowing lava for 6 s"): a patch
    /// every so many units, each living a fixed time, and the question whether a point lies on a live patch. Pure logic;
    /// the patches live in a fixed ring, so a long walk never allocates.
    /// </summary>
    public sealed class LavaTrail
    {
        private readonly Vector3[] _points;
        private readonly float[] _ages;
        private readonly float _lifetime;
        private readonly float _spacing;
        private int _next;
        private int _count;
        private Vector3 _last;
        private bool _started;

        public LavaTrail(float lifetime, float spacing, int capacity = 64)
        {
            _lifetime = lifetime;
            _spacing = Mathf.Max(0.05f, spacing);
            _points = new Vector3[capacity];
            _ages = new float[capacity];
        }

        public int Capacity => _points.Length;

        /// <summary>The patch in a slot of the ring, and how old it is (dead patches are older than the lifetime).</summary>
        public Vector3 PointAt(int slot) => _points[slot];

        public float AgeAt(int slot) => _ages[slot];

        public bool IsLive(int slot) => slot < _count && _ages[slot] < _lifetime;

        public float Lifetime => _lifetime;

        /// <summary>How far apart the patches are laid.</summary>
        public float Spacing => _spacing;

        /// <summary>Raised with the slot of a patch just laid, for the view.</summary>
        public event System.Action<int> Laid;

        public void Clear()
        {
            _count = 0;
            _next = 0;
            _started = false;
        }

        /// <summary>The boss is here: a fresh patch once it has moved a spacing from the last one.</summary>
        public void Walk(Vector3 at)
        {
            at.y = 0f;
            if (_started && (at - _last).sqrMagnitude < _spacing * _spacing)
            {
                return;
            }

            _started = true;
            _last = at;
            _points[_next] = at;
            _ages[_next] = 0f;
            int slot = _next;
            _next = (_next + 1) % _points.Length;
            _count = Mathf.Min(_count + 1, _points.Length);
            Laid?.Invoke(slot);
        }

        /// <summary>A break in the trail (it dived, it climbed): the next patch starts afresh.</summary>
        public void Lift() => _started = false;

        public void Step(float deltaTime)
        {
            for (int i = 0; i < _count; i++)
            {
                _ages[i] += deltaTime;
            }
        }

        /// <summary>True when a point lies within a radius of a live patch.</summary>
        public bool Touches(Vector3 at, float radius)
        {
            float sqr = radius * radius;
            for (int i = 0; i < _count; i++)
            {
                if (_ages[i] >= _lifetime)
                {
                    continue;
                }

                float dx = _points[i].x - at.x;
                float dz = _points[i].z - at.z;
                if (dx * dx + dz * dz <= sqr)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
