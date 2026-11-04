using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The hauler flock: drones fly a loop from the base to the drill, hover to load (the cargo pod lights up) and
    /// carry the ore home, each on its own height and lane so the flock reads as a stream. One drone can be sent to
    /// the core with the first crystal. Positions follow from time alone.
    /// </summary>
    public sealed class FieldDrones
    {
        private const float LoopSeconds = 9f;
        private const float CruiseHeight = 2.6f;
        private const float HeightSpread = 0.9f;
        private const float LaneSpread = 1.4f;
        private const float RotorDegreesPerSecond = 2400f;
        private const float LoadShare = 0.12f;
        private const float BankDegrees = 14f;
        private const float FanShare = 0.35f;
        private const float DeliveryHeight = 3.2f;
        private const float CrystalEmission = 6f;
        // Cinematic scale: at their real size next to the drill the haulers read as specks in a wide shot.
        private const float HaulerScale = 2.2f;
        // The imported hauler is built longer than the procedural one (README round 18), so it needs less of the boost.
        private const float ArtHaulerScale = 1.6f;
        private const float CarryDrop = 0.28f;

        private readonly List<CutsceneDrone> _drones = new List<CutsceneDrone>();
        private readonly List<Transform> _holders = new List<Transform>();
        private readonly Vector3 _home;
        private readonly Vector3 _pickup;
        private readonly CutsceneAudio _audio;
        private readonly AudioSource _fans;
        private readonly Transform _crystal;

        public FieldDrones(Transform parent, Vector3 home, Vector3 pickup, Color crystalColour, CutsceneContext context)
        {
            _home = home;
            _pickup = pickup;
            _audio = context.Audio;
            DroneDefinition hauler = context.Cast != null ? context.Cast.Hauler : null;
            ModelArtDefinition art = context.Cast != null ? context.Cast.HaulerArt : null;
            if (hauler != null || art != null)
            {
                int count = context.Scaled(context.Cast.DroneCount);
                for (int i = 0; i < count; i++)
                {
                    var holder = new GameObject($"Hauler{i}").transform;
                    holder.SetParent(parent, false);
                    holder.localScale = Vector3.one * (art != null ? ArtHaulerScale : HaulerScale);
                    _drones.Add(new CutsceneDrone(holder, art, hauler));
                    _holders.Add(holder);
                }
            }

            var crystalBuilder = new PropMeshBuilder(1);
            crystalBuilder.AddTube(new[] { Vector3.down * 0.18f, Vector3.down * 0.08f, Vector3.up * 0.08f, Vector3.up * 0.18f }, new[] { 0f, 0.07f, 0.07f, 0f }, 6, 0, true);
            _crystal = new GameObject("Crystal").transform;
            _crystal.SetParent(parent, false);
            _crystal.gameObject.AddComponent<MeshFilter>().sharedMesh = crystalBuilder.ToMesh("CarriedCrystal");
            _crystal.gameObject.AddComponent<MeshRenderer>().sharedMaterial = SurfaceMaterials.Create(crystalColour, CrystalEmission, 0.9f);
            _crystal.gameObject.SetActive(false);
            _fans = context.Audio.CreateLoop("HaulerFans", CutsceneSound.DroneFans);
        }

        public void Hide()
        {
            foreach (Transform holder in _holders)
            {
                holder.gameObject.SetActive(false);
            }

            _crystal.gameObject.SetActive(false);
            _audio.SetLoopVolume(_fans, 0f);
        }

        /// <summary>
        /// Flies the flock. <paramref name="launch"/> 0..1 staggers the take-off, so the flock leaves the base one
        /// drone after another; <paramref name="loudness"/> sets the fan hum.
        /// </summary>
        public void Pose(float time, float launch, float loudness)
        {
            for (int i = 0; i < _drones.Count; i++)
            {
                float start = i / (float)Mathf.Max(1, _drones.Count);
                float awake = CutsceneMath.Span(launch, start * 0.8f, start * 0.8f + 0.2f);
                _holders[i].gameObject.SetActive(awake > 0f);
                if (awake <= 0f)
                {
                    continue;
                }

                float loop = Mathf.Repeat(time / LoopSeconds + CutsceneMath.Hash(i, 21), 1f);
                Place(i, Sample(i, loop, awake), time, loop);
            }

            _audio.SetLoopVolume(_fans, FanShare * loudness * Mathf.Min(1f, launch * 3f));
        }

        /// <summary>
        /// The first crystal: drone 0 leaves the flock and carries it from the drill to the core socket over the
        /// window 0..1 of <paramref name="share"/>; the crystal stays in the socket afterwards.
        /// </summary>
        public void PoseDelivery(float share, Vector3 socket, float time)
        {
            if (_drones.Count == 0)
            {
                return;
            }

            float t = CutsceneMath.Smooth(Mathf.Clamp01(share));
            Vector3 from = _pickup + Vector3.up * 1.5f;
            Vector3 position = Vector3.Lerp(from, socket + Vector3.up * 0.9f, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * DeliveryHeight);
            _holders[0].gameObject.SetActive(true);
            Place(0, position, time, t);
            _crystal.gameObject.SetActive(true);
            _crystal.position = share >= 1f ? socket : position + Vector3.down * (CarryDrop * HaulerScale);
            _crystal.rotation = Quaternion.Euler(0f, time * 90f, 0f);
        }

        // Out along the left lane and back along the right one, rising off the pad and dropping to hover at the drill.
        private Vector3 Sample(int index, float loop, float awake)
        {
            float lane = (CutsceneMath.Hash(index, 5) - 0.5f) * LaneSpread;
            float height = CruiseHeight + CutsceneMath.Hash(index, 7) * HeightSpread;
            Vector3 across = Vector3.Cross(Vector3.up, (_pickup - _home).normalized);
            float outward = loop < 0.5f ? loop * 2f : 1f - (loop - 0.5f) * 2f;
            float travel = CutsceneMath.Smooth(CutsceneMath.Span(outward, LoadShare, 1f - LoadShare));
            float side = loop < 0.5f ? 1f : -1f;
            Vector3 flat = Vector3.Lerp(_home, _pickup, travel) + across * (lane + side * 0.6f);
            float lift = Mathf.Sin(travel * Mathf.PI) * 0.5f + Mathf.Lerp(0.8f, 1f, Mathf.Min(travel, 1f - travel) * 2f);
            Vector3 position = flat + Vector3.up * (height * lift);
            // A drone that has only just taken off still sits low over the pad.
            return Vector3.Lerp(_home + Vector3.up * 0.4f, position, awake);
        }

        private void Place(int index, Vector3 position, float time, float loop)
        {
            Transform holder = _holders[index];
            CutsceneDrone drone = _drones[index];
            Vector3 heading = loop < 0.5f ? _pickup - _home : _home - _pickup;
            heading.y = 0f;
            Quaternion facing = heading.sqrMagnitude > 0.001f ? Quaternion.LookRotation(heading) : Quaternion.identity;
            float bank = Mathf.Sin(time * 1.3f + index) * BankDegrees * 0.3f;
            holder.SetPositionAndRotation(position, facing * Quaternion.Euler(6f, 0f, bank));
            // Loaded on the way home: the pod glows with the ore it carries.
            drone.Pose(ModelClipRole.Move, time + index * 0.29f, time * RotorDegreesPerSecond + index * 37f, loop >= 0.5f);
        }
    }
}
