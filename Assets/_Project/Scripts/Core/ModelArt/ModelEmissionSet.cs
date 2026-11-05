using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A named look of a model's glow (README round 28: the Sealed One's Phase1..4 and Release maps): the emission map of
    /// the body slot and of the second slot, and how bright it burns against the material's rest glow. A view swaps a
    /// model to one by name; logic only names the set.
    /// </summary>
    [System.Serializable]
    public sealed class ModelEmissionSet
    {
        [SerializeField] private string _name = string.Empty;
        [SerializeField] private Texture2D _body;
        [SerializeField] private Texture2D _second;
        [SerializeField] private Texture2D _lowBody;
        [SerializeField] private Texture2D _lowSecond;
        [Tooltip("Glow against the material's rest emission strength (1 keeps it).")]
        [SerializeField] [Min(0f)] private float _strength = 1f;

        public ModelEmissionSet()
        {
        }

        public ModelEmissionSet(string name, Texture2D body, Texture2D second, Texture2D lowBody, Texture2D lowSecond, float strength)
        {
            _name = name;
            _body = body;
            _second = second;
            _lowBody = lowBody;
            _lowSecond = lowSecond;
            _strength = strength;
        }

        public string Name => _name;
        public float Strength => _strength;

        public Texture2D BodyFor(bool lowDetail) => lowDetail && _lowBody != null ? _lowBody : _body;

        public Texture2D SecondFor(bool lowDetail) => lowDetail && _lowSecond != null ? _lowSecond : _second;
    }
}
