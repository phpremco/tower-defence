using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>Placeholder look of a drone: a glowing disc with a small rotor and, while working, a light beam down to its target.</summary>
    [RequireComponent(typeof(Drone))]
    public sealed class DroneView : MonoBehaviour
    {
        private const float BodyEmission = 1.4f;
        private const float BeamThickness = 0.05f;
        private const float BeamEmission = 3f;
        private const float RotorSpeed = 540f;
        private static readonly Color RotorColor = new Color(0.9f, 0.95f, 1f);

        private Drone _drone;
        private Transform _rotor;
        private Transform _beam;

        public void Initialize(Drone drone)
        {
            _drone = drone;
            Color color = drone.Definition.Color;
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, color, transform, Vector3.zero, new Vector3(0.45f, 0.08f, 0.45f), BodyEmission);
            _rotor = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, RotorColor, transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.6f, 0.02f, 0.06f)).transform;
            _beam = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, color, null, Vector3.zero, Vector3.one, BeamEmission).transform;
            _beam.SetParent(transform.parent, true);
            _beam.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_beam != null)
            {
                Destroy(_beam.gameObject);
            }
        }

        private void Update()
        {
            if (_drone == null)
            {
                return;
            }

            _rotor.Rotate(0f, RotorSpeed * Time.deltaTime, 0f);

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
