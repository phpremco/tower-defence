using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// A rocket plume for the space shots: a wide soft cone of glowing gas (<c>Game/ExhaustPlume</c>), a narrow white-hot
    /// core inside it, a flare on the bell's mouth and, where the profile renders the opaque texture, a quad of heat
    /// shimmer over the plume (<c>Game/VfxDistortion</c>). The owner poses it at a nozzle each frame with a throttle; the
    /// length flickers on the cutscene clock, so a sought frame looks the same.
    /// </summary>
    public sealed class ExhaustPlume
    {
        public const string ShaderName = "Game/ExhaustPlume";
        public const string ShimmerShader = "Game/VfxDistortion";

        private const int Sides = 18;
        private const int Rings = 10;
        // How far the cone opens: the tail is this many nozzle radii across more than the mouth (gas spreads in vacuum).
        private const float Flare = 1.5f;
        private const float CoreWidth = 0.45f;
        private const float CoreLength = 0.4f;
        // The soft gas round the core is dimmer than the core, so the white-hot middle stands out of it.
        private const float OuterShare = 0.45f;
        private const float CoreShare = 0.75f;
        private const float FlareSize = 2.6f;
        private const float ShimmerWidth = 3.2f;
        private const float ShimmerStrength = 0.014f;
        private const int ShimmerTexture = 64;
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ClockId = Shader.PropertyToID("_Clock");
        private static readonly Vector2 ShimmerScroll = new Vector2(0f, -1.6f);

        private static Mesh _cone;
        private static Mesh _shimmerQuad;
        private static Texture2D _shimmerTexture;

        private readonly Transform _outer;
        private readonly Transform _core;
        private readonly Transform _flare;
        private readonly Transform _shimmer;
        private readonly Material _outerMaterial;
        private readonly Material _coreMaterial;
        private readonly Material _flareMaterial;
        private readonly float _intensity;

        /// <param name="core">The white-hot colour at the mouth (HDR).</param>
        /// <param name="edge">The colour of the gas at the soft edge (HDR).</param>
        /// <param name="shimmer">Adds the heat shimmer; only where the opaque texture is rendered (the high profile).</param>
        public ExhaustPlume(Transform parent, string name, Color core, Color edge, float intensity, bool shimmer)
        {
            _intensity = intensity;
            _outerMaterial = PlumeMaterial(core, edge, 2.2f, 1.3f, 0.35f, 0.45f);
            _coreMaterial = PlumeMaterial(core, Color.Lerp(core, edge, 0.4f), 3.2f, 2.4f, 0.7f, 0.9f);
            _outer = Cone(parent, name + "Plume", _outerMaterial);
            _core = Cone(parent, name + "Core", _coreMaterial);
            Color flareColour = Color.Lerp(core, edge, 0.5f);
            _flare = SpaceKit.Glow(parent, name + "Flare", flareColour, CutsceneSprite.EngineGlow);
            _flareMaterial = _flare.GetComponent<MeshRenderer>().sharedMaterial;
            if (shimmer)
            {
                _shimmer = BuildShimmer(parent, name + "Shimmer");
            }

            Set(false);
        }

        /// <summary>
        /// Poses the plume at a nozzle: its mouth, the way the gas leaves, the mouth's radius and the full length, at a
        /// throttle of 0 (off) to 1 (full burn). A throttle at zero hides every part.
        /// </summary>
        public void Pose(Vector3 mouth, Vector3 direction, float radius, float length, float throttle, float clock, Transform view)
        {
            bool on = throttle > 0.001f && direction.sqrMagnitude > 1e-6f;
            Set(on);
            if (!on)
            {
                return;
            }

            Vector3 along = direction.normalized;
            float flicker = 1f + 0.06f * Mathf.Sin(clock * 31f) + 0.04f * Mathf.Sin(clock * 53f + 1.3f);
            float reach = length * flicker * Mathf.Lerp(0.35f, 1f, throttle);
            Quaternion facing = Quaternion.LookRotation(along);
            _outer.SetPositionAndRotation(mouth, facing);
            _outer.localScale = new Vector3(radius, radius, reach);
            _core.SetPositionAndRotation(mouth, facing);
            _core.localScale = new Vector3(radius * CoreWidth, radius * CoreWidth, reach * CoreLength);
            _outerMaterial.SetFloat(IntensityId, _intensity * throttle * OuterShare);
            _outerMaterial.SetFloat(ClockId, clock);
            _coreMaterial.SetFloat(IntensityId, _intensity * throttle * CoreShare);
            _coreMaterial.SetFloat(ClockId, clock + 0.37f);
            _flare.SetPositionAndRotation(mouth + along * (radius * 0.2f), view.rotation);
            _flare.localScale = Vector3.one * (radius * FlareSize * (0.6f + 0.4f * throttle) * flicker);
            // The flare wears the atlas shader (round 50), whose colour is set once; the throttle dims it through its intensity,
            // turned linear as the old material turned the throttled colour (SpaceKit.Additive).
            _flareMaterial.SetFloat(IntensityId, Mathf.GammaToLinearSpace(throttle));
            if (_shimmer != null)
            {
                // A long quad down the plume, turned about the plume's axis to face the camera.
                Vector3 centre = mouth + along * (reach * 0.6f);
                Vector3 toEye = view.position - centre;
                Vector3 side = Vector3.Cross(along, toEye);
                if (side.sqrMagnitude < 1e-6f)
                {
                    side = Vector3.Cross(along, Vector3.up);
                }

                Vector3 normal = Vector3.Cross(side.normalized, along);
                _shimmer.SetPositionAndRotation(centre, Quaternion.LookRotation(normal, along));
                _shimmer.localScale = new Vector3(radius * ShimmerWidth, reach * 1.2f, 1f);
            }
        }

        private void Set(bool on)
        {
            if (_outer.gameObject.activeSelf == on)
            {
                return;
            }

            _outer.gameObject.SetActive(on);
            _core.gameObject.SetActive(on);
            _flare.gameObject.SetActive(on);
            _shimmer?.gameObject.SetActive(on);
        }

        private static Material PlumeMaterial(Color core, Color edge, float edgePower, float lengthFade, float coreLength, float diamonds)
        {
            Shader shader = Shader.Find(ShaderName);
            var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit")) { name = ShaderName };
            material.SetColor("_CoreColor", core);
            material.SetColor("_EdgeColor", edge);
            material.SetFloat("_EdgePower", edgePower);
            material.SetFloat("_LengthFade", lengthFade);
            material.SetFloat("_CoreLength", coreLength);
            material.SetFloat("_Diamonds", diamonds);
            return material;
        }

        private static Transform Cone(Transform parent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        private static Transform BuildShimmer(Transform parent, string name)
        {
            Shader shader = Shader.Find(ShimmerShader);
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader) { name = ShimmerShader };
            material.SetTexture("_MainTex", ShimmerTextureOf());
            material.SetFloat("_Strength", ShimmerStrength);
            material.SetVector("_Scroll", ShimmerScroll);
            material.SetFloat("_RadialMask", 1f);
            material.SetFloat("_UseMask", 0f);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = ShimmerQuad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        // A unit cone along +Z, its radius one at the mouth and opening towards the tail; UV.y runs along it.
        private static Mesh ConeMesh()
        {
            if (_cone != null)
            {
                return _cone;
            }

            int columns = Sides + 1;
            var vertices = new Vector3[columns * (Rings + 1)];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (int r = 0; r <= Rings; r++)
            {
                float z = (float)r / Rings;
                float radius = 1f + Flare * Mathf.Pow(z, 0.7f);
                float slope = Flare * 0.7f * Mathf.Pow(Mathf.Max(z, 0.02f), -0.3f);
                for (int s = 0; s < columns; s++)
                {
                    float u = (float)s / Sides;
                    float angle = u * Mathf.PI * 2f;
                    var round = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                    int i = r * columns + s;
                    vertices[i] = round * radius + Vector3.forward * z;
                    normals[i] = (round - Vector3.forward * slope).normalized;
                    uvs[i] = new Vector2(u, z);
                }
            }

            var triangles = new int[Sides * Rings * 6];
            int t = 0;
            for (int r = 0; r < Rings; r++)
            {
                for (int s = 0; s < Sides; s++)
                {
                    int a = r * columns + s;
                    int b = a + columns;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                }
            }

            _cone = new Mesh { name = "PlumeCone", vertices = vertices, normals = normals, uv = uvs, triangles = triangles };
            _cone.RecalculateBounds();
            return _cone;
        }

        // A unit quad from y 0 to 1 with white vertex colours (the distortion fades by their alpha).
        private static Mesh ShimmerQuad()
        {
            if (_shimmerQuad != null)
            {
                return _shimmerQuad;
            }

            _shimmerQuad = new Mesh { name = "ShimmerQuad" };
            _shimmerQuad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            _shimmerQuad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            _shimmerQuad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            _shimmerQuad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _shimmerQuad.RecalculateBounds();
            return _shimmerQuad;
        }

        // Smooth noise in R and G (0.5 is no offset), tiling, so the scrolling shimmer never shows a seam.
        private static Texture2D ShimmerTextureOf()
        {
            if (_shimmerTexture != null)
            {
                return _shimmerTexture;
            }

            const int size = ShimmerTexture;
            const float scale = 6f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float r = TiledNoise(u, v, scale, 0f);
                    float g = TiledNoise(u, v, scale, 17f);
                    pixels[y * size + x] = new Color(r, g, 1f, 1f);
                }
            }

            _shimmerTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true) { name = "PlumeShimmer", wrapMode = TextureWrapMode.Repeat };
            _shimmerTexture.SetPixels(pixels);
            _shimmerTexture.Apply();
            return _shimmerTexture;
        }

        private static float TiledNoise(float u, float v, float scale, float seed)
        {
            float a = Mathf.Sin((u * scale + seed) * Mathf.PI * 2f);
            float b = Mathf.Sin((v * scale * 1.5f + seed * 0.3f) * Mathf.PI * 2f + a * 1.7f);
            float c = Mathf.Sin(((u + v) * scale * 2f + seed) * Mathf.PI * 2f + b);
            return Mathf.Clamp01(0.5f + 0.22f * a * b + 0.18f * c);
        }
    }
}
