using System;
using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The hauler flock: drones lift off their places beside the base one after another and fly a loop to the ore heap beside the
    /// drill, dip down to it and come up with a chunk of ore or crystal (the level's own Blender rocks and crystals)
    /// hanging under them, carry it home and set it down on the pad. Each flies its own lane and height, so the flock
    /// reads as a stream. For the core, drone 0 leaves the stream from wherever it is, picks the first crystal off the
    /// heap and lowers it into the energy core. Positions follow from the flock's clock alone, which runs on across the
    /// shots, so a drone never jumps at a cut.
    /// </summary>
    public sealed class FieldDrones
    {
        /// <summary>Where the first crystal is and whether it has gone into the core.</summary>
        public struct Delivery
        {
            public bool Carried;
            public Vector3 CarriedTop;
            public float Insert;
        }

        private const float LoopSeconds = 9f;
        private const float FlightClearance = 0.3f;
        private const float CruiseHeight = 2.6f;
        private const float HeightSpread = 0.9f;
        private const float LaneSpread = 2.6f;
        private const float RotorDegreesPerSecond = 2400f;
        private const float LoadShare = 0.12f;
        private const float MaxBankDegrees = 10f;
        private const float BankPerTurnDegree = 0.35f;
        private const float MaxPitchDegrees = 3f;
        private const float PitchPerSpeed = 0.8f;
        private const float HeadingProbe = 0.012f;
        private const float LaneSwing = 0.9f;
        private const float HoverSpeed = 0.8f;
        private const float FanShare = 0.35f;
        // Cinematic scale: at their real size next to the drill the haulers read as specks in a wide shot.
        private const float HaulerScale = 2.2f;
        // The imported hauler is built longer than the procedural one (README round 18), so it needs less of the boost.
        private const float ArtHaulerScale = 1.6f;

        // The dips: how close to the heap and the pad a drone comes to grab and drop, how wide in loop phase the dip is.
        private const float GrabHeight = 0.75f;
        private const float DipWidth = 0.055f;
        private const float LaneAtHeap = 0.25f;

        // The ore heap beside the drill and the chunks the haulers carry, sized from the level's decor.
        private const int HeapChunks = 9;
        private const float HeapRadius = 0.8f;
        private const float ChunkSize = 0.42f;
        private const float CarriedChunk = 0.45f;
        // The chunk hangs this far under the grip (drone space), clear of the cargo pod.
        private const float ChunkHang = 0.16f;
        private const string GripSocket = "Grip";

        // Drone 0's errand with the first crystal: to the heap, over to the core, lower it in, rise and hover.
        private const float GrabAt = 0.2f;
        private const float OverCoreAt = 0.48f;
        private const float InsertAt = 0.6f;
        private const float RiseAt = 0.74f;
        private const float OverCoreHeight = 1.6f;
        private const float RiseHeight = 0.7f;

        private readonly List<CutsceneDrone> _drones = new List<CutsceneDrone>();
        private readonly List<Transform> _holders = new List<Transform>();
        private readonly List<Transform> _chunks = new List<Transform>();
        private readonly List<Vector3> _homes = new List<Vector3>();
        private readonly Vector3 _pad;
        private readonly Vector3 _heap;
        private readonly float _crystalHang;
        private readonly CutsceneAudio _audio;
        private readonly AudioSource _fans;
        private readonly Func<float, float, float> _ground;
        private readonly List<float> _footDepths = new List<float>();

        /// <param name="park">Where the next hauler rests before it takes off (open ground beside the base, clear of the rest).</param>
        /// <param name="ground">The drawn ground's height at (x, z): a parked hauler stands on it with every foot (round 39).</param>
        public FieldDrones(Transform parent, Vector3 pad, Vector3 heap, CutsceneContext context, VisualTheme theme, float crystalHang, Func<Vector3> park, Func<float, float, float> ground)
        {
            _pad = pad;
            _heap = heap;
            _ground = ground;
            _crystalHang = crystalHang;
            _audio = context.Audio;
            bool low = context.ParticleScale < 1f;
            EnvironmentSet environment = theme.Environment;
            BuildHeap(parent, environment, low);
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
                    var drone = new CutsceneDrone(holder, art, hauler);
                    _drones.Add(drone);
                    _holders.Add(holder);
                    // Round 37: eight haulers do not fit round the pad (they stood in each other and the storage tanks).
                    // Round 39: each stands on its skids, the ground sampled under every one of them.
                    _homes.Add(drone.RestOn(park(), Parked, ground));
                    _footDepths.Add(FootDepth(drone, holder));
                    Transform grip = drone.Socket(GripSocket);
                    // Every hauler brings Crysium home: the glowing crystal reads under it from any distance.
                    Transform chunk = CutsceneProps.Decor(grip != null ? grip : holder, $"Load{i}", environment, PropKind.Crystal, i, CarriedChunk, low, true);
                    chunk.localPosition = Vector3.down * ChunkHang;
                    _chunks.Add(chunk);
                }
            }

            _fans = context.Audio.CreateLoop("HaulerFans", CutsceneSound.DroneFans);
        }

        // How a parked hauler stands: facing the heap from the pad, level.
        private Quaternion Parked => Quaternion.LookRotation(Flat(_heap - _pad));

        public void Hide()
        {
            foreach (Transform holder in _holders)
            {
                holder.gameObject.SetActive(false);
            }

            _audio.SetLoopVolume(_fans, 0f);
        }

        /// <summary>Every hauler resting on the pad, rotors still: the flock before it takes off.</summary>
        public void Park(float time)
        {
            for (int i = 0; i < _drones.Count; i++)
            {
                _holders[i].gameObject.SetActive(true);
                _holders[i].SetPositionAndRotation(_homes[i], Parked);
                // At rest a hauler holds its idle's first frame: its feet were measured there.
                _drones[i].Pose(ModelClipRole.Idle, 0f, 0f, false);
                _chunks[i].gameObject.SetActive(false);
            }

            _audio.SetLoopVolume(_fans, 0f);
        }

        /// <summary>
        /// Flies the flock at <paramref name="clock"/> (seconds since the first take-off). <paramref name="launch"/> 0..1
        /// staggers the take-off from the pad; <paramref name="loudness"/> sets the fan hum; drone
        /// <paramref name="skip"/> (or none with -1) is on an errand of its own.
        /// </summary>
        public void Pose(float clock, float launch, float loudness, int skip = -1)
        {
            for (int i = 0; i < _drones.Count; i++)
            {
                if (i == skip)
                {
                    continue;
                }

                _holders[i].gameObject.SetActive(true);
                float awake = Awake(i, launch);
                float loop = Loop(i, clock);
                Vector3 position = Sample(i, loop, awake);
                Vector3 before = Sample(i, Mathf.Repeat(loop - HeadingProbe, 1f), awake);
                Vector3 after = Sample(i, Mathf.Repeat(loop + HeadingProbe, 1f), awake);
                // Loaded on the way home: from the grab at the heap (half way round) to the drop on the pad.
                bool loaded = awake >= 1f && loop >= 0.5f;
                Place(i, position, position - before, after - position, clock, loaded, awake > 0f);
            }

            _audio.SetLoopVolume(_fans, FanShare * loudness * Mathf.Min(1f, launch * 3f));
        }

        /// <summary>
        /// Drone 0 takes the first crystal to the core over <paramref name="share"/> 0..1 of the shot, starting where the
        /// flock had it at <paramref name="clock"/>; <paramref name="coreSocket"/> is the foot of the crystal in the core.
        /// </summary>
        public Delivery PoseDelivery(float share, float clock, Vector3 coreSocket, float time)
        {
            var delivery = new Delivery();
            if (_drones.Count == 0)
            {
                return delivery;
            }

            float t = Mathf.Clamp01(share);
            Vector3 start = Sample(0, Loop(0, clock), 1f);
            Vector3 position = Errand(t, start, coreSocket);
            Vector3 before = Errand(Mathf.Max(0f, t - HeadingProbe), start, coreSocket);
            Vector3 after = Errand(Mathf.Min(1f, t + HeadingProbe), start, coreSocket);
            _holders[0].gameObject.SetActive(true);
            bool carrying = t >= GrabAt && t < InsertAt;
            Place(0, position, position - before, after - position, time, carrying, true);
            _chunks[0].gameObject.SetActive(false);
            delivery.Carried = carrying;
            delivery.CarriedTop = position - Vector3.up * CarriedDrop();
            delivery.Insert = t >= InsertAt ? 1f : 0f;
            return delivery;
        }

        // How far under the drone's pivot the crystal's top rides (the grip sits under the belly).
        private float CarriedDrop() => GrabHeight * 0.75f;

        // The errand's path, eased between its stops: off its loop to the heap, up and over to the core, down until the
        // crystal stands in its socket, then up a little to watch it light.
        private Vector3 Errand(float t, Vector3 start, Vector3 socket)
        {
            Vector3 heap = _heap + Vector3.up * GrabHeight;
            Vector3 inserted = socket + Vector3.up * (_crystalHang * FieldBase.CrystalCarriedShare + CarriedDrop());
            Vector3 over = inserted + Vector3.up * OverCoreHeight;
            Vector3 risen = inserted + Vector3.up * RiseHeight;
            if (t < GrabAt)
            {
                return Ease(start, heap, CutsceneMath.Span(t, 0f, GrabAt), 0.8f);
            }

            if (t < OverCoreAt)
            {
                return Ease(heap, over, CutsceneMath.Span(t, GrabAt, OverCoreAt), 2f);
            }

            if (t < InsertAt)
            {
                return Ease(over, inserted, CutsceneMath.Span(t, OverCoreAt, InsertAt), 0f);
            }

            return Ease(inserted, risen, CutsceneMath.Span(t, InsertAt, RiseAt), 0f);
        }

        private static Vector3 Ease(Vector3 from, Vector3 to, float share, float arc)
        {
            float eased = CutsceneMath.Smooth(share);
            return Vector3.Lerp(from, to, eased) + Vector3.up * (Mathf.Sin(eased * Mathf.PI) * arc);
        }

        private float Awake(int index, float launch)
        {
            float start = index / (float)Mathf.Max(1, _drones.Count);
            return CutsceneMath.Span(launch, start * 0.55f, start * 0.55f + 0.45f);
        }

        // The drones spread evenly round the loop, so the stream never bunches up at the heap; the phase runs on with the clock.
        private float Loop(int index, float clock) => Mathf.Repeat(clock / LoopSeconds - index / (float)Mathf.Max(1, _drones.Count), 1f);

        // A closed oval: out along one side of the lane, a wide turn round the heap with a dip down to it, back along the
        // other side and a dip onto the pad. Smooth everywhere, so the heading never snaps round.
        private Vector3 Sample(int index, float loop, float awake)
        {
            float lane = (CutsceneMath.Hash(index, 5) - 0.5f) * LaneSpread;
            float height = CruiseHeight + CutsceneMath.Hash(index, 7) * HeightSpread;
            Vector3 home = _homes[index];
            Vector3 across = Vector3.Cross(Vector3.up, Flat(_heap - home).normalized);
            float outward = 0.5f - 0.5f * Mathf.Cos(loop * Mathf.PI * 2f);
            float travel = CutsceneMath.Smooth(CutsceneMath.Span(outward, LoadShare, 1f - LoadShare));
            float atHeap = Mathf.Exp(-Mathf.Pow((loop - 0.5f) / DipWidth, 2f));
            float atPad = Mathf.Exp(-Mathf.Pow(Mathf.Min(loop, 1f - loop) / DipWidth, 2f));
            float swing = Mathf.Sin(loop * Mathf.PI * 2f) * LaneSwing;
            Vector3 flat = Vector3.Lerp(home, _heap, travel) + across * ((lane + swing) * (1f - atHeap * (1f - LaneAtHeap)) * (1f - atPad));
            float lift = Mathf.Sin(travel * Mathf.PI) * 0.5f + Mathf.Lerp(0.8f, 1f, Mathf.Min(travel, 1f - travel) * 2f);
            float cruise = Mathf.Lerp(home.y, _heap.y, travel) + height * lift;
            float y = Mathf.Lerp(cruise, _heap.y + GrabHeight, atHeap);
            y = Mathf.Lerp(y, home.y + GrabHeight, atPad);
            var position = new Vector3(flat.x, y, flat.z);
            // A drone that has not taken off yet sits on the pad. Round 39: one parked up on the border flew its lane straight
            // through a ridge; in the air its feet keep a little above the drawn ground under it.
            Vector3 result = Vector3.Lerp(home, position, awake);
            if (awake > 0f)
            {
                result.y = Mathf.Max(result.y, _ground(result.x, result.z) + _footDepths[index] + FlightClearance * awake);
            }

            return result;
        }

        // How far below its holder a drone's feet hang, in metres.
        private static float FootDepth(CutsceneDrone drone, Transform holder)
        {
            float depth = 0f;
            foreach (Vector3 foot in drone.Feet())
            {
                depth = Mathf.Max(depth, -foot.y * holder.localScale.y);
            }

            return depth;
        }

        // Heading from the way the drone moves along its own path (a step either side of the moment), so it always
        // faces where it flies, banks a little into its turns and dips its nose only with speed.
        private void Place(int index, Vector3 position, Vector3 incoming, Vector3 outgoing, float time, bool loaded, bool flying)
        {
            Transform holder = _holders[index];
            Vector3 heading = incoming + outgoing;
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.000001f)
            {
                // Hovering still: keep facing the heap, nose level.
                heading = Flat(_heap - _pad);
            }

            Quaternion facing = flying ? Quaternion.LookRotation(heading) : Parked;
            float turn = Vector3.SignedAngle(Flat(incoming), Flat(outgoing), Vector3.up);
            float bank = Mathf.Clamp(turn * BankPerTurnDegree, -MaxBankDegrees, MaxBankDegrees);
            float speed = Flat(outgoing).magnitude / (HeadingProbe * LoopSeconds);
            float pitch = Mathf.Min(MaxPitchDegrees, speed * PitchPerSpeed);
            holder.SetPositionAndRotation(position, facing * Quaternion.Euler(pitch, 0f, -bank));
            // The flight cycle only while it travels; hovering to load it holds still in its work pose, so its own clip does not rock it.
            ModelClipRole role = !flying ? ModelClipRole.Idle : speed > HoverSpeed ? ModelClipRole.Move : ModelClipRole.Work;
            _drones[index].Pose(role, flying ? time + index * 0.29f : 0f, flying ? time * RotorDegreesPerSecond + index * 37f : 0f, loaded);
            _chunks[index].gameObject.SetActive(loaded);
        }

        // A heap of ore and crystal beside the drill, from the level's own rocks and crystals.
        private void BuildHeap(Transform parent, EnvironmentSet environment, bool low)
        {
            var heap = new GameObject("OreHeap").transform;
            heap.SetParent(parent, false);
            heap.position = _heap;
            for (int i = 0; i < HeapChunks; i++)
            {
                float angle = CutsceneMath.Hash(i, 61) * Mathf.PI * 2f;
                float distance = Mathf.Sqrt(CutsceneMath.Hash(i, 63)) * HeapRadius;
                var spot = new Vector3(Mathf.Cos(angle) * distance, (1f - distance / HeapRadius) * 0.15f, Mathf.Sin(angle) * distance);
                Transform chunk = Chunk(heap, environment, i + 3, low, false);
                chunk.localPosition = spot;
                chunk.localScale *= ChunkSize / CarriedChunk * (0.7f + 0.6f * CutsceneMath.Hash(i, 65));
                chunk.gameObject.SetActive(true);
            }
        }

        // One chunk: a crystal every third, else a rock, about CarriedChunk metres across, hanging from its parent by its
        // top (<paramref name="hang"/>) or standing on it by its foot.
        private static Transform Chunk(Transform parent, EnvironmentSet environment, int index, bool low, bool hang)
        {
            PropKind kind = index % 3 == 0 ? PropKind.Crystal : PropKind.Rock;
            return CutsceneProps.Decor(parent, $"Chunk{index}", environment, kind, index, CarriedChunk, low, hang);
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
