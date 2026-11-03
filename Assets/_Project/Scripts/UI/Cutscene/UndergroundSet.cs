using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The dark under the field: a rough cavern of boulders and hanging spikes, lit only by crystal clusters that
    /// tremble with the drill above, dust sifting down, and the walls full of eyes. In the second shot one pair
    /// opens, then another, then hundreds in a wave rolling away into the dark. Posed from time alone.
    /// </summary>
    public sealed class UndergroundSet : ICutsceneSet
    {
        private const float Length = 34f;
        private const int Boulders = 46;
        private const int Spikes = 26;
        private const int Clusters = 5;
        private const float WallRadius = 5.2f;
        private const float FloorHeight = -1.6f;
        private const float CrystalLight = 2.2f;
        private const float CrystalRange = 6f;
        private const float TrembleDegrees = 4f;
        private const float TrembleShift = 0.035f;
        private const float DustRate = 14f;
        private const float FirstEyeShare = 0.12f;
        private const float SecondEyeShare = 0.28f;
        private const float WaveShare = 0.42f;
        private const float WaveSpeed = 9f;
        private const float LeadEyeSize = 1.6f;
        private const int DustParticles = 300;
        private static readonly Color DustColour = new Color(0.4f, 0.38f, 0.36f, 0.35f);

        private readonly List<Transform> _crystals = new List<Transform>();
        private readonly List<Quaternion> _crystalRest = new List<Quaternion>();
        private readonly List<Vector3> _crystalPositions = new List<Vector3>();
        private VisualTheme _theme;
        private EyeField _eyes;
        private ParticleSystem _dust;
        private CutsceneAction _action;
        private float _eyeShotSeconds;

        public VisualTheme Theme => _theme;

        public void Build(Transform root, CutsceneContext context)
        {
            VisualTheme own = context.Cast != null ? context.Cast.UndergroundTheme : null;
            _theme = own != null ? own : context.FieldLevel.Theme;
            Material[] materials = PropFactory.CreateMaterials(_theme);
            BuildCave(root, materials);
            BuildCrystals(root);
            BuildEyes(root, context.Scaled(context.Cast != null ? context.Cast.EyePairs : 120));
            _dust = ParticleKit.Create(root, "Dust", new Vector3(0f, 3.5f, Length * 0.4f), DustColour, 3f, 5f, 0.05f, 0.12f, 0.05f, 0.2f, 0.08f, context.Scaled(DustParticles), 6f);
            _eyeShotSeconds = context.Cutscene.CameraLengthOf(CutsceneAction.EyesOpen);
        }

        public void Enter(CutsceneAction action)
        {
            _action = action;
            // The cave brings no sun: its theme sets the dark fog and the faint ambient, the crystals do the rest.
            ThemeLighting.ApplyAtmosphere(_theme);
        }

        public void Exit()
        {
            ParticleKit.SetRate(_dust, 0f);
        }

        public bool TryGetAnchor(CameraAnchor anchor, out Vector3 origin, out Quaternion facing)
        {
            origin = Vector3.zero;
            facing = Quaternion.identity;
            return false;
        }

        public void Tick(float time, float progress)
        {
            bool trembling = _action == CutsceneAction.CrystalsTremble;
            float tremble = trembling ? CutsceneMath.Span(progress, 0.1f, 0.4f) : 0.35f;
            for (int i = 0; i < _crystals.Count; i++)
            {
                float a = Mathf.Sin(time * 31f + i * 1.3f) * TrembleDegrees * tremble;
                float b = Mathf.Sin(time * 23f + i * 2.9f) * TrembleDegrees * tremble;
                _crystals[i].rotation = _crystalRest[i] * Quaternion.Euler(a, 0f, b);
                _crystals[i].position = _crystalPositions[i] + Vector3.up * (Mathf.Sin(time * 41f + i) * TrembleShift * tremble);
            }

            ParticleKit.SetRate(_dust, DustRate * (0.3f + tremble));
            // The eyes only open in their own shot; before that every slit stays shut.
            _eyes?.Pose(_action == CutsceneAction.EyesOpen ? time : -1f);
        }

        private void BuildCave(Transform root, Material[] materials)
        {
            for (int i = 0; i < Boulders; i++)
            {
                float z = CutsceneMath.Hash(i, 91) * Length;
                float angle = Mathf.Lerp(-0.25f, 1.25f, CutsceneMath.Hash(i, 93)) * Mathf.PI;
                float radius = WallRadius + CutsceneMath.Hash(i, 95) * 1.6f;
                var position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Max(FloorHeight, Mathf.Sin(angle) * radius * 0.7f), z);
                Prop(root, PropKind.Boulder, i, position, 2.2f + CutsceneMath.Hash(i, 97) * 2.2f, materials);
            }

            for (int i = 0; i < 18; i++)
            {
                var position = new Vector3((CutsceneMath.Hash(i, 101) - 0.5f) * 8f, FloorHeight - 0.6f, CutsceneMath.Hash(i, 103) * Length);
                Prop(root, PropKind.Rock, i, position, 1.6f + CutsceneMath.Hash(i, 105) * 1.4f, materials);
            }

            for (int i = 0; i < Spikes; i++)
            {
                var position = new Vector3((CutsceneMath.Hash(i, 107) - 0.5f) * 7f, 4.2f + CutsceneMath.Hash(i, 109), CutsceneMath.Hash(i, 111) * Length);
                Transform spike = Prop(root, PropKind.Spike, i, position, 0.8f + CutsceneMath.Hash(i, 113) * 0.9f, materials);
                spike.rotation = Quaternion.Euler(180f, CutsceneMath.Hash(i, 115) * 360f, 0f);
            }
        }

        private void BuildCrystals(Transform root)
        {
            for (int i = 0; i < Clusters; i++)
            {
                float z = 7f + i * (Length - 9f) / Clusters + CutsceneMath.Hash(i, 121) * 2f;
                float side = i % 2 == 0 ? -1f : 1f;
                var position = new Vector3(side * (3.6f + CutsceneMath.Hash(i, 123) * 0.8f), FloorHeight + 0.2f, z);
                Material[] materials = PropFactory.CreateMaterials(_theme, i % PropFactory.ColourVariants);
                Transform crystal = Prop(root, PropKind.Crystal, i, position, 1.2f + CutsceneMath.Hash(i, 125) * 0.8f, materials);
                _crystals.Add(crystal);
                _crystalRest.Add(crystal.rotation);
                _crystalPositions.Add(position);

                var glow = new GameObject("Light");
                glow.transform.SetParent(root, false);
                glow.transform.position = position + Vector3.up * 1.2f;
                var light = glow.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = _theme.AccentColor;
                light.intensity = CrystalLight;
                light.range = CrystalRange;
                light.shadows = LightShadows.None;
                GlowDecal.PlacePool(crystal, _theme.AccentColor * 0.25f, 1.6f);
            }
        }

        // The first two pairs sit close to the camera's path; the rest cover the walls and open in a wave from them.
        private void BuildEyes(Transform root, int pairs)
        {
            var centres = new List<Vector3>(pairs);
            var opens = new List<float>(pairs);
            var sizes = new List<float>(pairs);
            float length = Mathf.Max(1f, _eyeShotSeconds > 0f ? _eyeShotSeconds : 9f);
            Vector3 first = new Vector3(1.6f, 0.4f, 9f);
            centres.Add(first);
            opens.Add(length * FirstEyeShare);
            sizes.Add(LeadEyeSize);
            centres.Add(new Vector3(-2.2f, 1.3f, 12f));
            opens.Add(length * SecondEyeShare);
            sizes.Add(LeadEyeSize * 0.8f);
            for (int i = 2; i < pairs; i++)
            {
                float angle = Mathf.Lerp(-0.2f, 1.2f, CutsceneMath.Hash(i, 131)) * Mathf.PI;
                float radius = WallRadius - 0.6f + CutsceneMath.Hash(i, 133) * 1.2f;
                var position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Max(FloorHeight + 0.3f, Mathf.Sin(angle) * radius * 0.75f), 4f + CutsceneMath.Hash(i, 135) * (Length - 4f));
                centres.Add(position);
                opens.Add(length * WaveShare + Vector3.Distance(position, first) / WaveSpeed + CutsceneMath.Hash(i, 137) * 0.4f);
                sizes.Add(0.8f + CutsceneMath.Hash(i, 81) * 0.9f);
            }

            _eyes = new EyeField(root, centres, opens, sizes, new Vector3(0f, 0.6f, 0f));
        }

        private static Transform Prop(Transform root, PropKind kind, int variant, Vector3 position, float scale, Material[] materials)
        {
            var go = new GameObject($"{kind}{variant}");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, CutsceneMath.Hash(variant, (int)kind + 141) * 360f, 0f));
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = PropFactory.Create(kind, variant);
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }
    }
}
