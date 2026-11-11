using System;
using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// One drone in a cutscene: the imported (Blender) model posed from the shot time (its cycle sampled, its rotors
    /// turned on top), or the procedural drone of a definition when there is no art. Its cargo pod, when it has one,
    /// glows while it is loaded. Holds no state between frames, so a sought moment matches a played one.
    /// </summary>
    public sealed class CutsceneDrone
    {
        private const string RotorPrefix = "Rotor";
        private const string CargoSuffix = "_Cargo";

        // An empty pod keeps a faint glow, as in the level (DroneArtRig).
        private const float EmptyCargoGlow = 0.15f;
        // Round 39: the points a landed drone stands on are its vertices within this much (metres) of its lowest one, at most
        // this many of them; a drone at rest holds the first frame of its idle, so the feet stay where they were measured.
        private const float FootBand = 0.03f;
        private const int MaxFeet = 48;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly ArtModel _art;
        private readonly DroneModelBuilder.Rig _rig;
        private readonly Transform[] _rotors = new Transform[0];
        private readonly Quaternion[] _rotorRest = new Quaternion[0];
        private readonly Material _cargo;
        private readonly Color _cargoGlow;
        private Vector3[] _feet;

        public CutsceneDrone(Transform holder, ModelArtDefinition art, DroneDefinition fallback)
        {
            Holder = holder;
            if (art == null)
            {
                if (fallback != null)
                {
                    _rig = DroneModelBuilder.Build(holder, fallback);
                }

                return;
            }

            _art = ArtModel.CreateSampled(holder, art, ownMaterial: false);
            _rotors = _art.Numbered(RotorPrefix);
            _rotorRest = new Quaternion[_rotors.Length];
            for (int i = 0; i < _rotors.Length; i++)
            {
                _rotorRest[i] = _rotors[i].localRotation;
            }

            Renderer[] cargo = _art.RenderersEndingWith(CargoSuffix);
            if (cargo.Length > 0)
            {
                _cargo = new Material(_art.Material) { name = art.name + CargoSuffix };
                _cargoGlow = _cargo.GetColor(EmissionColorId);
                foreach (Renderer renderer in cargo)
                {
                    renderer.sharedMaterial = _cargo;
                }
            }
        }

        /// <summary>The holder the drone is placed and turned with.</summary>
        public Transform Holder { get; }

        /// <summary>True when the drone wears an imported model.</summary>
        public bool HasArt => _art != null;

        /// <summary>
        /// Where the drone's holder goes so it stands on the ground at <paramref name="at"/> (x, z) turned to
        /// <paramref name="facing"/>: the ground is sampled under every foot or skid (its lowest points at rest) and the
        /// highest of them carries it, so no foot sinks in (round 39: haulers and builders sank into slopes and banks).
        /// </summary>
        public Vector3 RestOn(Vector3 at, Quaternion facing, Func<float, float, float> ground)
        {
            Vector3[] feet = Feet();
            if (feet.Length == 0)
            {
                return new Vector3(at.x, ground(at.x, at.z), at.z);
            }

            float y = float.NegativeInfinity;
            foreach (Vector3 foot in feet)
            {
                Vector3 offset = facing * Vector3.Scale(foot, Holder.localScale);
                y = Mathf.Max(y, ground(at.x + offset.x, at.z + offset.z) - offset.y);
            }

            return new Vector3(at.x, y, at.z);
        }

        /// <summary>The drone's lowest points at rest (the first frame of its idle) in its holder's unscaled space, measured once.</summary>
        public Vector3[] Feet()
        {
            if (_feet != null)
            {
                return _feet;
            }

            Pose(ModelClipRole.Idle, 0f, 0f, false);
            var points = new List<Vector3>();
            var baked = new Mesh();
            foreach (Renderer renderer in Holder.GetComponentsInChildren<Renderer>(true))
            {
                Vector3[] vertices;
                Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.BakeMesh(baked, true);
                    vertices = baked.vertices;
                }
                else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    vertices = filter.sharedMesh.vertices;
                }
                else
                {
                    continue;
                }

                foreach (Vector3 vertex in vertices)
                {
                    points.Add(toWorld.MultiplyPoint3x4(vertex));
                }
            }

            UnityEngine.Object.DestroyImmediate(baked);
            _feet = LowestPoints(points);
            return _feet;
        }

        // The vertices near the lowest one, back in the holder's frame (position, turn and scale removed), thinned evenly.
        private Vector3[] LowestPoints(List<Vector3> points)
        {
            if (points.Count == 0)
            {
                return new Vector3[0];
            }

            float lowest = float.PositiveInfinity;
            foreach (Vector3 point in points)
            {
                lowest = Mathf.Min(lowest, point.y);
            }

            var feet = new List<Vector3>();
            foreach (Vector3 point in points)
            {
                if (point.y <= lowest + FootBand)
                {
                    feet.Add(Holder.InverseTransformPoint(point));
                }
            }

            int step = Mathf.Max(1, Mathf.CeilToInt(feet.Count / (float)MaxFeet));
            var kept = new List<Vector3>();
            for (int i = 0; i < feet.Count; i += step)
            {
                kept.Add(feet[i]);
            }

            return kept.ToArray();
        }

        /// <summary>A socket of the imported model (a welding arm, a cargo grip), or null.</summary>
        public Transform Socket(string name) => _art?.Find(name);

        /// <summary>
        /// Poses the drone: the cycle of <paramref name="role"/> at <paramref name="time"/> (falling back to its idle), the
        /// rotors turned by <paramref name="rotorDegrees"/> (neighbours opposite) and the pod lit when loaded.
        /// </summary>
        public void Pose(ModelClipRole role, float time, float rotorDegrees, bool loaded)
        {
            if (_art == null)
            {
                PoseProcedural(rotorDegrees, loaded);
                return;
            }

            if (!_art.Sample(role, time, loop: true))
            {
                _art.Sample(ModelClipRole.Idle, time, loop: true);
            }

            for (int i = 0; i < _rotors.Length; i++)
            {
                float turn = i % 2 == 0 ? rotorDegrees : -rotorDegrees;
                _rotors[i].localRotation = _rotorRest[i] * Quaternion.AngleAxis(turn, Vector3.up);
            }

            if (_cargo != null)
            {
                _cargo.SetColor(EmissionColorId, _cargoGlow * (loaded ? 1f : EmptyCargoGlow));
            }
        }

        private void PoseProcedural(float rotorDegrees, bool loaded)
        {
            if (_rig == null)
            {
                return;
            }

            foreach (Transform rotor in _rig.Rotors)
            {
                rotor.localRotation = Quaternion.AngleAxis(rotorDegrees, _rig.RotorAxis);
            }

            if (_rig.Cargo != null)
            {
                _rig.Cargo.enabled = loaded;
            }
        }
    }
}
