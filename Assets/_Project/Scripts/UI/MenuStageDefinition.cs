using Game.Enemies;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// What the living scene behind the main menu is made of: the themes that colour its corners (the forest pond,
    /// the lava pool, the sand pit and the hive) and the creatures that live there. Data, so the menu can be
    /// recast without code.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Menu Stage", fileName = "MainMenuStage")]
    public sealed class MenuStageDefinition : ScriptableObject
    {
        [Header("Themes")]
        [SerializeField] private VisualTheme _forestTheme;
        [SerializeField] private VisualTheme _lavaTheme;
        [SerializeField] private VisualTheme _ruinsTheme;
        [SerializeField] private VisualTheme _nestTheme;
        [SerializeField] private VisualTheme _iceTheme;
        [SerializeField] private VisualTheme _swampTheme;

        [Header("Look")]
        [Tooltip("The island, its props, pools, lights and camera as built in Blender (generator step \"Menu art\").")]
        [SerializeField] private MenuDioramaArt _art;

        [Header("Creatures")]
        [Tooltip("Crawls out of the hive mouth, roams around the mound and goes back in.")]
        [SerializeField] private EnemyDefinition[] _nestCreatures = new EnemyDefinition[0];
        [Tooltip("Sits in the hive mouth and watches.")]
        [SerializeField] private EnemyDefinition _nestGuardian;
        [SerializeField] private EnemyDefinition _flyer;
        [SerializeField] private EnemyDefinition _waterCreature;
        [SerializeField] private EnemyDefinition _lavaCreature;
        [SerializeField] private EnemyDefinition _sandCreature;
        [SerializeField] private EnemyDefinition _iceCreature;
        [SerializeField] private EnemyDefinition _bogCreature;

        [Header("Sound")]
        [Tooltip("The bed under the menu music; without one the nest theme's procedural ambience plays.")]
        [SerializeField] private AudioClip _ambience;
        [Tooltip("Its level before the Ambience volume setting.")]
        [SerializeField] [Range(0f, 1f)] private float _ambienceVolume = 0.25f;

        public VisualTheme ForestTheme => _forestTheme;
        public VisualTheme LavaTheme => _lavaTheme;
        public VisualTheme RuinsTheme => _ruinsTheme;
        public VisualTheme NestTheme => _nestTheme;

        /// <summary>The frozen lake and the bog pool; either may be null while that world is not generated yet.</summary>
        public VisualTheme IceTheme => _iceTheme;
        public VisualTheme SwampTheme => _swampTheme;

        /// <summary>The Blender island and its dressing; null on data generated before it existed.</summary>
        public MenuDioramaArt Art => _art;

        /// <summary>The theme a slot names; null for <see cref="MenuThemeSlot.None"/> or a theme not generated yet.</summary>
        public VisualTheme Theme(MenuThemeSlot slot)
        {
            switch (slot)
            {
                case MenuThemeSlot.Forest: return _forestTheme;
                case MenuThemeSlot.Lava: return _lavaTheme;
                case MenuThemeSlot.Ruins: return _ruinsTheme;
                case MenuThemeSlot.Nest: return _nestTheme;
                case MenuThemeSlot.Ice: return _iceTheme;
                case MenuThemeSlot.Swamp: return _swampTheme;
                default: return null;
            }
        }
        public EnemyDefinition[] NestCreatures => _nestCreatures;
        public EnemyDefinition NestGuardian => _nestGuardian;
        public EnemyDefinition Flyer => _flyer;
        public EnemyDefinition WaterCreature => _waterCreature;
        public EnemyDefinition LavaCreature => _lavaCreature;
        public EnemyDefinition SandCreature => _sandCreature;
        public EnemyDefinition IceCreature => _iceCreature;
        public EnemyDefinition BogCreature => _bogCreature;
        public AudioClip Ambience => _ambience;
        public float AmbienceVolume => _ambienceVolume;

        /// <summary>True when every theme is set; the creatures may be left out and are then simply not shown.</summary>
        public bool IsComplete => _forestTheme != null && _lavaTheme != null && _ruinsTheme != null && _nestTheme != null;

        private void OnValidate()
        {
            if (!IsComplete)
            {
                Debug.LogWarning($"MenuStageDefinition '{name}' is missing a theme.", this);
            }
        }
    }
}
