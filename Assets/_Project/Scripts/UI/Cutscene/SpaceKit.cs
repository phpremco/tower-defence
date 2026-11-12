using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// What every set in space shares (the sky and stars live in <see cref="SpaceBackdrop"/>): one hard sun with no fill (so a planet
    /// has a real night side) and the dark ambient of space. The sun lives under the set's root, so it goes out with
    /// the set.
    /// </summary>
    public static class SpaceKit
    {
        private const float AmbientShare = 0.08f;
        private const float SpaceReflection = 0.12f;
        private const float SunBoost = 3f;
        private const int DotSize = 64;

        private static Texture2D _softDot;
        private static Texture2D _softBand;

        /// <summary>
        /// The set's sun in the theme's light colour: a directional light along <paramref name="direction"/> (where
        /// the light travels), much brighter than on a field, because space has no ambient and painted planets are dark.
        /// </summary>
        public static Light Sun(Transform root, Vector3 direction, VisualTheme theme)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(root, false);
            go.transform.rotation = Quaternion.LookRotation(direction.normalized);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = theme.LightColor;
            light.intensity = theme.LightIntensity * SunBoost;
            // No shadows: nothing in space is close enough to shade anything else, and the far star sphere must not.
            light.shadows = LightShadows.None;
            return light;
        }

        /// <summary>No fog, almost no ambient and faint reflections: the theme's sky colour only lifts the night side a touch.</summary>
        public static void ApplyDarkness(VisualTheme theme)
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Color sky = theme != null ? theme.SkyColor : Color.grey;
            RenderSettings.ambientSkyColor = sky * AmbientShare;
            RenderSettings.ambientEquatorColor = sky * (AmbientShare * 0.6f);
            RenderSettings.ambientGroundColor = Color.black;
            if (theme != null)
            {
                SkyProbe.Apply(theme);
            }

            // Space mirrors almost nothing: at full strength a glossy sea seen at a grazing angle turns into a sheet of sky.
            RenderSettings.reflectionIntensity = SpaceReflection;
        }

        /// <summary>
        /// A soft additive glow on a quad, unlit, in an HDR colour so the bloom picks it up; the owner turns it to
        /// the camera and scales it. It wears the Blender <paramref name="sprite"/> of the cutscene's FX set, or the
        /// painted soft dot without one.
        /// </summary>
        public static Transform Glow(Transform parent, string name, Color colour, CutsceneSprite sprite = CutsceneSprite.GlowSoft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Additive(Sprite(sprite, SoftDot()), colour);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        /// <summary>The Blender sprite of the active cutscene FX set, or the painted fallback.</summary>
        public static Texture2D Sprite(CutsceneSprite sprite, Texture2D fallback)
        {
            Texture2D painted = CutsceneFxSet.Get(sprite);
            return painted != null ? painted : fallback;
        }

        /// <summary>The painted soft dot, for callers that need the fallback of a round sprite.</summary>
        public static Texture2D SoftDotTexture => SoftDot();

        // Built from the Blender particle atlas's additive material: the URP default particle material is null in a player build.
        private static Material Additive(Texture2D texture, Color colour) => ParticleKit.AdditiveSprite(texture, colour);

        // Bright in the middle and gone at the edge, round for glows.
        private static Texture2D SoftDot()
        {
            if (_softDot == null)
            {
                _softDot = Soft("SoftDot", (u, v) => Mathf.Sqrt(u * u + v * v));
            }

            return _softDot;
        }

        // The same falloff across a line only, so a line is a soft beam along its whole length.
        private static Texture2D SoftBand()
        {
            if (_softBand == null)
            {
                _softBand = Soft("SoftBand", (u, v) => Mathf.Abs(v));
            }

            return _softBand;
        }

        private static Texture2D Soft(string name, System.Func<float, float, float> distance)
        {
            var pixels = new Color32[DotSize * DotSize];
            for (int y = 0; y < DotSize; y++)
            {
                for (int x = 0; x < DotSize; x++)
                {
                    float u = (x + 0.5f) / DotSize * 2f - 1f;
                    float v = (y + 0.5f) / DotSize * 2f - 1f;
                    float value = Mathf.Pow(Mathf.Clamp01(1f - distance(u, v)), 1.8f);
                    pixels[y * DotSize + x] = new Color(value, value, value, value);
                }
            }

            var texture = new Texture2D(DotSize, DotSize, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// An additive soft beam; <paramref name="intensity"/> lifts its vertex colours above one so the bloom catches
        /// it. Width and colours are set by the caller.
        /// </summary>
        public static LineRenderer Line(Transform parent, string name, int points, bool loop, float intensity = 1f, CutsceneSprite sprite = CutsceneSprite.BeamSoft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points;
            line.sharedMaterial = Additive(Sprite(sprite, SoftBand()), Color.white * intensity);
            // The beam sprite tiles along the line; a trail sprite is stretched once from head to tail.
            line.textureMode = sprite == CutsceneSprite.BeamSoft ? LineTextureMode.Tile : LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            return line;
        }
    }
}
