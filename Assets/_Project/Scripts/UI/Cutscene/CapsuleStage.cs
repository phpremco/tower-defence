using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// One capsule's deorbit booster (the Blender <c>Cutscene_Booster</c>): carried on the capsule's apex out of the bay,
    /// lit a moment after the push clears it, burning with a hot plume and blinking strobes, then blown off at the
    /// separation: its clamps fly out tumbling (the model's Deploy clip), it drifts back and away end over end while its
    /// flame sputters out. The owner hands it the capsule's frame at the moment and at the separation, so every pose
    /// follows from the shot clock alone.
    /// </summary>
    public sealed class CapsuleStage
    {
        // Where the booster sits on the capsule (the capsule model's shoulder, in its own units) and the bell's exit above it.
        public const float MountHeight = 1.4f;
        private const float NozzleHeight = 2.08f;
        private const float NozzleRadius = 0.36f;
        private const float PlumeLength = 5.5f;
        private const float SputterSeconds = 0.35f;
        private const float RampSeconds = 0.12f;
        private const float BackDrift = 0.9f;
        private const float KeepShare = 0.55f;
        private const float TumbleDegrees = 95f;
        private const float StrobeSize = 0.55f;
        private const float StrobePeriod = 0.7f;
        private const float StrobeLit = 0.12f;
        private const string Nozzle = "Nozzle_1";
        private const string LightSocket = "Light";
        private static readonly Color PlumeCore = new Color(0.95f, 0.97f, 1f) * 7f;
        private static readonly Color PlumeEdge = new Color(0.35f, 0.62f, 1f) * 2.4f;
        private static readonly Color StrobeColour = new Color(1f, 0.95f, 0.85f) * 5f;

        private readonly Transform _root;
        private readonly ArtModel _art;
        private readonly Transform _nozzle;
        private readonly Transform[] _strobes;
        private readonly Vector3[] _strobeLocal;
        private readonly ExhaustPlume _plume;
        private readonly float _scale;
        private readonly int _index;

        public CapsuleStage(Transform parent, int index, ModelArtDefinition art, float scale, bool shimmer)
        {
            _index = index;
            _scale = scale;
            _root = new GameObject($"Booster{index}").transform;
            _root.SetParent(parent, false);
            _root.localScale = Vector3.one * scale;
            if (art != null)
            {
                _art = ArtModel.CreateSampled(_root, art, ownMaterial: false);
                _art.Sample(ModelClipRole.Deploy, 0f, loop: false);
                _nozzle = _art.Find(Nozzle);
                Transform[] sockets = _art.Numbered(LightSocket);
                _strobes = new Transform[sockets.Length];
                _strobeLocal = new Vector3[sockets.Length];
                for (int i = 0; i < sockets.Length; i++)
                {
                    _strobeLocal[i] = _root.InverseTransformPoint(sockets[i].position);
                    _strobes[i] = SpaceKit.Glow(parent, $"Booster{index}Strobe{i + 1}", StrobeColour, CutsceneSprite.RunningLight);
                }
            }
            else
            {
                _strobes = new Transform[0];
                _strobeLocal = new Vector3[0];
            }

            _plume = new ExhaustPlume(parent, $"Booster{index}", PlumeCore, PlumeEdge, 1.6f, shimmer);
            Show(false);
        }

        /// <summary>Hides the booster and its glows (the capsule is not in flight).</summary>
        public void Hide()
        {
            Show(false);
            _plume.Pose(Vector3.zero, Vector3.zero, 0f, 0f, 0f, 0f, null);
        }

        /// <summary>
        /// Poses the booster: <paramref name="mount"/> is the capsule's mount (position and facing, up away from the
        /// capsule) now, <paramref name="separationMount"/> and <paramref name="separationVelocity"/> at the separation,
        /// <paramref name="sinceIgnition"/> and <paramref name="sinceSeparation"/> the seconds since each (negative before).
        /// </summary>
        public void Pose(Pose mount, Pose separationMount, Vector3 separationVelocity, float sinceIgnition, float sinceSeparation, float clock, Transform view)
        {
            Show(true);
            Pose pose = mount;
            if (sinceSeparation >= 0f)
            {
                // Kept on much of the capsule's speed, pushed back off its apex and turning end over end.
                Vector3 up = separationMount.rotation * Vector3.up;
                Vector3 axis = separationMount.rotation * (CutsceneMath.Hash(_index, 71) < 0.5f ? Vector3.right : Vector3.forward);
                float t = sinceSeparation;
                Vector3 position = separationMount.position + separationVelocity * (KeepShare * t) + up * (BackDrift * t);
                pose = new Pose(position, Quaternion.AngleAxis(TumbleDegrees * t, axis) * separationMount.rotation);
                if (_art != null)
                {
                    _art.Sample(ModelClipRole.Deploy, Mathf.Min(t, _art.LengthOf(ModelClipRole.Deploy)), loop: false);
                }
            }
            else if (_art != null)
            {
                _art.Sample(ModelClipRole.Deploy, 0f, loop: false);
            }

            _root.SetPositionAndRotation(pose.position, pose.rotation);
            Vector3 along = pose.rotation * Vector3.up;
            Vector3 mouth = _nozzle != null ? _nozzle.position : pose.position + along * (NozzleHeight * _scale);
            _plume.Pose(mouth, along, NozzleRadius * _scale, PlumeLength, Throttle(sinceIgnition, sinceSeparation, clock), clock, view);
            PoseStrobes(clock, view);
        }

        /// <summary>Where the bell's mouth is (for the smoke the owner trails behind it), along the burn.</summary>
        public static Vector3 MouthOf(Pose mount, float scale) => mount.position + mount.rotation * Vector3.up * (NozzleHeight * scale);

        // Full from a short ramp after ignition until the separation, then a sputter that dies out.
        private float Throttle(float sinceIgnition, float sinceSeparation, float clock)
        {
            if (sinceIgnition < 0f)
            {
                return 0f;
            }

            float ramp = Mathf.Clamp01(sinceIgnition / RampSeconds);
            if (sinceSeparation < 0f)
            {
                return ramp;
            }

            float left = 1f - sinceSeparation / SputterSeconds;
            if (left <= 0f)
            {
                return 0f;
            }

            float cough = 0.5f + 0.5f * Mathf.Sin(clock * 70f + _index);
            return left * left * cough;
        }

        private void PoseStrobes(float clock, Transform view)
        {
            for (int i = 0; i < _strobes.Length; i++)
            {
                bool on = Mathf.Repeat(clock / StrobePeriod + i * 0.5f + _index * 0.13f, 1f) < StrobeLit;
                _strobes[i].SetPositionAndRotation(_root.TransformPoint(_strobeLocal[i]), view.rotation);
                _strobes[i].localScale = Vector3.one * (StrobeSize * (on ? 1f : 0.06f));
            }
        }

        private void Show(bool on)
        {
            if (_root.gameObject.activeSelf == on)
            {
                return;
            }

            _root.gameObject.SetActive(on);
            foreach (Transform strobe in _strobes)
            {
                strobe.gameObject.SetActive(on);
            }
        }
    }
}
