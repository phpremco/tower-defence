using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// What rises from the vents of an imported creature (the Cragback's lava craters): a light, soft smoke wisp and a
    /// few embers from every socket bone whose name starts with the prefix. Sizes, speeds and the emitter radius are per
    /// unit of the creature's world scale, so one definition serves a species at every size. Pure look data: the
    /// creature's view and its corpse build the emitters from it; logic never reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Model Vent Definition", fileName = "Vents")]
    public sealed class ModelVentDefinition : ScriptableObject
    {
        [Tooltip("Socket bones the vents rise from: every bone of the near model whose name starts with this (Smoke_1..3).")]
        [SerializeField] private string _socketPrefix = "Smoke_";
        [Tooltip("Radius of the spot each vent emits from, per unit of creature scale.")]
        [SerializeField] [Min(0f)] private float _radius = 0.03f;

        [Header("Smoke (a light wisp, never a cloud that hides the creature)")]
        [Tooltip("Colour of a fresh puff; its alpha is how much it covers (low: the smoke stays light).")]
        [SerializeField] private Color _smokeColor = new Color(0.72f, 0.66f, 0.6f, 0.5f);
        [Tooltip("Puffs per second from each vent.")]
        [SerializeField] [Min(0f)] private float _smokeRate = 3f;
        [Tooltip("Seconds a puff lives (min, max).")]
        [SerializeField] private Vector2 _smokeLife = new Vector2(1.6f, 2.6f);
        [Tooltip("Puff size at birth (min, max), per unit of creature scale; it swells as it rises.")]
        [SerializeField] private Vector2 _smokeSize = new Vector2(0.24f, 0.38f);
        [Tooltip("Speed a puff leaves the crater with (min, max), per unit of creature scale.")]
        [SerializeField] private Vector2 _smokeSpeed = new Vector2(0.15f, 0.3f);
        [Tooltip("Gravity factor on a puff: negative rises like warm smoke.")]
        [SerializeField] [Range(-1f, 0f)] private float _smokeBuoyancy = -0.05f;
        [SerializeField] [Min(1)] private int _smokeMax = 30;

        [Header("Embers (subtle sparks over the lava)")]
        [Tooltip("Colour of an ember; above 1 so the bloom catches it.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _emberColor = new Color(2.4f, 0.9f, 0.25f);
        [Tooltip("Embers per second from each vent; zero for smoke only.")]
        [SerializeField] [Min(0f)] private float _emberRate = 1.2f;
        [SerializeField] private Vector2 _emberLife = new Vector2(0.5f, 1f);
        [Tooltip("Ember size (min, max), per unit of creature scale.")]
        [SerializeField] private Vector2 _emberSize = new Vector2(0.02f, 0.04f);
        [SerializeField] private Vector2 _emberSpeed = new Vector2(0.25f, 0.6f);
        [SerializeField] [Range(-1f, 0f)] private float _emberBuoyancy = -0.12f;
        [SerializeField] [Min(1)] private int _emberMax = 12;

        [Header("Profiles and death")]
        [Tooltip("Share of the particles the Low (Switch) profile emits.")]
        [SerializeField] [Range(0f, 1f)] private float _lowShare = 0.5f;
        [Tooltip("Seconds after death over which the vents die down to nothing (the corpse's puffs live out their life).")]
        [SerializeField] [Min(0.05f)] private float _deathFadeSeconds = 1.2f;

        public string SocketPrefix => _socketPrefix;
        public float Radius => _radius;
        public Color SmokeColor => _smokeColor;
        public float SmokeRate => _smokeRate;
        public Vector2 SmokeLife => _smokeLife;
        public Vector2 SmokeSize => _smokeSize;
        public Vector2 SmokeSpeed => _smokeSpeed;
        public float SmokeBuoyancy => _smokeBuoyancy;
        public int SmokeMax => _smokeMax;
        public Color EmberColor => _emberColor;
        public float EmberRate => _emberRate;
        public Vector2 EmberLife => _emberLife;
        public Vector2 EmberSize => _emberSize;
        public Vector2 EmberSpeed => _emberSpeed;
        public float EmberBuoyancy => _emberBuoyancy;
        public int EmberMax => _emberMax;
        public float DeathFadeSeconds => _deathFadeSeconds;

        /// <summary>The share of the rates the active profile emits.</summary>
        public float RateShare(bool lowDetail) => lowDetail ? _lowShare : 1f;

        /// <summary>Empty when the data is sound, else what is wrong with it (shown by <see cref="OnValidate"/> and the tests).</summary>
        public string Problem()
        {
            if (string.IsNullOrEmpty(_socketPrefix))
            {
                return "has no socket prefix";
            }

            if (!Ordered(_smokeLife) || !Ordered(_smokeSize) || !Ordered(_smokeSpeed) || !Ordered(_emberLife) || !Ordered(_emberSize) || !Ordered(_emberSpeed))
            {
                return "has a (min, max) range with min above max or below zero";
            }

            if (_smokeLife.x <= 0f || _emberLife.x <= 0f)
            {
                return "has a particle life of zero";
            }

            return string.Empty;
        }

        private static bool Ordered(Vector2 range) => range.x >= 0f && range.x <= range.y;

        private void OnValidate()
        {
            string problem = Problem();
            if (problem.Length > 0)
            {
                Debug.LogWarning($"ModelVentDefinition '{name}' {problem}.", this);
            }
        }
    }
}
