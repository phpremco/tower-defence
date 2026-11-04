using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// One effect texture painted in Blender, ready to wear: its material for each quality profile and how its atlas
    /// is laid out (cells, frames, playback rate, life, the world size it was simulated at, its pivot). Generated from
    /// the export's report by the VFX builder; effects read it, they never hard-code a texture.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Vfx Sheet", fileName = "VfxSheet")]
    public sealed class VfxSheet : ScriptableObject
    {
        [SerializeField] private Material _material;
        [Tooltip("The Low (Switch) material: half-size texture, no soft particles, no noise layer.")]
        [SerializeField] private Material _lowMaterial;
        [SerializeField] [Min(1)] private int _columns = 1;
        [SerializeField] [Min(1)] private int _rows = 1;
        [Tooltip("Frames that hold a picture (a sheet need not fill its last row).")]
        [SerializeField] [Min(1)] private int _frames = 1;
        [SerializeField] [Min(0f)] private float _fps;
        [Tooltip("How long one play of the flipbook lasts at its own rate, in seconds.")]
        [SerializeField] [Min(0f)] private float _lifetime;
        [Tooltip("Size in metres of one cell as simulated (six-way sheets); 0 when the texture has no own scale.")]
        [SerializeField] [Min(0f)] private float _worldSize;
        [Tooltip("Where in a cell the effect starts (U, V from the bottom left).")]
        [SerializeField] private Vector2 _pivot = new Vector2(0.5f, 0.5f);
        [Tooltip("A cell's height over its width, for cards that keep the painted proportions.")]
        [SerializeField] [Min(0.01f)] private float _cellAspect = 1f;

        public int Columns => _columns;
        public int Rows => _rows;
        public int Frames => _frames;
        public float Fps => _fps;
        public float Lifetime => _lifetime;
        public float WorldSize => _worldSize;
        public Vector2 Pivot => _pivot;
        public float CellAspect => _cellAspect;

        /// <summary>The material of a profile; the low profile falls back to the high one.</summary>
        public Material MaterialFor(bool low) => low && _lowMaterial != null ? _lowMaterial : _material;

        private void OnValidate()
        {
            if (_material == null)
            {
                Debug.LogWarning($"{name}: the effect sheet has no material.", this);
            }

            if (_frames > _columns * _rows)
            {
                Debug.LogWarning($"{name}: {_frames} frames do not fit a {_columns} x {_rows} atlas.", this);
            }
        }
    }
}
