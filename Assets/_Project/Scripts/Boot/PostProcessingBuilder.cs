using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Boot
{
    /// <summary>
    /// Creates the global post-processing volume for a level from its theme and the platform quality profile:
    /// bloom (the glow), the theme's colour grading (contrast, saturation, tinted shadows and highlights), a
    /// vignette and tonemapping. The tilt-shift depth of field is a full-screen pass on the renderer (the pipeline
    /// cannot read depth from an orthographic camera); Boot switches it by profile. Low (Switch) scales the bloom down and uses the cheaper bloom filter.
    /// </summary>
    public static class PostProcessingBuilder
    {
        private const float BloomThreshold = 1f;
        private const float BloomScatter = 0.65f;
        private const float VignetteIntensity = 0.3f;
        private const float VignetteSmoothness = 0.45f;

        /// <summary>Prepares the camera and returns the new volume, at full weight unless told otherwise, under <paramref name="parent"/> when given.</summary>
        public static Volume Build(Camera camera, VisualTheme theme, IPlatformSettings platform, float weight = 1f, Transform parent = null)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
            data.dithering = true;
            camera.allowMSAA = true;
            camera.allowHDR = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            if (platform.BloomEnabled)
            {
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(theme.BloomIntensity * platform.BloomScale);
                bloom.threshold.Override(BloomThreshold);
                bloom.scatter.Override(BloomScatter);
                bloom.highQualityFiltering.Override(platform.HighQualityBloom);
            }

            var grading = profile.Add<ColorAdjustments>(true);
            grading.contrast.Override(theme.Contrast);
            grading.saturation.Override(theme.Saturation);

            var tints = profile.Add<ShadowsMidtonesHighlights>(true);
            tints.shadows.Override(new Vector4(theme.ShadowTint.r, theme.ShadowTint.g, theme.ShadowTint.b, 0f));
            tints.highlights.Override(new Vector4(theme.HighlightTint.r, theme.HighlightTint.g, theme.HighlightTint.b, 0f));

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(VignetteIntensity);
            vignette.smoothness.Override(VignetteSmoothness);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var go = new GameObject("PostProcessing");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = weight;
            volume.sharedProfile = profile;
            return volume;
        }

        /// <summary>
        /// A global volume that only lifts the post exposure (a cutscene shot's own exposure, over whatever theme volume
        /// blends in), and the setter for it in stops, so the interface code never touches the pipeline's types.
        /// </summary>
        public static System.Action<float> BuildExposure(Transform parent)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var exposure = profile.Add<ColorAdjustments>(true);
            exposure.postExposure.Override(0f);
            var go = new GameObject("ShotExposure");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = ExposurePriority;
            volume.sharedProfile = profile;
            return stops => exposure.postExposure.Override(stops);
        }

        private const float ExposurePriority = 10f;
    }
}
