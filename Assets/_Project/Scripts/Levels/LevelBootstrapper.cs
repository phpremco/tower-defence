using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Builds the generic level scene from the LevelDefinition in the LevelSession: camera, light and ground.
    /// Towers, enemies and paths are added by later milestones.
    /// </summary>
    public sealed class LevelBootstrapper : MonoBehaviour
    {
        private const float GroundThickness = 0.2f;
        private const string BaseColorProperty = "_BaseColor";
        private static readonly Vector3 SunRotation = new Vector3(50f, -30f, 0f);

        [SerializeField] private FixedCameraRig _cameraRig;

        private void Start()
        {
            if (!Services.TryGet(out LevelSession session))
            {
                Debug.LogError("No LevelSession registered. Start the game from the Boot scene.", this);
                return;
            }

            LevelDefinition level = session.Level;
            if (!level.IsValid(out string error))
            {
                Debug.LogError($"Level '{level.name}' is invalid: {error}", level);
                return;
            }

            _cameraRig.Apply(level);
            BuildLight(level.Theme);
            BuildGround(level);
            ApplyFog(level.Theme);
        }

        private static void BuildLight(VisualTheme theme)
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = theme.LightColor;
            light.intensity = theme.LightIntensity;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(SunRotation);
        }

        private static void BuildGround(LevelDefinition level)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            Vector2 size = level.WorldSize;
            ground.transform.localScale = new Vector3(size.x, GroundThickness, size.y);
            ground.transform.position = new Vector3(0f, -GroundThickness * 0.5f, 0f);

            // The pipeline default material is always included in builds, unlike a shader found by name.
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetColor(BaseColorProperty, level.Theme.GroundColor);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void ApplyFog(VisualTheme theme)
        {
            RenderSettings.fog = theme.FogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = theme.FogColor;
            RenderSettings.fogDensity = theme.FogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = theme.FogColor;
        }
    }
}

