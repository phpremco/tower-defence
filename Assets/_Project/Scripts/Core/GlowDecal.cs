using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// Soft pools and sheets of light that are added over whatever is below: a radial glow on the ground under a
    /// crystal, the shimmer of an energy field. Textures and meshes are generated once and shared.
    /// </summary>
    public static class GlowDecal
    {
        private const int TextureSize = 64;
        private const int ScanlineSize = 64;
        private const float DecalHeight = 0.035f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

        private static Texture2D _radial;
        private static Texture2D _scanlines;
        private static Texture2D _blade;
        private static Mesh _quad;
        private static readonly Dictionary<Color, Material> RadialMaterials = new Dictionary<Color, Material>();

        /// <summary>A pool of light on the ground, strongest in the middle and gone at the edge.</summary>
        public static GameObject PlacePool(Transform parent, Color colour, float radius, float height = DecalHeight)
        {
            MarkerArt art = MarkerArt.Active;
            MeshRenderer painted = art != null ? art.Place(MarkerKind.GlowPool, parent, "GlowPool") : null;
            if (painted != null)
            {
                // The painted pool lies flat already (README round 25b): scale = diameter.
                painted.transform.localPosition = new Vector3(0f, height, 0f);
                painted.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
                painted.sharedMaterial = RadialMaterial(colour);
                return painted.gameObject;
            }

            var go = new GameObject("GlowPool");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = RadialMaterial(colour);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>An additive material that fades out towards the edge of its quad.</summary>
        public static Material RadialMaterial(Color colour)
        {
            if (RadialMaterials.TryGetValue(colour, out Material cached) && cached != null)
            {
                return cached;
            }

            Material material = Painted(MarkerKind.GlowPool, colour) ?? Additive(Radial(), colour);
            RadialMaterials[colour] = material;
            return material;
        }

        /// <summary>
        /// An additive glow for a blade mesh whose UV x runs from the core (0) to the rim (1) and UV y from the base (0) to
        /// the tip (1): bright along the core, soft at the edge, fading out at both ends.
        /// </summary>
        public static Material BladeMaterial(Color colour) => Painted(MarkerKind.GlowBlade, colour) ?? Additive(Blade(), colour);

        /// <summary>An additive material with horizontal lines, for energy fields; scroll its texture to animate it.</summary>
        public static Material ScanlineMaterial(Color colour) => Painted(MarkerKind.EnergyField, colour) ?? Additive(Scanlines(), colour);

        /// <summary>Recolours a material from this class (painted or fallback) without knowing which it is.</summary>
        public static void SetColour(Material material, Color colour)
        {
            material.SetColor(EmissionColorId, colour);
            material.SetColor(MarkerArt.Tint, colour);
        }

        /// <summary>Moves the texture of a material from this class along, to animate a field or a streak.</summary>
        public static void SetScroll(Material material, Vector2 offset)
        {
            if (material.HasProperty(MainTexId))
            {
                material.SetTextureOffset(MainTexId, offset);
                return;
            }

            material.SetTextureOffset(BaseMapId, offset);
            material.SetTextureOffset(EmissionMapId, offset);
        }

        // README round 25b: the painted channel pack in a tinted copy of the marker material, or null without the art.
        private static Material Painted(MarkerKind kind, Color colour)
        {
            MarkerArt art = MarkerArt.Active;
            Material shared = art != null ? art.Material(kind) : null;
            if (shared == null)
            {
                return null;
            }

            var material = new Material(shared);
            material.SetColor(MarkerArt.Tint, colour);
            return material;
        }

        public static Mesh Quad()
        {
            if (_quad != null)
            {
                return _quad;
            }

            _quad = new Mesh { name = "GlowQuad" };
            _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            _quad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            _quad.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            _quad.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 };
            _quad.RecalculateBounds();
            return _quad;
        }

        /// <summary>An unlit additive material wearing <paramref name="texture"/> in <paramref name="colour"/> (black adds nothing).</summary>
        public static Material Additive(Texture2D texture, Color colour)
        {
            var material = PipelineMaterials.Lit();
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.black);
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", colour);
            return material;
        }

        private static Texture2D Radial()
        {
            if (_radial != null)
            {
                return _radial;
            }

            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (x + 0.5f) / TextureSize * 2f - 1f;
                    float v = (y + 0.5f) / TextureSize * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float value = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                    pixels[y * TextureSize + x] = new Color(value, value, value, 1f);
                }
            }

            _radial = Finish(pixels, TextureSize, "GlowRadial", TextureWrapMode.Clamp);
            return _radial;
        }

        private static Texture2D Blade()
        {
            if (_blade != null)
            {
                return _blade;
            }

            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                float along = (y + 0.5f) / TextureSize;
                float ends = Mathf.Clamp01(along / 0.08f) * Mathf.Clamp01((1f - along) / 0.2f);
                for (int x = 0; x < TextureSize; x++)
                {
                    float across = (x + 0.5f) / TextureSize;
                    float value = Mathf.Pow(1f - across, 1.6f) * ends;
                    pixels[y * TextureSize + x] = new Color(value, value, value, 1f);
                }
            }

            _blade = Finish(pixels, TextureSize, "GlowBlade", TextureWrapMode.Clamp);
            return _blade;
        }

        private static Texture2D Scanlines()
        {
            if (_scanlines != null)
            {
                return _scanlines;
            }

            var pixels = new Color32[ScanlineSize * ScanlineSize];
            for (int y = 0; y < ScanlineSize; y++)
            {
                float line = 0.35f + 0.65f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(y / (float)ScanlineSize * Mathf.PI * 2f * 6f), 3f);
                for (int x = 0; x < ScanlineSize; x++)
                {
                    float edge = 1f - Mathf.Pow(Mathf.Abs((x + 0.5f) / ScanlineSize * 2f - 1f), 4f);
                    float value = line * edge;
                    pixels[y * ScanlineSize + x] = new Color(value, value, value, 1f);
                }
            }

            _scanlines = Finish(pixels, ScanlineSize, "GlowScanlines", TextureWrapMode.Repeat);
            return _scanlines;
        }

        private static Texture2D Finish(Color32[] pixels, int size, string name, TextureWrapMode wrap)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = name,
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }
    }
}
