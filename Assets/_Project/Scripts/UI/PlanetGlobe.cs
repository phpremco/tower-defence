using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The planet as a real sphere inside the interface: a small rig far from the scene (globe with relief, gloss
    /// and lights, two layers of cloud, a shell of atmosphere, lights and a camera) renders into a texture a
    /// RawImage shows. The screen turns and zooms it and asks where a place on the surface lands on the image, so
    /// markers drawn in the canvas stay glued to the surface.
    /// </summary>
    public sealed class PlanetGlobe : MonoBehaviour
    {
        private const int TextureSize = 2048;
        private const float RigDistance = 5000f;
        private const float Radius = 1f;
        private const float CloudRadius = 1.016f;
        private const float HighCloudRadius = 1.03f;
        private const float HighCloudAlpha = 0.4f;
        private const float HighCloudDegreesPerSecond = -0.5f;
        private const float FieldOfView = 30f;
        private const float FarDistance = 9.4f;
        private const float RegionDistance = 7.2f;
        private const float NearDistance = 5f;
        private const float ZoomShift = 0.6f;
        private const float HighlightCloseFade = 0.5f;
        private const float DefaultCloudClear = 0.5f;
        // Fully thinned over the heart of the region, back to full cover a little past its rim.
        private const float ClearInnerShare = 0.55f;
        private const float ClearOuterShare = 1.1f;

        /// <summary>Zoom value at which one region fills the view, with its levels readable.</summary>
        public const float RegionZoom = 1f;

        /// <summary>Zoom value of the close-up on one level, slid aside to leave room for the panel.</summary>
        public const float LevelZoom = 2f;
        private const float ZoomSeconds = 0.55f;
        private const float TurnFollow = 7f;
        private const float MaxPitch = 72f;
        private const float CloudDegreesPerSecond = 0.9f;
        private const int Rings = 48;
        private const int Segments = 96;
        // The image is drawn larger than the globe looks from afar, so the sphere stays inside it when the view flies in.
        private const float ImageScale = 2f;
        private static readonly Color KeyColor = new Color(1f, 0.96f, 0.88f);
        private static readonly Color FillColor = new Color(0.4f, 0.55f, 0.95f);
        private static readonly Color BackColor = new Color(0.5f, 0.7f, 1f);

        private Transform _rig;
        private Transform _globe;
        private Transform _clouds;
        private Transform _highClouds;
        private Material _cloudMaterial;
        private Material _highCloudMaterial;
        private Material _surfaceMaterial;
        private PlanetStyle _style;
        private PlanetTextureSet _painted;
        private int _highlightId = -1;
        private Color _highlightColour;
        private float _highlightStrength;
        private Vector3 _clearDirection;
        private float _clearRadius;
        private Camera _camera;
        private RenderTexture _texture;
        private RectTransform _rect;
        private float _yaw;
        private float _pitch;
        private float _targetYaw;
        private float _targetPitch;
        private float _zoom;
        private bool _snapping;

        /// <summary>How far the view has zoomed in: 0 for the whole planet, RegionZoom for one region, LevelZoom for the close-up on a level.</summary>
        public float Zoom => _zoom;

        public float ZoomTarget { get; set; }

        /// <summary>True while a snap turn towards a chosen place is still under way.</summary>
        public bool Turning => _snapping && (Mathf.Abs(Mathf.DeltaAngle(_yaw, _targetYaw)) > 0.5f || Mathf.Abs(_pitch - _targetPitch) > 0.5f);

        /// <param name="painted">The planet painted in Blender: when set it replaces <paramref name="surface"/> and the cloud
        /// maps, and <paramref name="surface"/>'s emission stays as the colony lights over its glow.</param>
        public static PlanetGlobe Create(RectTransform parent, float size, GlobePainter.Maps surface, Texture2D[] clouds, PlanetStyle style, PlanetTextureSet painted = null)
        {
            var go = new GameObject("PlanetGlobe", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.raycastTarget = false;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size * ImageScale, size * ImageScale);

            var globe = go.AddComponent<PlanetGlobe>();
            globe._rect = rect;
            globe._style = style;
            globe._painted = painted;
            globe.Build(image, surface, clouds);
            return globe;
        }

        /// <summary>
        /// Marks a region of a painted planet (brighter land, a soft rim in its colour along the coast, the rest a little
        /// darker), by its index in the campaign; a negative index clears it. A plain globe has no region map and ignores it.
        /// </summary>
        /// <param name="direction">The region's centre on the globe; the cloud decks thin over it out to <paramref name="radiusDegrees"/>.</param>
        public void Highlight(int regionIndex, Color colour, bool open, Vector3 direction = default, float radiusDegrees = 0f)
        {
            _clearDirection = regionIndex >= 0 ? direction : Vector3.zero;
            _clearRadius = radiusDegrees;
            _highlightId = _painted != null && regionIndex >= 0 ? _painted.RegionId(regionIndex) : -1;
            _highlightColour = colour;
            _highlightStrength = _painted == null ? 0f : open ? 1f : _painted.LockedShare;
        }

        /// <summary>Turns the globe by hand: x spins it, y tilts it. Cancels any snap turn under way.</summary>
        public void Spin(Vector2 degrees)
        {
            _snapping = false;
            _targetYaw = _yaw + degrees.x;
            _targetPitch = Mathf.Clamp(_pitch + degrees.y, -MaxPitch, MaxPitch);
            _yaw = _targetYaw;
            _pitch = _targetPitch;
        }

        /// <summary>Glides the globe until the place faces the camera.</summary>
        public void TurnTo(Vector3 direction)
        {
            Vector2 turn = PlanetCoordinates.TurnToFront(direction);
            _targetYaw = _yaw + Mathf.DeltaAngle(_yaw, turn.x);
            _targetPitch = Mathf.Clamp(turn.y, -MaxPitch, MaxPitch);
            _snapping = true;
        }

        /// <summary>Puts the globe at once in the turn that shows the place, for the first frame.</summary>
        public void SnapTo(Vector3 direction)
        {
            TurnTo(direction);
            _yaw = _targetYaw;
            _pitch = _targetPitch;
            Apply();
        }

        /// <summary>
        /// Where a place on the surface lands on the image, relative to its centre, and how squarely it faces the
        /// camera (1 in the middle, 0 on the rim, negative on the far side).
        /// </summary>
        public Vector2 Project(Vector3 direction, out float facing)
        {
            Vector3 surface = _globe.rotation * direction;
            Vector3 world = _globe.position + surface * Radius;
            facing = Vector3.Dot(surface, (_camera.transform.position - world).normalized);
            return ToImage(world);
        }

        /// <summary>The centre of the globe on the image, and its apparent radius in image pixels.</summary>
        public Vector2 Centre(out float radiusPixels)
        {
            Vector2 centre = ToImage(_globe.position);
            Vector2 rim = ToImage(_globe.position + _camera.transform.right * Radius);
            radiusPixels = Vector2.Distance(centre, rim);
            return centre;
        }

        // Projected by hand with the square aspect the texture has, so markers land where the camera drew the surface
        // whatever aspect the editor or the screen reports for the camera.
        private Vector2 ToImage(Vector3 world)
        {
            Vector3 local = _camera.transform.InverseTransformPoint(world);
            float halfExtent = Mathf.Max(0.001f, local.z) * Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            return new Vector2(local.x / halfExtent * 0.5f * _rect.rect.width, local.y / halfExtent * 0.5f * _rect.rect.height);
        }

        private void Build(RawImage image, GlobePainter.Maps surface, Texture2D[] clouds)
        {
            _texture = new RenderTexture(TextureSize, TextureSize, 16);
            image.texture = _texture;

            var rig = new GameObject("GlobeRig");
            rig.transform.position = new Vector3(0f, 0f, -RigDistance);
            _rig = rig.transform;

            // The same planet materials as the cutscene, with the style's map overrides so every region stays readable.
            // A painted planet brings its own cloud map; both decks wear it, turned apart, so they never line up.
            Texture2D lowDeck = clouds[0];
            Texture2D highDeck = clouds[clouds.Length - 1];
            float wisps = -1f;
            if (_painted != null)
            {
                _surfaceMaterial = PlanetMaterials.Surface(_painted, surface.Emission, _style, forMap: true);
                lowDeck = _painted.For(ModelQuality.IsLow).Clouds;
                highDeck = lowDeck;
                wisps = _painted.CloudWisps;
            }
            else
            {
                _surfaceMaterial = PlanetMaterials.Surface(surface, lowDeck, _style, forMap: true);
            }

            _globe = Sphere("Globe", Radius, _surfaceMaterial).transform;
            _cloudMaterial = PlanetMaterials.Clouds(lowDeck, 1f, _style, wisps);
            _clouds = Sphere("Clouds", CloudRadius, _cloudMaterial).transform;
            _highCloudMaterial = PlanetMaterials.Clouds(highDeck, HighCloudAlpha, _style, wisps);
            _highClouds = Sphere("HighClouds", HighCloudRadius, _highCloudMaterial).transform;
            Material air = PlanetMaterials.Atmosphere(_style, _style.AtmosphereLift);
            Sphere("Atmosphere", _style.AtmosphereLift, air);

            var cameraGo = new GameObject("GlobeCamera");
            cameraGo.transform.SetParent(_rig, false);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.fieldOfView = FieldOfView;
            _camera.targetTexture = _texture;
            _camera.aspect = 1f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 20f;

            Light key = AddLight("Key", new Vector3(28f, -40f, 0f), KeyColor, 2f);
            foreach (Material material in new[] { _surfaceMaterial, _cloudMaterial, _highCloudMaterial, air })
            {
                PlanetMaterials.SetSun(material, key);
            }

            AddLight("Fill", new Vector3(-10f, 150f, 0f), FillColor, 0.2f);
            AddLight("Back", new Vector3(-20f, 200f, 0f), BackColor, 0.5f);
            Apply();
        }

        private GameObject Sphere(string name, float radius, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rig, false);
            go.transform.localScale = Vector3.one * radius;
            go.AddComponent<MeshFilter>().sharedMesh = SphereMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        private Light AddLight(string name, Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rig, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        // A latitude-longitude sphere whose texture coordinates follow PlanetCoordinates.ToUv exactly, so the map
        // painted for it and the markers projected on it agree on where every place is.
        internal static Mesh SphereMesh()
        {
            var vertices = new Vector3[(Rings + 1) * (Segments + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            for (int r = 0; r <= Rings; r++)
            {
                float latitude = r / (float)Rings * 180f - 90f;
                for (int s = 0; s <= Segments; s++)
                {
                    float longitude = s / (float)Segments * 360f - 180f;
                    int index = r * (Segments + 1) + s;
                    Vector3 direction = PlanetCoordinates.ToDirection(longitude, latitude);
                    vertices[index] = direction;
                    normals[index] = direction;
                    uv[index] = PlanetCoordinates.ToUv(longitude, latitude);
                }
            }

            var triangles = new int[Rings * Segments * 6];
            int t = 0;
            for (int r = 0; r < Rings; r++)
            {
                for (int s = 0; s < Segments; s++)
                {
                    int a = r * (Segments + 1) + s;
                    int b = a + Segments + 1;
                    // Wound so the outside faces the camera; the other way round the far side shows through, mirrored.
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                }
            }

            var mesh = new Mesh { name = "Globe" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            // The normal map needs tangents along the longitude lines.
            mesh.RecalculateTangents();
            return mesh;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_snapping)
            {
                float k = 1f - Mathf.Exp(-TurnFollow * dt);
                _yaw += Mathf.DeltaAngle(_yaw, _targetYaw) * k;
                _pitch += (_targetPitch - _pitch) * k;
            }

            _zoom = Mathf.MoveTowards(_zoom, ZoomTarget, dt / ZoomSeconds);
            Apply();
        }

        private void Apply()
        {
            Quaternion rotation = PlanetCoordinates.Rotation(_yaw, _pitch);
            _globe.localRotation = rotation;
            // The two cloud decks drift at their own pace and the opposite way, so the weather has depth.
            _clouds.localRotation = rotation * Quaternion.AngleAxis(Time.unscaledTime * CloudDegreesPerSecond, Vector3.up);
            _highClouds.localRotation = rotation * Quaternion.AngleAxis(Time.unscaledTime * HighCloudDegreesPerSecond + 180f, Vector3.up);

            // The first stage of the zoom flies onto a region; the second comes closer still and moves the camera
            // sideways, so the chosen level slides left and leaves room for the panel.
            float region = Ease(Mathf.Clamp01(_zoom / RegionZoom));
            float level = Ease(Mathf.Clamp01((_zoom - RegionZoom) / (LevelZoom - RegionZoom)));
            float distance = Mathf.Lerp(Mathf.Lerp(FarDistance, RegionDistance, region), NearDistance, level);
            // Clouds thin out on the way in, so the levels sit on clear land.
            float cover = (_painted != null ? _painted.MapCloudShare : 1f) * (1f - region);
            PlanetMaterials.SetCloudAlpha(_cloudMaterial, cover);
            PlanetMaterials.SetCloudAlpha(_highCloudMaterial, HighCloudAlpha * cover);
            ClearOverRegion(_cloudMaterial, _clouds);
            ClearOverRegion(_highCloudMaterial, _highClouds);
            PlanetMaterials.SetCloudShadow(_surfaceMaterial, _style.CloudShadow * (1f - region));
            PlanetMaterials.SetCloudFrame(_surfaceMaterial, _clouds);
            if (_painted != null)
            {
                // The mark softens in the close-up on a level, where the level's own panel takes over.
                PlanetMaterials.SetSelectedRegion(_surfaceMaterial, _highlightId, _highlightColour, _highlightStrength * (1f - HighlightCloseFade * level));
            }

            _camera.transform.localPosition = new Vector3(ZoomShift * level, 0f, -distance);
        }

        // The region's centre in the deck's own frame (the decks drift on their own), thinned to its rim.
        private void ClearOverRegion(Material deck, Transform frame)
        {
            bool clear = _clearDirection.sqrMagnitude > 0f && _clearRadius > 0f;
            float share = clear ? (_painted != null ? _painted.RegionCloudClear : DefaultCloudClear) : 0f;
            Vector3 local = clear ? frame.InverseTransformDirection(_globe.TransformDirection(_clearDirection)) : Vector3.forward;
            PlanetMaterials.SetCloudClearing(deck, local, _clearRadius * ClearInnerShare, _clearRadius * ClearOuterShare, share);
        }

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private void OnDestroy()
        {
            if (_rig != null)
            {
                Destroy(_rig.gameObject);
            }

            if (_texture != null)
            {
                _texture.Release();
            }
        }
    }
}
