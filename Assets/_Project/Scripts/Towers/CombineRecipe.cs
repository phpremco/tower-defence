using UnityEngine;

namespace Game.Towers
{
    /// <summary>A fixed recipe: two neighbouring towers of these kinds become one hybrid tower, for a price.</summary>
    [CreateAssetMenu(menuName = "Game/Combine Recipe", fileName = "CombineRecipe")]
    public sealed class CombineRecipe : ScriptableObject
    {
        [SerializeField] private TowerDefinition _first;
        [SerializeField] private TowerDefinition _second;
        [SerializeField] private TowerDefinition _result;
        [SerializeField] [Min(0)] private int _cost = 100;

        public TowerDefinition First => _first;
        public TowerDefinition Second => _second;
        public TowerDefinition Result => _result;
        public int Cost => _cost;

        /// <summary>True when the recipe takes exactly these two kinds, in either order.</summary>
        public bool Matches(TowerDefinition a, TowerDefinition b)
        {
            return (a == _first && b == _second) || (a == _second && b == _first);
        }

        private void OnValidate()
        {
            if (_first == null || _second == null || _result == null)
            {
                Debug.LogWarning($"CombineRecipe '{name}' needs two ingredients and a result.", this);
            }
        }
    }
}
