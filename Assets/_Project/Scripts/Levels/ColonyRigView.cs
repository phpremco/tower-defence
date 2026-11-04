using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The colony's drilling rig beside the field: the derrick with its string sunk and spinning, and a few haulers
    /// flying an oval out from it and back, their cargo pods glowing on the way out (full of ore) and dim on the way
    /// back. Decoration only: no health, no collider, never a target, and nothing in the gameplay knows it is there.
    /// </summary>
    public sealed class ColonyRigView : MonoBehaviour
    {
        private const string RotorPrefix = "Rotor";
        private const string CargoSuffix = "_Cargo";
        private const float RotorDegreesPerSecond = 1440f;
        private const float EmptyCargoGlow = 0.15f;
        private const float BankDegrees = 10f;
        private const float BobCycles = 0.5f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private ColonyRigDefinition _rig;
        private ArtModel _drill;
        private ArtModel[] _haulers = new ArtModel[0];
        private Transform[] _holders = new Transform[0];
        private Transform[][] _rotors = new Transform[0][];
        private Quaternion[][] _rotorRest = new Quaternion[0][];
        private Material[] _cargo = new Material[0];
        private Color _cargoGlow;
        private float _clock;

        public void Initialize(ColonyRigDefinition rig)
        {
            _rig = rig;
            var drillRoot = new GameObject("Drill").transform;
            drillRoot.SetParent(transform, false);
            drillRoot.localScale = Vector3.one * rig.DrillScale;
            _drill = ArtModel.CreateSampled(drillRoot, rig.Drill, ownMaterial: false);
            // The string sinks once (the cutscene's drilling) and then only spins: the Idle clip does not move it back up.
            _drill.Sample(ModelClipRole.Deploy, _drill.LengthOf(ModelClipRole.Deploy), loop: false);
            BuildHaulers(rig);
        }

        private void BuildHaulers(ColonyRigDefinition rig)
        {
            int count = rig.Haulers;
            _haulers = new ArtModel[count];
            _holders = new Transform[count];
            _rotors = new Transform[count][];
            _rotorRest = new Quaternion[count][];
            _cargo = new Material[count];
            for (int i = 0; i < count; i++)
            {
                var holder = new GameObject($"Hauler{i}").transform;
                holder.SetParent(transform, false);
                holder.localScale = Vector3.one * rig.HaulerScale;
                _holders[i] = holder;
                _haulers[i] = ArtModel.CreateSampled(holder, rig.Hauler, ownMaterial: false);
                _rotors[i] = _haulers[i].Numbered(RotorPrefix);
                _rotorRest[i] = new Quaternion[_rotors[i].Length];
                for (int r = 0; r < _rotors[i].Length; r++)
                {
                    _rotorRest[i][r] = _rotors[i][r].localRotation;
                }

                _cargo[i] = CargoMaterial(_haulers[i]);
            }
        }

        // Each hauler's pod gets its own copy of the model material, so it can glow full or empty on its own.
        private Material CargoMaterial(ArtModel hauler)
        {
            Renderer[] pods = hauler.RenderersEndingWith(CargoSuffix);
            if (pods.Length == 0)
            {
                return null;
            }

            var cargo = new Material(hauler.Material) { name = hauler.Art.name + CargoSuffix };
            _cargoGlow = cargo.GetColor(EmissionColorId);
            foreach (Renderer pod in pods)
            {
                pod.sharedMaterial = cargo;
            }

            return cargo;
        }

        private void Update()
        {
            if (_rig == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            _clock += dt;
            _drill.Sample(ModelClipRole.Idle, _clock, loop: true);
            for (int i = 0; i < _haulers.Length; i++)
            {
                float lap = Mathf.Repeat(_clock / _rig.LapSeconds + i / (float)_haulers.Length, 1f);
                Fly(i, lap);
                if (_cargo[i] != null)
                {
                    _cargo[i].SetColor(EmissionColorId, _cargoGlow * (Loaded(lap) ? 1f : EmptyCargoGlow));
                }
            }
        }

        // An oval that starts over the rig's pad and swings out in front of it (towards the base) and back.
        private void Fly(int index, float lap)
        {
            float angle = lap * Mathf.PI * 2f;
            Vector2 radius = _rig.FlightRadius;
            var local = new Vector3(Mathf.Sin(angle) * radius.y, 0f, (1f - Mathf.Cos(angle)) * radius.x);
            var ahead = new Vector3(Mathf.Cos(angle) * radius.y, 0f, Mathf.Sin(angle) * radius.x);
            float bob = Mathf.Sin((lap * BobCycles + index * 0.37f) * Mathf.PI * 2f) * _rig.Bob;
            Transform holder = _holders[index];
            holder.localPosition = local + Vector3.up * (_rig.FlightHeight + bob);
            // Leaning into the oval's turn, never more than a few degrees.
            holder.localRotation = Quaternion.LookRotation(ahead.normalized) * Quaternion.Euler(0f, 0f, -BankDegrees);

            ArtModel hauler = _haulers[index];
            if (!hauler.Sample(ModelClipRole.Move, _clock, loop: true))
            {
                hauler.Sample(ModelClipRole.Idle, _clock, loop: true);
            }

            float turn = _clock * RotorDegreesPerSecond;
            Transform[] rotors = _rotors[index];
            for (int r = 0; r < rotors.Length; r++)
            {
                rotors[r].localRotation = _rotorRest[index][r] * Quaternion.AngleAxis(r % 2 == 0 ? turn : -turn, Vector3.up);
            }
        }

        // Loaded from the pad out to the far end of the oval, empty on the way back.
        private static bool Loaded(float lap) => lap < 0.5f;

        private void OnDestroy()
        {
            _drill?.Dispose();
            foreach (ArtModel hauler in _haulers)
            {
                hauler?.Dispose();
            }

            foreach (Material cargo in _cargo)
            {
                if (cargo != null)
                {
                    Destroy(cargo);
                }
            }
        }
    }
}
