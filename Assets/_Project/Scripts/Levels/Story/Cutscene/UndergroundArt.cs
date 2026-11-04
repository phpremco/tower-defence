using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The cave under the field from Blender (README round 26): the kit pieces by <see cref="CavePiece"/> (walls with and
    /// without the eye crevice, ceiling slabs, stalactites, floors, boulders, crystal clusters, veins) and the three
    /// Burrower eyes. The underground set builds the cavern from them; without art it keeps its procedural rocks.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story/Underground Art", fileName = "UndergroundArt")]
    public sealed class UndergroundArt : ScriptableObject
    {
        [Tooltip("One mesh per CavePiece, in enum order.")]
        [SerializeField] private EnvironmentMesh[] _pieces = new EnvironmentMesh[0];
        [SerializeField] private BurrowerEyeArt[] _eyes = new BurrowerEyeArt[0];

        [Header("Look")]
        [Tooltip("HDR strength of the iris glow (README: 2.5 to 5 with bloom).")]
        [SerializeField] [Min(0f)] private float _irisGlow = 4f;
        [Tooltip("How far an eye's pivot (the eyeball centre) sits behind the rock face, in units of the eye's scale (the unit eye bulges 0.42 forward).")]
        [SerializeField] [Range(0f, 0.6f)] private float _seatDepth = 0.15f;

        public float IrisGlow => _irisGlow;
        public float SeatDepth => _seatDepth;
        public BurrowerEyeArt[] Eyes => _eyes;

        public EnvironmentMesh Piece(CavePiece piece)
        {
            int index = (int)piece;
            return index < _pieces.Length && _pieces[index] != null && _pieces[index].IsValid ? _pieces[index] : null;
        }

        /// <summary>True when every kit piece and at least one eye are there.</summary>
        public bool IsComplete
        {
            get
            {
                foreach (CavePiece piece in System.Enum.GetValues(typeof(CavePiece)))
                {
                    if (Piece(piece) == null)
                    {
                        return false;
                    }
                }

                foreach (BurrowerEyeArt eye in _eyes)
                {
                    if (eye != null && eye.IsValid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void OnValidate()
        {
            int count = System.Enum.GetValues(typeof(CavePiece)).Length;
            if (_pieces.Length != 0 && _pieces.Length != count)
            {
                Debug.LogWarning($"Underground art '{name}' holds {_pieces.Length} pieces; CavePiece has {count}.", this);
            }
        }
    }
}
