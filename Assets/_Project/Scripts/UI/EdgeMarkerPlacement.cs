using UnityEngine;

namespace Game.UI
{
    /// <summary>Where <see cref="MarkerLayout"/> put an edge marker: the arrow, its word and the block the two fill together.</summary>
    public readonly struct EdgeMarkerPlacement
    {
        public EdgeMarkerPlacement(Vector2 arrow, Vector2 label, Rect block, float angle)
        {
            Arrow = arrow;
            Label = label;
            Block = block;
            Angle = angle;
        }

        /// <summary>Centre of the arrow.</summary>
        public Vector2 Arrow { get; }

        /// <summary>Centre of the word.</summary>
        public Vector2 Label { get; }

        /// <summary>The arrow (at any turn) and the word as one rect.</summary>
        public Rect Block { get; }

        /// <summary>The arrow's turn in degrees, from where it sits towards the real target.</summary>
        public float Angle { get; }
    }
}
