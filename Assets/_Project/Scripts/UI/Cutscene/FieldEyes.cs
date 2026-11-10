using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// Playtest note 5 (Remco 2026-11-08): once the eyes have opened under the field, pairs of small red eyes glow between
    /// the rocks along the road from the entrance to past the first gate, low against each rock's face on the road side,
    /// slanted like an insect's. They open one pair after another while the ground shivers, blink now and then, watch
    /// through the attack and shut at the last roar, when the brood goes back into the ground. Camera-facing glow cards,
    /// one shared material; posed from time alone, so any moment can be sought.
    /// </summary>
    public sealed class FieldEyes
    {
        private const int MaxPairs = 12;
        private const float LowShare = 0.5f;
        private const float RoadClearance = 0.6f;
        private const float MaxFromRoad = 9f;
        private const float BeyondGate = 6f;
        private const float MinSpacing = 2.4f;
        private const float MinRockHeight = 0.5f;
        private const float MinRockReach = 0.35f;
        private const float EyeSpacing = 0.15f;
        private const float EyeSize = 0.11f;
        private const float EyeStretch = 1.5f;
        private const float EyeSlantDegrees = 18f;
        private const float MinEyeHeight = 0.22f;
        private const float MaxEyeHeight = 0.6f;
        private const float FaceShare = 0.85f;
        private const float FaceOut = 0.06f;
        private const float OpenSeconds = 0.5f;
        private const float OpenStagger = 3.2f;
        private const float BlinkGap = 2.6f;
        private const float BlinkSeconds = 0.18f;
        private const float GlowPulse = 0.15f;
        private static readonly Color EyeColour = new Color(1f, 0.1f, 0.04f) * 5f;

        private readonly List<Transform[]> _pairs = new List<Transform[]>();
        private readonly List<float> _phases = new List<float>();
        private readonly Material _material;
        private readonly Transform _root;

        public FieldEyes(Transform parent, FieldLayout layout, IReadOnlyList<EnvironmentInstancer> decor, bool low)
        {
            _root = new GameObject("FieldEyes").transform;
            _root.SetParent(parent, false);
            _material = new Material(GlowDecal.RadialMaterial(EyeColour));
            int wanted = low ? Mathf.RoundToInt(MaxPairs * LowShare) : MaxPairs;
            foreach ((Vector3 left, Vector3 right) in Seats(layout, decor, wanted))
            {
                int index = _pairs.Count;
                _pairs.Add(new[] { Eye($"Eye{index}L", left), Eye($"Eye{index}R", right) });
                _phases.Add(CutsceneMath.Hash(index, 71));
            }

            _root.gameObject.SetActive(false);
        }

        /// <summary>How many pairs found a rock to watch from, for probes.</summary>
        public int Count => _pairs.Count;

        /// <summary>
        /// <paramref name="opening"/> seconds since the first pair began to open (negative: all shut), <paramref name="shut"/>
        /// 0..1 how far they have closed for good, <paramref name="time"/> the shot clock for the blinks.
        /// </summary>
        public void Pose(float opening, float shut, float time, Camera camera)
        {
            bool any = opening > 0f && shut < 1f && _pairs.Count > 0;
            _root.gameObject.SetActive(any);
            if (!any)
            {
                return;
            }

            Quaternion facing = camera != null ? camera.transform.rotation : Quaternion.identity;
            for (int i = 0; i < _pairs.Count; i++)
            {
                float open = CutsceneMath.Smooth(CutsceneMath.Span(opening, _phases[i] * OpenStagger, _phases[i] * OpenStagger + OpenSeconds)) * (1f - CutsceneMath.Smooth(shut));
                float blink = Mathf.Repeat(time + _phases[i] * BlinkGap, BlinkGap) < BlinkSeconds ? 0.1f : 1f;
                float lid = open * blink;
                float pulse = 1f + GlowPulse * Mathf.Sin((time + _phases[i]) * 3f);
                Transform[] pair = _pairs[i];
                for (int side = 0; side < pair.Length; side++)
                {
                    Transform eye = pair[side];
                    eye.gameObject.SetActive(lid > 0.01f);
                    float slant = side == 0 ? -EyeSlantDegrees : EyeSlantDegrees;
                    eye.rotation = facing * Quaternion.Euler(0f, 0f, slant);
                    eye.localScale = new Vector3(EyeSize * EyeStretch * pulse, Mathf.Max(0.001f, EyeSize * lid * pulse), 1f);
                }
            }
        }

        private Transform Eye(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }

        // The rocks beside the road between the entrance and a few metres past the first gate, the nearest to the road
        // first, kept apart from one another; on each the pair sits low on the face that looks at the road.
        private static List<(Vector3, Vector3)> Seats(FieldLayout layout, IReadOnlyList<EnvironmentInstancer> decor, int wanted)
        {
            float roadHalf = layout.Level.Road.Width * layout.Level.CellSize * 0.5f;
            float lastAlong = layout.GateCentreDistance + BeyondGate;
            var candidates = new List<(float Score, Vector3 Face, Vector3 Toward)>();
            foreach (Bounds rock in Rocks(decor))
            {
                float reach = Mathf.Max(rock.extents.x, rock.extents.z);
                float along = FieldLayout.DistanceAlong(layout.Road, rock.center);
                Vector3 onRoad = FieldLayout.PointAlong(layout.Road, along);
                Vector3 toward = Flat(onRoad - rock.center);
                float gap = toward.magnitude - reach - roadHalf;
                if (reach < MinRockReach || along > lastAlong || gap < RoadClearance || gap > MaxFromRoad || toward.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                toward.Normalize();
                Vector3 face = rock.center + toward * (reach * FaceShare + FaceOut);
                face.y = rock.min.y + Mathf.Clamp(rock.extents.y, MinEyeHeight, MaxEyeHeight);
                candidates.Add((gap, face, toward));
            }

            candidates.Sort((a, b) => a.Score.CompareTo(b.Score));
            var seats = new List<(Vector3, Vector3)>();
            var taken = new List<Vector3>();
            foreach (var (_, face, toward) in candidates)
            {
                if (seats.Count >= wanted)
                {
                    break;
                }

                bool crowded = false;
                foreach (Vector3 other in taken)
                {
                    crowded |= Flat(other - face).magnitude < MinSpacing;
                }

                if (crowded)
                {
                    continue;
                }

                Vector3 across = Vector3.Cross(Vector3.up, toward) * (EyeSpacing * 0.5f);
                seats.Add((face - across, face + across));
                taken.Add(face);
            }

            return seats;
        }

        // The big formations on the blocked cells and every other drawn piece tall and bulky enough to hide behind.
        private static List<Bounds> Rocks(IReadOnlyList<EnvironmentInstancer> decor)
        {
            var rocks = new List<Bounds>();
            foreach (EnvironmentInstancer instancer in decor)
            {
                if (instancer == null)
                {
                    continue;
                }

                foreach (DecorFormation formation in instancer.Formations)
                {
                    rocks.Add(new Bounds(formation.Centre, formation.Extents * 2f));
                }

                instancer.CollectTall(MinRockHeight, rocks);
            }

            return rocks;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
