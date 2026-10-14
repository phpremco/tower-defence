using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// Loot that defeated enemies leave behind when collector drones are flying. It is a bonus on top of the normal
    /// reward and fades after a while, so collectors are worth having but never required.
    /// </summary>
    public sealed class PickupField : MonoBehaviour
    {
        private const float LifetimeSeconds = 12f;
        private const float BobHeight = 0.12f;
        private const float BobSpeed = 4f;
        private const float RestHeight = 0.35f;
        private const float Size = 1.4f;
        private const float SpinDegrees = 120f;
        private static readonly Color PickupColor = new Color(1f, 0.85f, 0.2f);

        private static Mesh Crystal => _crystal != null ? _crystal : (_crystal = StructureModelFactory.LootCrystal());

        private static Material[] Materials => _materials != null && _materials[0] != null ? _materials : (_materials = ModelPalette.Create(PickupColor));

        private static Mesh _crystal;
        private static Material[] _materials;

        private readonly List<Pickup> _pickups = new List<Pickup>();

        private sealed class Pickup
        {
            public Transform Body;
            public Vector3 Position;
            public int Value;
            public float Remaining;
        }

        public int Count => _pickups.Count;

        public void Drop(Vector3 position, int value)
        {
            if (value <= 0)
            {
                return;
            }

            var body = new GameObject("Pickup");
            body.transform.SetParent(transform, false);
            body.transform.localScale = Vector3.one * Size;
            body.AddComponent<MeshFilter>().sharedMesh = Crystal;
            body.AddComponent<MeshRenderer>().sharedMaterials = Materials;
            var flat = new Vector3(position.x, 0f, position.z);
            body.transform.position = flat + Vector3.up * RestHeight;
            _pickups.Add(new Pickup { Body = body.transform, Position = flat, Value = value, Remaining = LifetimeSeconds });
        }

        /// <summary>Position of the loot closest to a point on the ground plane, or false when there is none.</summary>
        public bool TryFindNearest(Vector3 from, out Vector3 position)
        {
            float best = float.PositiveInfinity;
            position = Vector3.zero;
            for (int i = 0; i < _pickups.Count; i++)
            {
                float dx = _pickups[i].Position.x - from.x;
                float dz = _pickups[i].Position.z - from.z;
                float sqr = dx * dx + dz * dz;
                if (sqr < best)
                {
                    best = sqr;
                    position = _pickups[i].Position;
                }
            }

            return best < float.PositiveInfinity;
        }

        /// <summary>Takes the loot at a position, if any is left, and returns its value.</summary>
        public int Collect(Vector3 position)
        {
            for (int i = 0; i < _pickups.Count; i++)
            {
                Vector3 delta = _pickups[i].Position - position;
                delta.y = 0f;
                if (delta.sqrMagnitude < 0.01f)
                {
                    int value = _pickups[i].Value;
                    Remove(i);
                    return value;
                }
            }

            return 0;
        }

        private void Update()
        {
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                Pickup pickup = _pickups[i];
                pickup.Remaining -= Time.deltaTime;
                if (pickup.Remaining <= 0f)
                {
                    Remove(i);
                    continue;
                }

                float bob = Mathf.Sin(Time.time * BobSpeed + i) * BobHeight;
                pickup.Body.position = pickup.Position + Vector3.up * (RestHeight + bob);
                pickup.Body.Rotate(0f, SpinDegrees * Time.deltaTime, 0f, Space.World);
            }
        }

        private void Remove(int index)
        {
            GameObject body = _pickups[index].Body.gameObject;
            if (Application.isPlaying)
            {
                Destroy(body);
            }
            else
            {
                DestroyImmediate(body);
            }


            _pickups.RemoveAt(index);
        }
    }
}
