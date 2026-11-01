using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Editor
{
    /// <summary>
    /// The render pipeline settings the look depends on, set from code so a fresh checkout renders the same:
    /// anti-aliasing, one sharp shadow cascade sized to the fixed camera, HDR grading and a stronger ambient occlusion.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string PcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const int MsaaSamples = 4;
        // The camera sits 50 units from the focus; one cascade out to here covers the whole field at full resolution.
        private const float ShadowDistance = 90f;
        private const int ShadowmapResolution = 4096;
        private const float OcclusionIntensity = 0.6f;
        private const float OcclusionRadius = 0.45f;
        private const float OcclusionDirectStrength = 0.15f;

        private static void ConfigureRenderPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PcPipelinePath);
            if (pipeline == null)
            {
                Debug.LogWarning($"No pipeline asset at {PcPipelinePath}; render settings left as they are.");
                return;
            }

            pipeline.msaaSampleCount = MsaaSamples;
            pipeline.supportsHDR = true;
            pipeline.shadowCascadeCount = 1;
            pipeline.shadowDistance = ShadowDistance;
            pipeline.mainLightShadowmapResolution = ShadowmapResolution;
            pipeline.supportsCameraOpaqueTexture = true;
            pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
            pipeline.colorGradingLutSize = 32;
            EditorUtility.SetDirty(pipeline);

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(PcRendererPath))
            {
                if (asset == null || asset.name != "ScreenSpaceAmbientOcclusion")
                {
                    continue;
                }

                var so = new SerializedObject(asset);
                SerializedProperty settings = so.FindProperty("m_Settings");
                settings.FindPropertyRelative("Intensity").floatValue = OcclusionIntensity;
                settings.FindPropertyRelative("Radius").floatValue = OcclusionRadius;
                settings.FindPropertyRelative("DirectLightingStrength").floatValue = OcclusionDirectStrength;
                settings.FindPropertyRelative("Downsample").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
