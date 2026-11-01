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
    /// cannot read depth from an orthographic camera); Boot switches it by profile. Low (Switch) scales the bloom down.
    /// </summary>
    public static class PostProcessingBuilder
    {
        private const float BloomThreshold = 1f;
        private const float BloomScatter = 0.65f;
        private const float VignetteIntensity = 0.3f;
        private const float VignetteSmoothness = 0.45f;

        public static void Build(Camera camera, VisualTheme theme, IPlatformSettings platform)
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
                bloom.highQualityFiltering.Override(true);
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
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }
    }
}
