using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// A planet standing in a cutscene set as real geometry: the ground, a cloud deck that drifts a little faster
    /// than the ground and casts its shadows on it, and a shell of air that glows past the limb. It wears the same
    /// <see cref="PlanetMaterials"/> as the map's globe, so both planets read alike; the set's sun lights it through
    /// <see cref="SetSun"/>. Posed from time alone.
    /// </summary>
    public sealed class PlanetBody
    {
        private const float CloudLift = 1.012f;
        private const float CloudDrift = 1.3f;
        private const float CloudStartTurn = 40f;

        private static Mesh _sphere;

        private readonly Transform _surface;
        private readonly Transform _clouds;
        private readonly Material _surfaceMaterial;
        private readonly Material _cloudMaterial;
        private readonly Material _atmosphereMaterial;
        private readonly Quaternion _tilt;
        private readonly float _degreesPerSecond;
        private readonly float _cloudDegreesPerSecond;

        /// <param name="tilt">The axis tilt and the turn the planet starts in.</param>
        /// <param name="degreesPerSecond">How fast the ground turns; the clouds turn a little faster.</param>
        /// <param name="surface">The ground material (<see cref="PlanetMaterials.Surface(GlobePainter.Maps, Texture2D, PlanetStyle, bool)"/> or its painted twin); the body owns it from here.</param>
        /// <param name="cloudWisps">How far noise tears the cloud map into wisps; the style's amount when negative.</param>
        public PlanetBody(Transform parent, string name, Vector3 centre, float radius, Material surface, Texture2D clouds, float cloudAlpha, PlanetStyle style, Quaternion tilt, float degreesPerSecond, float cloudWisps = -1f)
        {
            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
            Root.localPosition = centre;
            Root.localScale = Vector3.one * radius;
            Radius = radius;
            _tilt = tilt;
            _degreesPerSecond = degreesPerSecond;
            _cloudDegreesPerSecond = degreesPerSecond * CloudDrift;

            _surfaceMaterial = surface;
            _surface = Sphere("Surface", 1f, _surfaceMaterial);
            if (clouds != null)
            {
                _cloudMaterial = PlanetMaterials.Clouds(clouds, cloudAlpha, style, cloudWisps);
                _clouds = Sphere("Clouds", CloudLift, _cloudMaterial);
            }

            _atmosphereMaterial = PlanetMaterials.Atmosphere(style, style.AtmosphereLift);
            Sphere("Atmosphere", style.AtmosphereLift, _atmosphereMaterial);
            Pose(0f);
        }

        public Transform Root { get; }

        public float Radius { get; }

        public Vector3 Centre => Root.position;

        /// <summary>The ground's turn at the last pose, to put things on the surface.</summary>
        public Quaternion Rotation => _surface.rotation;

        /// <summary>Lights every layer with the set's sun.</summary>
        public void SetSun(Light sun)
        {
            PlanetMaterials.SetSun(_surfaceMaterial, sun);
            PlanetMaterials.SetSun(_atmosphereMaterial, sun);
            if (_cloudMaterial != null)
            {
                PlanetMaterials.SetSun(_cloudMaterial, sun);
            }
        }

        /// <summary>The cloud deck takes its colour from its map (a painted smog deck); no deck, no change.</summary>
        public void TintCloudsFromMap()
        {
            if (_cloudMaterial != null)
            {
                PlanetMaterials.SetCloudMapColour(_cloudMaterial, 1f);
            }
        }

        public void Pose(float time)
        {
            _surface.localRotation = _tilt * Quaternion.AngleAxis(-time * _degreesPerSecond, Vector3.up);
            if (_clouds != null)
            {
                _clouds.localRotation = _tilt * Quaternion.AngleAxis(-time * _cloudDegreesPerSecond + CloudStartTurn, Vector3.up);
                PlanetMaterials.SetCloudFrame(_surfaceMaterial, _clouds);
            }
        }

        private Transform Sphere(string name, float scale, Material material)
        {
            if (_sphere == null)
            {
                _sphere = PlanetGlobe.SphereMesh();
            }

            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = _sphere;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }
    }
}
