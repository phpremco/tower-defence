using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// The look of a drone: its own model, rotors that spin on their pivots, a nose that turns the way it flies,
    /// a gentle bob, a light beam down to whatever it works on, a cargo pod that lights up when loaded, muzzle
    /// flashes when it fires and a floating "+money" when it delivers. Reads the drone, never drives it.
    /// </summary>
    [RequireComponent(typeof(Drone))]
    public sealed class DroneView : MonoBehaviour
    {
        private const float BeamThickness = 0.05f;
        private const float BeamEmission = 3f;
        private const float RotorSpeed = 1500f;
        private const float TurnSpeed = 8f;
        private const float BobHeight = 0.06f;
        private const float BobSpeed = 2.3f;
        private const float BankDegrees = 14f;
        private const float FlashSeconds = 0.06f;
        private const float FlashSize = 0.12f;
        private const float FlashEmission = 6f;
        private static readonly Color MoneyColor = new Color(1f, 0.9f, 0.4f);

        private Drone _drone;
        private DroneModelBuilder.Rig _rig;
        private Transform _beam;
        private Transform[] _flashes;
        private float _flashRemaining;
        private float _bobPhase;
        private Quaternion _facing = Quaternion.identity;

        public void Initialize(Drone drone)
        {
            _drone = drone;
            _rig = DroneModelBuilder.Build(transform, drone.Definition);
            _bobPhase = Random.value * Mathf.PI * 2f;

            Color color = drone.Definition.Color;
            _beam = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, color, null, Vector3.zero, Vector3.one, BeamEmission).transform;
            _beam.SetParent(transform.parent, true);
            _beam.gameObject.SetActive(false);

            _flashes = new Transform[_rig.Muzzles.Length];
            for (int i = 0; i < _flashes.Length; i++)
            {
                GameObject flash = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(color, Color.white, 0.6f), _rig.Root.transform, _rig.Muzzles[i], Vector3.one * FlashSize, FlashEmission);
                flash.SetActive(false);
                _flashes[i] = flash.transform;
            }

            _drone.Fired += OnFired;
            _drone.Delivered += OnDelivered;
        }

        private void OnDestroy()
        {
            if (_beam != null)
            {
                Destroy(_beam.gameObject);
            }

            if (_drone != null)
            {
                _drone.Fired -= OnFired;
                _drone.Delivered -= OnDelivered;
            }
        }

        private void OnFired(Vector3 target)
        {
            _flashRemaining = FlashSeconds;
            foreach (Transform flash in _flashes)
            {
                flash.gameObject.SetActive(true);
            }
        }

        private void OnDelivered(int value) => MoneyPopup.Show(transform.parent, transform.position, value, MoneyColor);

        private void Update()
        {
            if (_drone == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            foreach (Transform rotor in _rig.Rotors)
            {
                rotor.Rotate(0f, RotorSpeed * dt, 0f, Space.Self);
            }

            // Turn towards the heading and lean into the turn a little, like a real quadcopter.
            Quaternion wanted = Quaternion.LookRotation(_drone.Heading, Vector3.up);
            float turn = Quaternion.Angle(_facing, wanted);
            _facing = Quaternion.Slerp(_facing, wanted, TurnSpeed * dt);
            float bank = Mathf.Clamp(turn, -BankDegrees, BankDegrees) * Mathf.Sign(Vector3.Dot(Vector3.Cross(_facing * Vector3.forward, wanted * Vector3.forward), Vector3.up));
            _bobPhase += BobSpeed * dt;
            _rig.Root.transform.localRotation = _facing * Quaternion.Euler(0f, 0f, -bank);
            _rig.Root.transform.localPosition = Vector3.up * (Mathf.Sin(_bobPhase) * BobHeight);

            if (_rig.Cargo != null)
            {
                _rig.Cargo.sharedMaterial = _rig.Materials[_drone.IsCarrying ? ModelPalette.Glow : ModelPalette.Dark];
            }

            if (_flashRemaining > 0f)
            {
                _flashRemaining -= dt;
                if (_flashRemaining <= 0f)
                {
                    foreach (Transform flash in _flashes)
                    {
                        flash.gameObject.SetActive(false);
                    }
                }
            }

            bool working = _drone.IsAlive && _drone.WorkPoint.HasValue;
            _beam.gameObject.SetActive(working);
            if (!working)
            {
                return;
            }

            Vector3 from = transform.position;
            Vector3 to = _drone.WorkPoint.Value;
            Vector3 delta = to - from;
            _beam.position = from + delta * 0.5f;
            _beam.rotation = Quaternion.LookRotation(delta);
            _beam.localScale = new Vector3(BeamThickness, BeamThickness, delta.magnitude);
        }
    }
}
