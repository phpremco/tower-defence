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
        private const float LaneSpread = 2.6f;
        private const float RotorDegreesPerSecond = 2400f;
        private const float LoadShare = 0.12f;
        private const float MaxBankDegrees = 10f;
        private const float BankPerTurnDegree = 0.35f;
        private const float MaxPitchDegrees = 3f;
        private const float PitchPerSpeed = 0.8f;
        private const float HeadingProbe = 0.015f;
        private const float LaneSwing = 0.9f;
        private const float HoverSpeed = 0.8f;
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
                PlaceOnLoop(i, loop, awake, time);
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

            float t = Mathf.Clamp01(share);
            Vector3 position = DeliveryAt(t, socket);
            Vector3 velocity = DeliveryAt(Mathf.Min(1f, t + HeadingProbe), socket) - DeliveryAt(Mathf.Max(0f, t - HeadingProbe), socket);
            _holders[0].gameObject.SetActive(true);
            Place(0, position, velocity, velocity, time, loaded: true);
            _crystal.gameObject.SetActive(true);
            _crystal.position = share >= 1f ? socket : position + Vector3.down * (CarryDrop * HaulerScale);
            _crystal.rotation = Quaternion.Euler(0f, time * 90f, 0f);
        }

        // From the drill up in an arc and down onto the core socket.
        private Vector3 DeliveryAt(float t, Vector3 socket)
        {
            float eased = CutsceneMath.Smooth(t);
            Vector3 from = _pickup + Vector3.up * 1.5f;
            return Vector3.Lerp(from, socket + Vector3.up * 0.9f, eased) + Vector3.up * (Mathf.Sin(eased * Mathf.PI) * DeliveryHeight);
        }

        // A closed oval: out along one side of the lane, a wide turn round the drill while it hovers to load, back
        // along the other side and a turn over the pad. Smooth everywhere, so the heading never snaps round.
        private Vector3 Sample(int index, float loop, float awake)
        {
            float lane = (CutsceneMath.Hash(index, 5) - 0.5f) * LaneSpread;
            float height = CruiseHeight + CutsceneMath.Hash(index, 7) * HeightSpread;
            Vector3 across = Vector3.Cross(Vector3.up, (_pickup - _home).normalized);
            float outward = 0.5f - 0.5f * Mathf.Cos(loop * Mathf.PI * 2f);
            float travel = CutsceneMath.Smooth(CutsceneMath.Span(outward, LoadShare, 1f - LoadShare));
            float swing = Mathf.Sin(loop * Mathf.PI * 2f) * LaneSwing;
            Vector3 flat = Vector3.Lerp(_home, _pickup, travel) + across * (lane + swing);
            float lift = Mathf.Sin(travel * Mathf.PI) * 0.5f + Mathf.Lerp(0.8f, 1f, Mathf.Min(travel, 1f - travel) * 2f);
            Vector3 position = flat + Vector3.up * (height * lift);
            // A drone that has only just taken off still sits low over the pad.
            return Vector3.Lerp(_home + Vector3.up * 0.4f, position, awake);
        }

        // Heading from the way the drone moves along its own path (a step either side of the moment), so it always
        // faces where it flies, banks a little into its turns and dips its nose only with speed.
        private void PlaceOnLoop(int index, float loop, float awake, float time)
        {
            Vector3 position = Sample(index, loop, awake);
            Vector3 before = Sample(index, Mathf.Repeat(loop - HeadingProbe, 1f), awake);
            Vector3 after = Sample(index, Mathf.Repeat(loop + HeadingProbe, 1f), awake);
            Vector3 incoming = position - before;
            Vector3 outgoing = after - position;
            Place(index, position, incoming, outgoing, time, loop >= 0.5f);
        }

        private void Place(int index, Vector3 position, Vector3 incoming, Vector3 outgoing, float time, bool loaded)
        {
            Transform holder = _holders[index];
            Vector3 heading = incoming + outgoing;
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.000001f)
            {
                // Hovering still: keep facing the drill, nose level.
                heading = _pickup - _home;
                heading.y = 0f;
            }

            Quaternion facing = Quaternion.LookRotation(heading);
            float turn = Vector3.SignedAngle(Flat(incoming), Flat(outgoing), Vector3.up);
            float bank = Mathf.Clamp(turn * BankPerTurnDegree, -MaxBankDegrees, MaxBankDegrees);
            float speed = Flat(outgoing).magnitude / (HeadingProbe * LoopSeconds);
            float pitch = Mathf.Min(MaxPitchDegrees, speed * PitchPerSpeed);
            holder.SetPositionAndRotation(position, facing * Quaternion.Euler(pitch, 0f, -bank));
            // Loaded on the way home: the pod glows with the ore it carries.
            // The flight cycle only while it travels; hovering to load it holds still in its work pose, so its own clip does not rock it.
            ModelClipRole role = speed > HoverSpeed ? ModelClipRole.Move : ModelClipRole.Work;
            _drones[index].Pose(role, time + index * 0.29f, time * RotorDegreesPerSecond + index * 37f, loaded);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
