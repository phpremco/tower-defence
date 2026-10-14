using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>Placeholder look of a drone: a glowing disc with a small rotor and, while working, a light beam down to its target.</summary>
    [RequireComponent(typeof(Drone))]
    public sealed class DroneView : MonoBehaviour
    {
        private const float BeamThickness = 0.05f;
        private const float BeamEmission = 3f;
        private const float RotorSpeed = 540f;

        private Drone _drone;
        private Transform _rotor;
        private Transform _beam;

        public void Initialize(Drone drone)
        {
            _drone = drone;
            Color color = drone.Definition.Color;
            Material[] materials = ModelPalette.Create(color);
            Mesh body = StructureModelFactory.Drone(drone.Definition.Kind == DroneKind.Repair, out Mesh rotors);
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(transform, false);
            bodyObject.AddComponent<MeshFilter>().sharedMesh = body;
            bodyObject.AddComponent<MeshRenderer>().sharedMaterials = materials;

            var rotorObject = new GameObject("Rotors");
            rotorObject.transform.SetParent(transform, false);
            rotorObject.AddComponent<MeshFilter>().sharedMesh = rotors;
            rotorObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            _rotor = rotorObject.transform;
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
