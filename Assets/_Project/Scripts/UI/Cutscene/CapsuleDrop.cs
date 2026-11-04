using System;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Landing capsules leaving the mothership's bay one after another and falling to the planet: a small push
    /// clear of the hull, then a long curve down that speeds up into the atmosphere, where the heat shield glows,
    /// a white-hot head flares and a burning trail stretches out behind, so each capsule ends as a falling star.
    /// The drop repeats for as long as the shot holds. Every pose follows from time alone; the trail is the path
    /// itself sampled back in time, never a history of frames.
    /// </summary>
    public sealed class CapsuleDrop
    {
        private const float FirstRelease = 1.2f;
        private const float ReleaseGap = 1.15f;
        private const float FallSeconds = 6.5f;
        private const float CycleRest = 2.5f;
        private const float CapsuleScale = 0.6f;
        private const float AtmosphereDepth = 0.35f;
        private const int TrailPoints = 22;
        private const float TrailStep = 0.07f;
        private const float TrailWidth = 0.7f;
        private const float HeadSize = 3f;
        private const float ShieldEmission = 5f;
        private const float TrailIntensity = 3f;
        private const float ColdHead = 0.25f;
        private static readonly Color HotColour = new Color(1f, 0.5f, 0.15f);
        private static readonly Color CoreColour = new Color(1f, 0.85f, 0.6f) * 3f;

        private readonly List<Capsule> _capsules = new List<Capsule>();
        private readonly Vector3[] _trail = new Vector3[TrailPoints];
        private readonly Func<float, Vector3> _bayAt;
        private readonly Vector3 _planetCentre;
        private readonly float _planetRadius;
        private readonly Transform _view;
        private readonly float _cycle;

        /// <param name="bayAt">Where the bay opening is at a moment of the shot (the ship moves).</param>
        public CapsuleDrop(Transform parent, CutsceneContext context, Color glow, Func<float, Vector3> bayAt, Vector3 planetCentre, float planetRadius)
        {
            _bayAt = bayAt;
            _planetCentre = planetCentre;
            _planetRadius = planetRadius;
            _view = context.Camera.transform;
            int count = context.Scaled(context.Cast != null ? context.Cast.CapsuleCount : 6);
            _cycle = FirstRelease + count * ReleaseGap + FallSeconds + CycleRest;

            GameObject custom = context.Cast != null ? context.Cast.Capsule : null;
            ModelArtDefinition art = context.Cast != null ? context.Cast.CapsuleArt : null;
            CapsuleModelFactory.Parts parts = CapsuleModelFactory.Create();
            Material[] materials = ModelPalette.Create(glow);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0f), new GradientColorKey(HotColour, 0.25f), new GradientColorKey(new Color(0.8f, 0.2f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0f, 1f) });
            for (int i = 0; i < count; i++)
            {
                _capsules.Add(BuildCapsule(parent, i, art, custom, parts, materials, gradient));
            }
        }

        public void Pose(float time)
        {
            for (int i = 0; i < _capsules.Count; i++)
            {
                Capsule capsule = _capsules[i];
                float release = FirstRelease + i * ReleaseGap + CutsceneMath.Hash(i, 61) * 0.4f;
                float age = time < release ? -1f : Mathf.Repeat(time - release, _cycle);
                bool falling = age >= 0f && age < FallSeconds;
                capsule.Root.gameObject.SetActive(falling);
                capsule.Trail.enabled = falling;
                capsule.Head.gameObject.SetActive(falling);
                if (!falling)
                {
                    continue;
                }

                float launched = time - age;
                Vector3 position = PathPoint(i, launched, age);
                Vector3 ahead = PathPoint(i, launched, age + 0.05f);
                float heat = Heat(position);
                capsule.Root.SetPositionAndRotation(position, Quaternion.LookRotation(ahead - position) * Quaternion.Euler(-90f, 0f, 0f));
                capsule.Shield.SetColor("_EmissionColor", HotColour * (ShieldEmission * heat * heat));
                capsule.Head.SetPositionAndRotation(position, _view.rotation);
                capsule.Head.localScale = Vector3.one * (HeadSize * heat + ColdHead);
                for (int p = 0; p < TrailPoints; p++)
                {
                    _trail[p] = PathPoint(i, launched, Mathf.Max(0f, age - p * TrailStep));
                }

                capsule.Trail.SetPositions(_trail);
                capsule.Trail.widthMultiplier = TrailWidth * Mathf.Max(0.1f, heat);
            }
        }

        // Out of the bay with a small push down and ahead, then a falling curve that keeps speeding up into the air.
        private Vector3 PathPoint(int index, float launched, float age)
        {
            Vector3 bay = _bayAt(launched);
            float u = Mathf.Clamp01(age / FallSeconds);
            float eased = u * u * (1.6f - 0.6f * u);
            float lane = (CutsceneMath.Hash(index, 63) - 0.5f) * 0.9f;
            Vector3 aim = new Vector3(lane * _planetRadius * 0.7f, 0f, _planetRadius * (0.25f + CutsceneMath.Hash(index, 65) * 0.2f));
            Vector3 target = _planetCentre + (aim + Vector3.up * _planetRadius).normalized * (_planetRadius * 0.995f);
            Vector3 push = bay + new Vector3(lane * 2f, -2.5f, 2f);
            Vector3 swing = target + new Vector3(0f, _planetRadius * 0.25f, -_planetRadius * 0.2f);
            float a = 1f - eased;
            return a * a * a * bay + 3f * a * a * eased * push + 3f * a * eased * eased * swing + eased * eased * eased * target;
        }

        private float Heat(Vector3 position)
        {
            float altitude = Vector3.Distance(position, _planetCentre) - _planetRadius;
            return CutsceneMath.Smooth(1f - Mathf.Clamp01(altitude / (_planetRadius * AtmosphereDepth)));
        }

        private Capsule BuildCapsule(Transform parent, int index, ModelArtDefinition art, GameObject custom, CapsuleModelFactory.Parts parts, Material[] materials, Gradient gradient)
        {
            var root = new GameObject($"Capsule{index}").transform;
            root.SetParent(parent, false);
            var model = new GameObject("Model").transform;
            model.SetParent(root, false);
            model.localScale = Vector3.one * CapsuleScale;
            model.localPosition = Vector3.down * (CapsuleScale * 0.9f);
            Material shield;
            if (art != null)
            {
                // The imported capsule with its legs folded (the first frame of its landing); its heat shield wears its own glow.
                ArtModel capsule = ArtModel.CreateSampled(model, art, ownMaterial: false);
                capsule.Sample(ModelClipRole.Land, 0f, loop: false);
                shield = CapsuleArt.OwnShield(capsule);
            }
            else
            {
                if (custom != null)
                {
                    UnityEngine.Object.Instantiate(custom, model);
                }
                else
                {
                    AddMesh("Hull", model, parts.Hull, materials);
                }

                shield = SurfaceMaterials.Create(new Color(0.12f, 0.1f, 0.09f), 1f, 0.3f);
                AddMesh("Shield", model, parts.Shield, new[] { shield });
            }

            LineRenderer trail = SpaceKit.Line(parent, $"Trail{index}", TrailPoints, false, TrailIntensity);
            trail.colorGradient = gradient;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            Transform head = SpaceKit.Glow(parent, $"Head{index}", CoreColour);
            return new Capsule { Root = root, Shield = shield, Trail = trail, Head = head };
        }

        private static void AddMesh(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        private sealed class Capsule
        {
            public Transform Root;
            public Material Shield;
            public LineRenderer Trail;
            public Transform Head;
        }
    }
}
