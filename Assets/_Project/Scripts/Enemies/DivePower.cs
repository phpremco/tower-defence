using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// How a boss hides in the pools (Docs/BOSSES.md, "Use the pools"): which liquids, how long at most, what still
    /// reaches it under the surface, how fast it heals there and what its burst out does.
    /// </summary>
    [Serializable]
    public sealed class DivePower
    {
        [SerializeField] private PoolKinds _pools = PoolKinds.None;
        [Tooltip("Most seconds under the surface per dive (BOSSES.md: 6).")]
        [SerializeField] [Range(1f, 6f)] private float _maxSeconds = 6f;
        [Tooltip("Share of a blast's damage that still reaches it under the surface. Aimed shots cannot find it.")]
        [SerializeField] [Range(0f, 1f)] private float _splashShare = 0.5f;
        [Tooltip("Share of its full health it heals per second while hidden.")]
        [SerializeField] [Range(0f, 0.05f)] private float _regenPerSecond = 0.006f;
        [Tooltip("World units per second under the surface.")]
        [SerializeField] [Min(0.2f)] private float _swimSpeed = 2.4f;
        [Tooltip("How close the road must pass a pool's bank for the boss to take it.")]
        [SerializeField] [Min(0.5f)] private float _reach = 2.5f;
        [Tooltip("Damage to every structure near the bank where it bursts out.")]
        [SerializeField] [Min(0f)] private float _burstDamage = 30f;
        [SerializeField] [Min(0.5f)] private float _burstRadius = 2.4f;
        [Tooltip("The glow under the surface that shows where it swims.")]
        [SerializeField] private Color _wakeGlow = new Color(1f, 0.35f, 0.6f, 0.8f);

        public PoolKinds Pools => _pools;
        public bool Dives => _pools != PoolKinds.None;
        public float MaxSeconds => _maxSeconds;
        public float SplashShare => _splashShare;
        public float RegenPerSecond => _regenPerSecond;
        public float SwimSpeed => _swimSpeed;
        public float Reach => _reach;
        public float BurstDamage => _burstDamage;
        public float BurstRadius => _burstRadius;
        public Color WakeGlow => _wakeGlow;
    }
}
