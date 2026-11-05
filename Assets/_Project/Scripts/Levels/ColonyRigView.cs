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
        private const float FleeSpeed = 6f;
        private const float FleeClimb = 0.45f;
        private const float FleeSpreadDegrees = 40f;
        private const float FleeSeconds = 4f;
        private const float CrashSeconds = 1.1f;
        private const float CrashTumbleDegrees = 220f;
        private const float CrashRestHeight = 0.15f;
        private const float CrashSinkSeconds = 6f;
        private const float CrashSinkDepth = 0.6f;
        private const float CrashSinkStart = 0.6f;
        private const float CrashDrift = 1.5f;
        private const float CrashPitchDegrees = 35f;
        private const float CrashRollShare = 0.25f;
        private const float FleeRampSeconds = 0.6f;
        private const float FleePitchDegrees = 12f;
        private const float FleeTurnRate = 4f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private ColonyRigDefinition _rig;
        private ArtModel _drill;
        private ArtModel _wreck;
        private ArtModel[] _haulers = new ArtModel[0];
        private Transform[] _holders = new Transform[0];
        private Transform[][] _rotors = new Transform[0][];
        private Quaternion[][] _rotorRest = new Quaternion[0][];
        private Material[] _cargo = new Material[0];
        private Vector3[] _flee = new Vector3[0];
        private Vector3[] _crashFrom = new Vector3[0];
        private Color _cargoGlow;
        private float _clock;
        private Transform _drillRoot;
        private float _smashAge = -1f;
        private int _crasher = -1;

        /// <summary>True once a boss has broken out under the rig and wrecked the derrick.</summary>
        public bool Smashed => _smashAge >= 0f;

        /// <summary>Seconds into the wreck's collapse clip when its top hits the ground, or zero without such a clip.</summary>
        public float CollapseMoment
        {
            get
            {
                ModelArtDefinition wreck = _rig != null ? _rig.Wreck : null;
                int index = wreck != null ? wreck.IndexOf(ModelClipRole.Death) : -1;
                return index >= 0 ? wreck.Clips[index].EventSeconds : 0f;
            }
        }

        /// <summary>
        /// A boss breaks out under the rig (Gravemaw's entrance, a story beat). The caller hides the moment in a dust and
        /// spark burst; under it the intact derrick is swapped for the wreck at once (no toppling of the whole model), its
        /// collapse clip (the wreck's Death role) playing when the art has one. The string stops; the hauler nearest the
        /// break-out crashes beside the rig and the others scatter away from it and are gone: nothing hauls to a wreck.
        /// </summary>
        public void Smash(Vector3 from)
        {
            if (Smashed)
            {
                return;
            }

            _smashAge = 0f;
            ShowWreck(from);
            ScatterHaulers(from);
        }

        public void Initialize(ColonyRigDefinition rig)
        {
            _rig = rig;
            var drillRoot = new GameObject("Drill").transform;
            _drillRoot = drillRoot;
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
            _flee = new Vector3[count];
            _crashFrom = new Vector3[count];
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
            if (Smashed)
            {
                _smashAge += dt;
                _wreck?.Sample(ModelClipRole.Death, _smashAge, loop: false);
                for (int i = 0; i < _holders.Length; i++)
                {
                    if (i == _crasher)
                    {
                        Crash(i);
                    }
                    else
                    {
                        Flee(i, dt);
                    }
                }

                return;
            }

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

        // README round 29/31: the wreck takes the drill's place in the dust, its fallen top turned away from the hole the
        // boss came up through. Its collapse clip (round 31) starts in the standing rig and breaks it apart into the wreck
        // pose; without a clip the wreck simply stands there. Without the art at all the derrick only goes dark and still:
        // a tipping intact model reads as a toy falling over.
        private void ShowWreck(Vector3 from)
        {
            if (_rig.Wreck == null)
            {
                return;
            }

            var holder = new GameObject("DrillWreck").transform;
            holder.SetParent(transform, false);
            holder.localScale = Vector3.one * _rig.DrillScale;
            Vector3 away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f)
            {
                // The model folds over its +X legs: turn its +X away from the hole.
                holder.rotation = Quaternion.FromToRotation(Vector3.right, away.normalized);
            }

            _wreck = ArtModel.CreateSampled(holder, _rig.Wreck, ownMaterial: false);
            _wreck.Sample(ModelClipRole.Death, 0f, loop: false);
            _drillRoot.gameObject.SetActive(false);
        }

        // The nearest hauler is caught in the blast and comes down; the rest fly off away from the hole, fanned out.
        private void ScatterHaulers(Vector3 from)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < _holders.Length; i++)
            {
                Vector3 at = _holders[i].position;
                Vector3 away = at - from;
                away.y = 0f;
                away = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
                float spread = (_holders.Length > 1 ? i / (_holders.Length - 1f) - 0.5f : 0f) * 2f * FleeSpreadDegrees;
                _flee[i] = Quaternion.Euler(0f, spread, 0f) * away;
                _crashFrom[i] = _holders[i].localPosition;
                float distance = (at - from).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    _crasher = i;
                }

                if (_cargo[i] != null)
                {
                    _cargo[i].SetColor(EmissionColorId, _cargoGlow * EmptyCargoGlow);
                }
            }

            // One hauler alone would only crash: with a single one it flies off instead, the rig never looks still in use.
            if (_holders.Length < 2)
            {
                _crasher = -1;
            }
        }

        private void Flee(int index, float dt)
        {
            Transform holder = _holders[index];
            if (!holder.gameObject.activeSelf)
            {
                return;
            }

            float ramp = Mathf.Clamp01(_smashAge / FleeRampSeconds);
            Vector3 heading = _flee[index] + Vector3.up * FleeClimb;
            holder.position += heading * (FleeSpeed * ramp * dt);
            holder.rotation = Quaternion.Slerp(holder.rotation, Quaternion.LookRotation(_flee[index]) * Quaternion.Euler(FleePitchDegrees, 0f, 0f), dt * FleeTurnRate);
            SpinRotors(index);
            if (_smashAge > FleeSeconds)
            {
                holder.gameObject.SetActive(false);
            }
        }

        // Down in a tumble beside the rig, its rotors dead, then it sinks into the dust and is gone.
        private void Crash(int index)
        {
            Transform holder = _holders[index];
            if (!holder.gameObject.activeSelf)
            {
                return;
            }

            float fall = Mathf.Clamp01(_smashAge / CrashSeconds);
            float eased = fall * fall;
            Vector3 start = _crashFrom[index];
            Vector3 drift = transform.InverseTransformDirection(_flee[index]) * (eased * CrashDrift);
            float height = Mathf.Lerp(start.y, CrashRestHeight, eased);
            float sink = Mathf.Clamp01((_smashAge - CrashSinkSeconds * CrashSinkStart) / (CrashSinkSeconds * (1f - CrashSinkStart))) * CrashSinkDepth;
            holder.localPosition = new Vector3(start.x + drift.x, height - sink, start.z + drift.z);
            holder.localRotation = Quaternion.Euler(eased * CrashPitchDegrees, 0f, eased * CrashTumbleDegrees * CrashRollShare) * Quaternion.Euler(0f, eased * CrashTumbleDegrees, 0f);
            if (_smashAge > CrashSinkSeconds)
            {
                holder.gameObject.SetActive(false);
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
            SpinRotors(index);
        }

        private void SpinRotors(int index)
        {
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
            _wreck?.Dispose();
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
