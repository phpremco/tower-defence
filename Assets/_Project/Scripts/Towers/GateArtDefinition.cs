using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// The imported (Blender) look of a road gate: one model per gate level (pylons, beam and hex panels on their own
    /// bones, with Hit, Repair, Build and Collapse clips and a damage overlay mesh), the wreck that stays after the
    /// collapse, and which panels fall off once the gate is badly hurt. A gate's <see cref="TowerDefinition"/> points at
    /// one of these; without it the gate keeps its procedural pylons. Pure look data: logic never reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gate Art Definition", fileName = "GateArt")]
    public sealed class GateArtDefinition : ScriptableObject
    {
        [Header("Looks")]
        [Tooltip("The gate's model per level, in level order; a level without its own keeps the one below.")]
        [SerializeField] private ModelArtDefinition[] _levels = new ModelArtDefinition[0];
        [SerializeField] private ModelArtDefinition _wreck;

        [Header("Fit")]
        [Tooltip("Distance between the outer faces of the pylons the model was built for; a wider road stretches it across.")]
        [SerializeField] [Min(0.1f)] private float _modelWidth = 2.05f;
        [Tooltip("Width of the energy field between the pylons, as a share of the gate's width.")]
        [SerializeField] [Range(0.3f, 1f)] private float _fieldShare = 0.78f;

        [Header("Damage")]
        [Tooltip("Health share below which the damage overlay shows and the loose panels fall off.")]
        [SerializeField] [Range(0.05f, 0.95f)] private float _damagedBelow = 0.5f;
        [Tooltip("The panel bones that fall off when the gate is damaged (scaled to zero until the Repair clip pops them back).")]
        [SerializeField] private string[] _looseParts = new string[0];

        public ModelArtDefinition Wreck => _wreck;
        public float ModelWidth => _modelWidth;
        public float FieldShare => _fieldShare;
        public float DamagedBelow => _damagedBelow;
        public string[] LooseParts => _looseParts;

        /// <summary>The look of a level: that level's model, or the newest one below it.</summary>
        public ModelArtDefinition ArtFor(int levelIndex)
        {
            for (int level = Mathf.Min(levelIndex, _levels.Length - 1); level >= 0; level--)
            {
                if (_levels[level] != null)
                {
                    return _levels[level];
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (ArtFor(0) == null)
            {
                Debug.LogWarning($"GateArtDefinition '{name}' has no level-1 look.", this);
            }

            if (_wreck == null)
            {
                Debug.LogWarning($"GateArtDefinition '{name}' has no wreck.", this);
            }
        }
    }
}
