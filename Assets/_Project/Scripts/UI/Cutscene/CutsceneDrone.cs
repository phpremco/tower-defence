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
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly ArtModel _art;
        private readonly DroneModelBuilder.Rig _rig;
        private readonly Transform[] _rotors = new Transform[0];
        private readonly Quaternion[] _rotorRest = new Quaternion[0];
        private readonly Material _cargo;
        private readonly Color _cargoGlow;

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
