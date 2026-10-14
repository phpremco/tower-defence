using UnityEngine;

namespace Game.Levels
{
    /// <summary>Look of one level as data: palette, light, fog and bloom. Changing it needs no code.</summary>
    [CreateAssetMenu(menuName = "Game/Visual Theme", fileName = "VisualTheme")]
    public sealed class VisualTheme : ScriptableObject
    {
        [SerializeField] private Color _backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        [SerializeField] private Color _groundColor = new Color(0.25f, 0.28f, 0.33f);
        [SerializeField] private Color _pathColor = new Color(0.12f, 0.14f, 0.18f);
        [SerializeField] private Color _slotColor = new Color(0.2f, 0.38f, 0.6f);
        [SerializeField] private Color _baseColor = new Color(0.3f, 0.7f, 1f);
        [Header("Models")]
        [SerializeField] private GameObject _groundTile;
        [SerializeField] private GameObject _roadStraight;
        [SerializeField] private GameObject _roadCorner;
        [SerializeField] private GameObject _roadEnd;
        [SerializeField] private GameObject _roadCross;
        [SerializeField] private GameObject _baseModel;
        [SerializeField] private GameObject _spawnModel;
        [SerializeField] private GameObject[] _blockedProps = new GameObject[0];
        [SerializeField] private GameObject[] _scenery = new GameObject[0];

        [Header("Light and atmosphere")]
        [SerializeField] private Color _lightColor = new Color(0.8f, 0.85f, 1f);
        [SerializeField] [Min(0f)] private float _lightIntensity = 1.1f;
        [SerializeField] private Color _fogColor = new Color(0.06f, 0.08f, 0.12f);
        [SerializeField] [Min(0f)] private float _fogDensity = 0.015f;
        [SerializeField] [Min(0f)] private float _bloomIntensity = 1.2f;

        public Color BackgroundColor => _backgroundColor;
        public Color GroundColor => _groundColor;
        public Color PathColor => _pathColor;
        public Color SlotColor => _slotColor;
        public Color BaseColor => _baseColor;
        public GameObject GroundTile => _groundTile;
        public GameObject RoadStraight => _roadStraight;
        public GameObject RoadCorner => _roadCorner;
        public GameObject RoadEnd => _roadEnd;
        public GameObject RoadCross => _roadCross;
        public GameObject BaseModel => _baseModel;
        public GameObject SpawnModel => _spawnModel;

        /// <summary>Rocks and crystals placed on blocked cells.</summary>
        public GameObject[] BlockedProps => _blockedProps;

        /// <summary>Props scattered on the ground around the playing field.</summary>
        public GameObject[] Scenery => _scenery;

        public Color LightColor => _lightColor;
        public float LightIntensity => _lightIntensity;
        public Color FogColor => _fogColor;
        public float FogDensity => _fogDensity;
        public float BloomIntensity => _bloomIntensity;
    }
}



