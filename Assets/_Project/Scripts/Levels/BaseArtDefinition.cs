using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The imported (Blender) look of the colony base: the modules (deck, command post, dish, landing pad, core, crystal,
    /// habitat, hardpoints) with their own pivots, the damage overlay shown once the base is badly hurt, the wreck left
    /// after the blast, and which modules turn or pulse. A <see cref="VisualTheme"/> points at one of these; without it
    /// the level keeps its procedural outpost. Pure look data: logic never reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Base Art Definition", fileName = "BaseArt")]
    public sealed class BaseArtDefinition : ScriptableObject
    {
        [SerializeField] private ModelArtDefinition _model;
        [SerializeField] private ModelArtDefinition _damage;
        [SerializeField] private ModelArtDefinition _wreck;

        [Header("Life")]
        [Tooltip("Name of the module that turns on its own pivot (the dish).")]
        [SerializeField] private string _turningModule = "Base_Dish";
        [SerializeField] private float _turnDegreesPerSecond = 40f;
        [Tooltip("Name of the module whose glow breathes and dies with the base (the core crystal).")]
        [SerializeField] private string _pulsingModule = "Base_Crystal";

        [Header("Damage")]
        [Tooltip("Health share below which the damage overlay shows and the sockets smoke.")]
        [SerializeField] [Range(0.05f, 0.95f)] private float _damagedBelow = 0.5f;
        [Tooltip("Seconds after the base falls before the wreck replaces it: the flash hides the swap.")]
        [SerializeField] [Min(0f)] private float _wreckDelay = 0.25f;

        public ModelArtDefinition Model => _model;
        public ModelArtDefinition Damage => _damage;
        public ModelArtDefinition Wreck => _wreck;
        public string TurningModule => _turningModule;
        public float TurnDegreesPerSecond => _turnDegreesPerSecond;
        public string PulsingModule => _pulsingModule;
        public float DamagedBelow => _damagedBelow;
        public float WreckDelay => _wreckDelay;

        private void OnValidate()
        {
            if (_model == null)
            {
                Debug.LogWarning($"BaseArtDefinition '{name}' has no model.", this);
            }
        }
    }
}
