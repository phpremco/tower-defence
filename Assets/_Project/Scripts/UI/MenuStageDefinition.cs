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

        [Header("Creatures")]
        [Tooltip("Crawls out of the hive mouth, roams around the mound and goes back in.")]
        [SerializeField] private EnemyDefinition[] _nestCreatures = new EnemyDefinition[0];
        [Tooltip("Sits in the hive mouth and watches.")]
        [SerializeField] private EnemyDefinition _nestGuardian;
        [SerializeField] private EnemyDefinition _flyer;
        [SerializeField] private EnemyDefinition _waterCreature;
        [SerializeField] private EnemyDefinition _lavaCreature;
        [SerializeField] private EnemyDefinition _sandCreature;

        public VisualTheme ForestTheme => _forestTheme;
        public VisualTheme LavaTheme => _lavaTheme;
        public VisualTheme RuinsTheme => _ruinsTheme;
        public VisualTheme NestTheme => _nestTheme;
        public EnemyDefinition[] NestCreatures => _nestCreatures;
        public EnemyDefinition NestGuardian => _nestGuardian;
        public EnemyDefinition Flyer => _flyer;
        public EnemyDefinition WaterCreature => _waterCreature;
        public EnemyDefinition LavaCreature => _lavaCreature;
        public EnemyDefinition SandCreature => _sandCreature;

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
