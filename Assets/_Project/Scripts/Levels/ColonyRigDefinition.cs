using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The colony's drilling rig as a level decoration (the derrick from the opening cutscene, with its haulers flying
    /// ore away): which models, how big, how far it keeps from everything else and how the haulers fly. Hung on an
    /// <see cref="EnvironmentSet"/>, so every level of that continent shows it; the level builder finds it a spot at
    /// the side of the field. Purely for looks: it has no health, no collider and no target.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Colony Rig", fileName = "ColonyRig")]
    public sealed class ColonyRigDefinition : ScriptableObject
    {
        [Tooltip("The derrick; its Deploy clip (the string sinking) is held at its end and its Idle clip (the spin) loops.")]
        [SerializeField] private ModelArtDefinition _drill;
        [SerializeField] [Min(0.1f)] private float _drillScale = 0.7f;
        [Tooltip("The broken derrick shown in place of the drill once a boss has broken out under it (static, same pivot).")]
        [SerializeField] private ModelArtDefinition _wreck;
        [Tooltip("How far round its pivot the rig keeps the ground free, in world units at its scale.")]
        [SerializeField] [Min(0.5f)] private float _footprintRadius = 1.8f;
        [Tooltip("How tall the rig stands at its scale; its top must stay on screen.")]
        [SerializeField] [Min(0.5f)] private float _height = 3.4f;
        [Tooltip("Space kept between the rig's footprint and the field's edge cells.")]
        [SerializeField] [Min(0f)] private float _fieldGap = 0.6f;

        [Header("Haulers")]
        [SerializeField] private ModelArtDefinition _hauler;
        [SerializeField] [Range(0, 6)] private int _haulers = 3;
        [SerializeField] [Min(0.1f)] private float _haulerScale = 1.1f;
        [Tooltip("The oval the haulers fly: half its length (away from the rig) and half its width.")]
        [SerializeField] private Vector2 _flightRadius = new Vector2(2.6f, 1.4f);
        [SerializeField] [Min(0f)] private float _flightHeight = 2.4f;
        [SerializeField] [Min(1f)] private float _lapSeconds = 11f;
        [SerializeField] [Min(0f)] private float _bob = 0.12f;

        public ModelArtDefinition Drill => _drill;
        public float DrillScale => _drillScale;
        public ModelArtDefinition Wreck => _wreck;
        public float FootprintRadius => _footprintRadius;
        public float Height => _height;
        public float FieldGap => _fieldGap;
        public ModelArtDefinition Hauler => _hauler;
        public int Haulers => _hauler != null ? _haulers : 0;
        public float HaulerScale => _haulerScale;
        public Vector2 FlightRadius => _flightRadius;
        public float FlightHeight => _flightHeight;
        public float LapSeconds => _lapSeconds;
        public float Bob => _bob;

        /// <summary>The ground the rig and its flight keep free of decor round its pivot.</summary>
        public float ClearRadius => Mathf.Max(_footprintRadius, _hauler != null ? _flightRadius.y : 0f);

        private void OnValidate()
        {
            if (_drill == null)
            {
                Debug.LogWarning($"ColonyRigDefinition '{name}': no drill model, nothing will stand in the levels.", this);
            }
        }
    }
}
