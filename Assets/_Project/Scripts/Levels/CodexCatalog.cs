using System.Collections.Generic;
using Game.Enemies;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// What the in-game codex can show: every combine recipe in the campaign and every creature, in the order the
    /// generator lists them. Pure data; the codex screen reads the numbers off the definitions themselves.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Codex Catalog", fileName = "CodexCatalog")]
    public sealed class CodexCatalog : ScriptableObject
    {
        [SerializeField] private List<CombineRecipe> _recipes = new List<CombineRecipe>();
        [SerializeField] private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();

        public IReadOnlyList<CombineRecipe> Recipes => _recipes;
        public IReadOnlyList<EnemyDefinition> Enemies => _enemies;
    }
}
