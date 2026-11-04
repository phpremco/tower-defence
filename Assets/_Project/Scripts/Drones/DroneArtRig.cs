using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// An imported (Blender) drone model under a holder the view turns and tilts: the rotor bones it spins, the sockets its
    /// beam, shots and thruster trail leave from, the cargo pod that lights up when loaded, and its Idle, Fly and Work
    /// cycles. Plain class; <see cref="DroneView"/> drives it, menus use a still.
    /// </summary>
    public sealed class DroneArtRig
    {
        private const string RotorPrefix = "Rotor";
        private const string MuzzlePrefix = "Muzzle";
        private const string BeamSocket = "Beam";
        private const string MineSocket = "Mine";
        private const string ThrusterSocket = "Thruster";
        private const string CargoSuffix = "_Cargo";

        // An empty pod keeps a faint glow so it still reads as a pod.
        private const float CargoEmptyGlow = 0.15f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Quaternion[] _rotorRest;
        private readonly int _idle;
        private readonly int _move;
        private readonly int _work;
        private readonly Material _cargo;
        private readonly Color _cargoGlow;
        private int _cycle = -1;
        private bool _loaded = true;

        private DroneArtRig(Transform holder, ArtModel model, bool live)
        {
            Holder = holder;
            Model = model;
            Rotors = model.Numbered(RotorPrefix);
            _rotorRest = new Quaternion[Rotors.Length];
            for (int i = 0; i < Rotors.Length; i++)
            {
                _rotorRest[i] = Rotors[i].localRotation;
            }

            Muzzles = model.Numbered(MuzzlePrefix);
            Tool = model.Find(BeamSocket) ?? model.Find(MineSocket);
            Thruster = model.Find(ThrusterSocket);
            Cargo = model.RenderersEndingWith(CargoSuffix);
            ModelArtDefinition art = model.Art;
            _idle = art.IndexOf(ModelClipRole.Idle);
            _move = art.IndexOf(ModelClipRole.Move);
            _work = art.IndexOf(ModelClipRole.Work);
            if (live && Cargo.Length > 0)
            {
                // The pod's own copy of the material, so its glow follows the load without touching the hull.
                _cargo = new Material(model.Material) { name = model.Material.name + CargoSuffix };
                _cargoGlow = _cargo.GetColor(EmissionColorId);
                foreach (Renderer renderer in Cargo)
                {
                    renderer.sharedMaterial = _cargo;
                }

                SetLoaded(false);
            }
        }

        /// <summary>What the view turns, tilts and bobs; the model sits under it facing its +Z.</summary>
        public Transform Holder { get; }

        public ArtModel Model { get; }

        /// <summary>The rotor bones; they spin about their own up axis.</summary>
        public Transform[] Rotors { get; }

        /// <summary>The gun mouths, in order (the attack drone).</summary>
        public Transform[] Muzzles { get; }

        /// <summary>Where the work beam leaves: the repair emitter or the drill tip, or null.</summary>
        public Transform Tool { get; }

        /// <summary>The exhaust a trail streams from, or null.</summary>
        public Transform Thruster { get; }

        /// <summary>The cargo pod's meshes, possibly none.</summary>
        public Renderer[] Cargo { get; }

        /// <summary>A live drone: its own animated model under a new holder.</summary>
        public static DroneArtRig Create(Transform parent, ModelArtDefinition art)
        {
            Transform holder = NewHolder(parent);
            return new DroneArtRig(holder, ArtModel.Create(holder, art, ownMaterial: false), live: true);
        }

        /// <summary>A still for menus and icons.</summary>
        public static DroneArtRig CreateStill(Transform parent, ModelArtDefinition art)
        {
            Transform holder = NewHolder(parent);
            return new DroneArtRig(holder, ArtModel.CreateStill(holder, art), live: false);
        }

        /// <summary>Picks the cycle: work while at the job, fly while moving, else idle.</summary>
        public void Animate(bool working, bool moving, float deltaTime)
        {
            ModelClipPlayer player = Model.Player;
            if (player == null)
            {
                return;
            }

            int wanted = working && _work >= 0 ? _work : moving && _move >= 0 ? _move : _idle;
            if (wanted != _cycle)
            {
                _cycle = wanted;
                player.Loop(wanted, 1f);
            }

            player.Tick(deltaTime);
        }

        /// <summary>Spins every rotor to an angle about its own axis, over whatever the clip did to it this frame.</summary>
        public void SpinRotors(float degrees)
        {
            for (int i = 0; i < Rotors.Length; i++)
            {
                // Neighbouring rotors turn opposite ways, like a real multirotor.
                float turn = i % 2 == 0 ? degrees : -degrees;
                Rotors[i].localRotation = _rotorRest[i] * Quaternion.AngleAxis(turn, Vector3.up);
            }
        }

        /// <summary>A loaded pod glows; an empty one only shows a hint.</summary>
        public void SetLoaded(bool loaded)
        {
            if (_cargo == null || loaded == _loaded)
            {
                return;
            }

            _loaded = loaded;
            _cargo.SetColor(EmissionColorId, loaded ? _cargoGlow : _cargoGlow * CargoEmptyGlow);
        }

        public void Dispose()
        {
            Model.Dispose();
            if (_cargo != null)
            {
                Object.Destroy(_cargo);
            }
        }

        private static Transform NewHolder(Transform parent)
        {
            var holder = new GameObject("DroneModel").transform;
            holder.SetParent(parent, false);
            return holder;
        }
    }
}
