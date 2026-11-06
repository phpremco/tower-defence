using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Lights a scene from a visual theme: the sun with soft shadows, a low back light that only touches the edges of
    /// shapes, the fog, a three-way ambient and the sky reflection. Shared by the level and the cutscene, so a
    /// cutscene on a level's field looks exactly like playing it.
    /// </summary>
    public static class ThemeLighting
    {
        // The back light comes in low from the far side of the field, so it only touches the edges of shapes.
        private const float RimPitchDegrees = 28f;
        private const float RimYawOffsetDegrees = 180f;
        // Ambient comes from three directions: the sky colour from above, fog at the horizon and near black below,
        // so the underside of every shape stays darker than its top even in shadow.
        private const float AmbientSkyBrightness = 1f;
        private const float AmbientEquatorShare = 0.5f;
        private const float AmbientGroundBrightness = 0.5f;

        /// <summary>The two directional lights a theme creates, for a cutscene that sweeps the sun.</summary>
        public readonly struct Lights
        {
            public Lights(Light sun, Light rim)
            {
                Sun = sun;
                Rim = rim;
            }

            public Light Sun { get; }
            public Light Rim { get; }
        }

        /// <summary>Creates the sun and the rim light (under <paramref name="parent"/> when given) and sets fog, ambient and reflections.</summary>
        public static Lights Apply(VisualTheme theme, Transform parent = null)
        {
            var sun = new GameObject("Sun");
            sun.transform.SetParent(parent, false);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = theme.LightColor;
            light.intensity = theme.LightIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1f;
            sun.transform.rotation = Quaternion.Euler(theme.SunPitch, theme.SunYaw, 0f);

            var rimObject = new GameObject("RimLight");
            rimObject.transform.SetParent(parent, false);
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = theme.RimColor;
            rim.intensity = theme.RimIntensity;
            rim.shadows = LightShadows.None;
            rimObject.transform.rotation = Quaternion.Euler(RimPitchDegrees, theme.SunYaw + RimYawOffsetDegrees, 0f);

            IsolateFromPreviews(light, rim);
            ApplyAtmosphere(theme);
            return new Lights(light, rim);
        }

        // Lighting trial (PlaytestSettings): the theme's sun is the scene's sun, so a display stage's brighter key light can
        // never become the main light, and neither field light reaches the display stages' layer.
        private static void IsolateFromPreviews(Light sun, Light rim)
        {
            int layer = Game.Core.PlaytestSettings.IsolatedPreviewLayer();
            if (layer < 0)
            {
                return;
            }

            RenderSettings.sun = sun;
            sun.cullingMask = ~(1 << layer);
            rim.cullingMask = ~(1 << layer);
        }

        /// <summary>Fog, ambient and reflections only, for a set that brings its own lights.</summary>
        public static void ApplyAtmosphere(VisualTheme theme)
        {
            RenderSettings.fog = theme.FogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = theme.FogColor;
            RenderSettings.fogDensity = theme.FogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.SkyColor * AmbientSkyBrightness;
            RenderSettings.ambientEquatorColor = Color.Lerp(theme.FogColor, theme.LightColor, AmbientEquatorShare) * AmbientSkyBrightness;
            RenderSettings.ambientGroundColor = theme.FogColor * AmbientGroundBrightness;
            SkyProbe.Apply(theme);
        }
    }
}
