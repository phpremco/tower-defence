using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The main-menu island as built in Blender (ArtSource README round 26): the island mesh with its Top and Cliff
    /// materials, the ground height map the creatures walk on, the camera, the pool outlines, the lights and every placed
    /// export. Written by the generator from <c>Menu_Island_layout.json</c>; <see cref="MenuDiorama"/> only places it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Menu Diorama Art", fileName = "MenuDioramaArt")]
    public sealed class MenuDioramaArt : ScriptableObject
    {
        [Header("Island")]
        [Tooltip("The island in world space (the generator splits it into the Top and Cliff sub-meshes and turns it into the layout's frame).")]
        [SerializeField] private Mesh _island;
        [Tooltip("Per sub-mesh of the island (Top, Cliff), High profile.")]
        [SerializeField] private Material[] _islandMaterials = new Material[0];
        [SerializeField] private Material[] _islandLowMaterials = new Material[0];

        [Header("Ground height (16-bit, ray cast onto the island)")]
        [SerializeField] private Texture2D _heightMap;
        [SerializeField] private float _minHeight;
        [SerializeField] private float _maxHeight;
        [Tooltip("World x/z of the height map's first texel corner (u = 0, v = 0).")]
        [SerializeField] private Vector2 _heightOrigin;
        [Tooltip("World size the height map spans along x and z.")]
        [SerializeField] private float _heightSize = 1f;

        [Header("Camera")]
        [SerializeField] private Vector3 _cameraFocus;
        [SerializeField] private float _cameraDistance = 15f;
        [SerializeField] private float _cameraPitch = 30f;
        [SerializeField] private float _cameraYaw;
        [SerializeField] private float _cameraFieldOfView = 36f;

        [Header("Layout")]
        [SerializeField] private MenuDioramaModel[] _models = new MenuDioramaModel[0];
        [SerializeField] private MenuDioramaPlacement[] _placements = new MenuDioramaPlacement[0];
        [SerializeField] private MenuDioramaPool[] _pools = new MenuDioramaPool[0];
        [SerializeField] private MenuDioramaLight[] _lights = new MenuDioramaLight[0];

        public Mesh Island => _island;
        public Material[] IslandMaterials(bool low) => low && _islandLowMaterials.Length == _islandMaterials.Length ? _islandLowMaterials : _islandMaterials;
        public Texture2D HeightMap => _heightMap;
        public float MinHeight => _minHeight;
        public float MaxHeight => _maxHeight;
        public Vector2 HeightOrigin => _heightOrigin;
        public float HeightSize => _heightSize;
        public Vector3 CameraFocus => _cameraFocus;
        public float CameraDistance => _cameraDistance;
        public float CameraPitch => _cameraPitch;
        public float CameraYaw => _cameraYaw;
        public float CameraFieldOfView => _cameraFieldOfView;
        public MenuDioramaModel[] Models => _models;
        public MenuDioramaPlacement[] Placements => _placements;
        public MenuDioramaPool[] Pools => _pools;
        public MenuDioramaLight[] Lights => _lights;

        /// <summary>True when the island and its height map are there; without them the menu shows no ground.</summary>
        public bool IsComplete => _island != null && _heightMap != null && _islandMaterials.Length == _island.subMeshCount && _heightSize > 0f;

        private void OnValidate()
        {
            foreach (MenuDioramaPlacement placement in _placements)
            {
                if (placement.Model < 0 || placement.Model >= _models.Length)
                {
                    Debug.LogWarning($"MenuDioramaArt '{name}': a placement points at model {placement.Model}, which does not exist.", this);
                    return;
                }
            }
        }
    }
}
