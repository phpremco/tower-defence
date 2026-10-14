using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Boot
{
    /// <summary>
    /// Creates the global post-processing volume for a level from its theme and the platform quality profile:
    /// bloom (the glow), a vignette and tonemapping. The Low (Switch) profile turns bloom off.
    /// </summary>
    public static class PostProcessingBuilder
    {
        private const float BloomThreshold = 0.9f;
        private const float BloomScatter = 0.7f;
        private const float VignetteIntensity = 0.3f;

        public static void Build(Camera camera, VisualTheme theme, IPlatformSettings platform)
        {
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            if (platform.BloomEnabled)
            {
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(theme.BloomIntensity);
                bloom.threshold.Override(BloomThreshold);
                bloom.scatter.Override(BloomScatter);
            }

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(VignetteIntensity);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var go = new GameObject("PostProcessing");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }
    }
}
