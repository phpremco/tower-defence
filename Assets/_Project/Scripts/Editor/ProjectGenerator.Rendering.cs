using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
        private const string TiltShiftMaterialPath = "Assets/_Project/Art/Materials/TiltShift.mat";
        private const string TiltShiftFeatureName = "TiltShift";
        private const string TiltShiftShaderName = "Game/TiltShift";
        private const float TiltShiftBand = 0.22f;
        private const float TiltShiftFeather = 0.3f;
        private const float TiltShiftRadius = 3f;

        /// <summary>The tilt-shift pass on the PC renderer, for the Boot scene to switch by quality profile.</summary>
        private static FullScreenPassRendererFeature _tiltShift;

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

            _tiltShift = CreateTiltShift();
            AssetDatabase.SaveAssets();
        }

        // The depth of field is a screen-space blur in a full-screen pass, because the pipeline cannot read depth
        // from an orthographic camera. The feature lives as a sub-asset of the renderer, as the editor would add it.
        private static FullScreenPassRendererFeature CreateTiltShift()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TiltShiftMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find(TiltShiftShaderName));
                AssetDatabase.CreateAsset(material, TiltShiftMaterialPath);
            }

            material.SetFloat("_Band", TiltShiftBand);
            material.SetFloat("_Feather", TiltShiftFeather);
            material.SetFloat("_Radius", TiltShiftRadius);
            EditorUtility.SetDirty(material);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            if (renderer == null)
            {
                return null;
            }

            FullScreenPassRendererFeature feature = null;
            foreach (ScriptableRendererFeature existing in renderer.rendererFeatures)
            {
                if (existing is FullScreenPassRendererFeature pass && pass.name == TiltShiftFeatureName)
                {
                    feature = pass;
                }
            }

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = TiltShiftFeatureName;
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);
                AssetDatabase.SaveAssets();
                // The renderer keeps a parallel list of local ids; without the entry the editor drops the feature.
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
                var so = new SerializedObject(renderer);
                SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            feature.passMaterial = material;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.requirements = ScriptableRenderPassInput.None;
            feature.fetchColorBuffer = true;
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            return feature;
        }
    }
}
