using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The one material setup every planet in the game wears (the cutscene's planets and the world map's globe), so
    /// they read as the same world: the ground (Game/Planet), the cloud deck (Game/PlanetClouds) and the air past the
    /// limb (Game/PlanetAtmosphere), all filled from a <see cref="PlanetStyle"/>. The owner tells them where the sun
    /// is and how the clouds are turned, because the shaders light themselves and the ground draws the cloud shadows.
    /// </summary>
    public static class PlanetMaterials
    {
        public const string SurfaceShader = "Game/Planet";
        public const string CloudShader = "Game/PlanetClouds";
        public const string AtmosphereShader = "Game/PlanetAtmosphere";

        // The map's level markers sit on painted land, so its coasts may wander only a little from the paint.
        private const float MapCoastWarpShare = 0.3f;

        private static readonly int SunDirectionId = Shader.PropertyToID("_SunDirection");
        private static readonly int SunColorId = Shader.PropertyToID("_SunColor");
        private static readonly int WorldToCloudId = Shader.PropertyToID("_WorldToCloud");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int CloudShadowId = Shader.PropertyToID("_CloudShadow");

        /// <summary>The ground. <paramref name="forMap"/> applies the style's map overrides (more region colour, a lit night side).</summary>
        public static Material Surface(GlobePainter.Maps maps, Texture2D clouds, PlanetStyle style, bool forMap = false)
        {
            var material = Create(SurfaceShader, "PlanetSurface");
            material.SetTexture("_AlbedoMap", maps.Albedo);
            material.SetTexture("_GlossMap", maps.Gloss);
            material.SetTexture("_NormalMap", maps.Normal);
            material.SetTexture("_LightsMap", maps.Emission);
            material.SetTexture("_CloudMap", clouds != null ? clouds : Texture2D.blackTexture);
            material.SetFloat("_Alien", style.Alien ? 1f : 0f);
            material.SetColor("_DeepSea", style.DeepSea);
            material.SetColor("_ShallowSea", style.ShallowSea);
            material.SetColor("_LandA", style.LandA);
            material.SetColor("_LandB", style.LandB);
            material.SetColor("_Highland", style.Highland);
            material.SetColor("_Peak", style.Peak);
            material.SetColor("_CrystalColor", style.Crystal);
            material.SetColor("_CityColor", style.City);
            material.SetColor("_AtmoColor", style.Atmosphere);
            material.SetColor("_SunsetColor", style.Twilight);
            material.SetFloat("_TextureTint", forMap ? style.MapTextureTint : style.TextureTint);
            material.SetFloat("_CoastWarp", forMap ? style.CoastWarp * MapCoastWarpShare : style.CoastWarp);
            material.SetFloat("_CrystalDensity", style.CrystalDensity);
            material.SetFloat(CloudShadowId, clouds != null ? style.CloudShadow : 0f);
            material.SetFloat("_SpecStrength", forMap ? style.MapSeaGlint : style.SeaGlint);
            material.SetFloat("_SpecPower", style.SeaGlintTightness);
            material.SetFloat("_RimStrength", style.LimbHaze);
            material.SetFloat("_RimPower", style.LimbHazeFalloff);
            material.SetFloat("_Ambient", forMap ? style.MapAmbient : style.NightAmbient);
            material.SetFloat("_DayLights", forMap ? style.MapDayLights : 0f);
            material.SetFloat("_BumpStrength", style.Relief);
            material.SetFloat("_MapRelief", style.MapRelief);
            material.SetFloat("_CoastBlur", forMap ? style.MapCoastBlur : style.CoastBlur);
            material.SetFloat("_DayHaze", style.DayHaze);
            material.SetFloat("_Octaves", Octaves(style));
            material.SetMatrix(WorldToCloudId, Matrix4x4.identity);
            return material;
        }

        public static Material Clouds(Texture2D clouds, float alpha, PlanetStyle style)
        {
            var material = Create(CloudShader, "PlanetClouds");
            material.SetTexture("_CloudMap", clouds);
            material.SetColor("_CloudColor", style.Cloud);
            material.SetColor("_SunsetColor", style.Twilight);
            material.SetFloat(AlphaId, alpha);
            material.SetFloat("_Detail", style.CloudWisps);
            material.SetFloat("_Octaves", Mathf.Max(1, Octaves(style) - 1));
            return material;
        }

        /// <param name="lift">The shell's radius over the planet's, as the shell sphere is scaled.</param>
        public static Material Atmosphere(PlanetStyle style, float lift)
        {
            var material = Create(AtmosphereShader, "PlanetAtmosphere");
            material.SetColor("_AtmoColor", style.Atmosphere);
            material.SetColor("_SunsetColor", style.Twilight);
            material.SetFloat("_Strength", style.AtmosphereStrength);
            material.SetFloat("_Falloff", style.AtmosphereFalloff);
            material.SetFloat("_Lift", lift);
            material.SetFloat("_Forward", style.ForwardScatter);
            return material;
        }

        /// <summary>Where the sun is (the direction towards it, in world space) and its colour, for any planet material.</summary>
        public static void SetSun(Material material, Vector3 towardsSun, Color colour)
        {
            material.SetVector(SunDirectionId, towardsSun.normalized);
            material.SetColor(SunColorId, colour);
        }

        /// <summary>The sun of a directional light: light travels along its forward, so the sun lies the other way.</summary>
        public static void SetSun(Material material, Light light) => SetSun(material, -light.transform.forward, light.color * light.intensity);

        /// <summary>Lets the ground draw the shadows of a cloud deck turned by <paramref name="clouds"/>.</summary>
        public static void SetCloudFrame(Material surface, Transform clouds) => surface.SetMatrix(WorldToCloudId, clouds.worldToLocalMatrix);

        public static void SetCloudAlpha(Material clouds, float alpha) => clouds.SetFloat(AlphaId, alpha);

        public static void SetCloudShadow(Material surface, float strength) => surface.SetFloat(CloudShadowId, strength);

        private static int Octaves(PlanetStyle style) => ModelQuality.IsLow ? style.LowOctaves : style.HighOctaves;

        // The shaders are kept in the build by the generator's shader keepers; the stock lit material stands in should one be missing.
        private static Material Create(string shaderName, string name)
        {
            Shader shader = Shader.Find(shaderName);
            Material material = shader != null ? new Material(shader) : new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.name = name;
            return material;
        }
    }
}
