using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// What lies at infinity behind a space set: the galaxy band and nebula on a sky sphere, the sun as a hot core
    /// with a soft halo and a thin lens streak (bloom does the rest), and a cool rim light opposite the sun so hard
    /// surfaces keep an edge on their dark side. Sky and sun follow the camera, so they never come nearer.
    /// </summary>
    public sealed class SpaceBackdrop
    {
        public const string SkyShader = "Game/SpaceSky";

        private const float SkyRadius = 480f;
        private const float SunDistance = 420f;
        private const float StreakThickness = 0.035f;

        private readonly Transform _sky;
        private readonly Transform _sun;
        private readonly Transform _halo;
        private readonly Transform _streak;
        private readonly Vector3 _towardsSun;
        private readonly SpaceStyle _style;

        /// <param name="sunTravel">The direction the set's sunlight travels (the sun lies the other way).</param>
        public SpaceBackdrop(Transform root, SpaceStyle style, Vector3 sunTravel, bool low)
        {
            _style = style;
            _towardsSun = -sunTravel.normalized;
            _sky = BuildSky(root, style, low);
            _halo = SpaceKit.Glow(root, "SunHalo", style.SunHalo);
            _sun = SpaceKit.Glow(root, "SunCore", style.SunCore);
            _streak = SpaceKit.Glow(root, "SunStreak", style.SunHalo * 0.6f);
            BuildRimLight(root, sunTravel, style);
        }

        /// <summary>Keeps the sky round the camera and turns the sun's glows to face it.</summary>
        public void Pose(Transform view)
        {
            Vector3 eye = view.position;
            if (_sky != null)
            {
                _sky.position = eye;
            }

            Vector3 sun = eye + _towardsSun * SunDistance;
            Quaternion facing = view.rotation;
            _sun.SetPositionAndRotation(sun, facing);
            _sun.localScale = Vector3.one * _style.SunSize;
            _halo.SetPositionAndRotation(sun, facing);
            _halo.localScale = Vector3.one * _style.HaloSize;
            _streak.SetPositionAndRotation(sun, facing);
            _streak.localScale = new Vector3(_style.StreakLength, _style.StreakLength * StreakThickness, 1f);
        }

        private static Transform BuildSky(Transform root, SpaceStyle style, bool low)
        {
            Shader shader = Shader.Find(SkyShader);
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader) { name = "SpaceSky" };
            material.SetColor("_NebulaA", style.NebulaA);
            material.SetColor("_NebulaB", style.NebulaB);
            material.SetColor("_BandColor", style.Band);
            material.SetVector("_BandNormal", style.BandNormal.normalized);
            material.SetFloat("_BandWidth", style.BandWidth);
            material.SetFloat("_Strength", style.SkyStrength);
            material.SetFloat("_Haze", style.Haze);
            material.SetFloat("_StarScale", style.StarScale);
            material.SetFloat("_StarBrightness", style.StarBrightness);
            material.SetFloat("_BandStars", style.BandStars);
            material.SetFloat("_Octaves", low ? 3f : 6f);

            var go = new GameObject("Sky");
            go.transform.SetParent(root, false);
            go.transform.localScale = Vector3.one * SkyRadius;
            go.AddComponent<MeshFilter>().sharedMesh = PlanetGlobe.SphereMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        // From the side away from the sun and a little above, so a ship's shadowed flank still shows its shape.
        private static void BuildRimLight(Transform root, Vector3 sunTravel, SpaceStyle style)
        {
            Vector3 rim = Vector3.Reflect(sunTravel.normalized, Vector3.up);
            rim = new Vector3(-rim.x, rim.y - 0.4f, -rim.z);
            var go = new GameObject("RimLight");
            go.transform.SetParent(root, false);
            go.transform.rotation = Quaternion.LookRotation(rim.normalized);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = style.RimColour;
            light.intensity = style.RimIntensity;
            light.shadows = LightShadows.None;
        }
    }
}
