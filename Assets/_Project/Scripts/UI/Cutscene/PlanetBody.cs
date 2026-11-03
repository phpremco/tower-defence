using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// A planet standing in a cutscene set as real geometry, lit by the set's own sun: the painted surface (relief,
    /// gloss and lights), a cloud or smog deck that drifts a little faster than the ground, and a shell of
    /// atmosphere that glows along the rim. It shares the sphere and the materials of the map's globe, so both
    /// planets read alike. Posed from time alone.
    /// </summary>
    public sealed class PlanetBody
    {
        private const float CloudLift = 1.012f;
        private const float AtmosphereLift = 1.06f;

        private static Mesh _sphere;

        private readonly Transform _surface;
        private readonly Transform _clouds;
        private readonly Quaternion _tilt;
        private readonly float _degreesPerSecond;
        private readonly float _cloudDegreesPerSecond;

        /// <param name="tilt">The axis tilt and the turn the planet starts in.</param>
        /// <param name="degreesPerSecond">How fast the ground turns; the clouds turn a little faster.</param>
        public PlanetBody(Transform parent, string name, Vector3 centre, float radius, GlobePainter.Maps maps, Texture2D clouds, float cloudAlpha, Color atmosphere, float rimPower, Quaternion tilt, float degreesPerSecond)
        {
            Root = new GameObject(name).transform;
            Root.SetParent(parent, false);
            Root.localPosition = centre;
            Root.localScale = Vector3.one * radius;
            Radius = radius;
            _tilt = tilt;
            _degreesPerSecond = degreesPerSecond;
            _cloudDegreesPerSecond = degreesPerSecond * 1.3f;

            _surface = Sphere("Surface", 1f, PlanetGlobe.Surface(maps));
            if (clouds != null)
            {
                _clouds = Sphere("Clouds", CloudLift, PlanetGlobe.CloudMaterial(clouds, cloudAlpha));
            }

            Sphere("Atmosphere", AtmosphereLift, PlanetGlobe.AtmosphereMaterial(atmosphere, rimPower));
            Pose(0f);
        }

        public Transform Root { get; }

        public float Radius { get; }

        public Vector3 Centre => Root.position;

        /// <summary>The ground's turn at the last pose, to put things on the surface.</summary>
        public Quaternion Rotation => _surface.rotation;

        public void Pose(float time)
        {
            _surface.localRotation = _tilt * Quaternion.AngleAxis(-time * _degreesPerSecond, Vector3.up);
            if (_clouds != null)
            {
                _clouds.localRotation = _tilt * Quaternion.AngleAxis(-time * _cloudDegreesPerSecond + 40f, Vector3.up);
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
