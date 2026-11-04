using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The drill on its deposit: the rig unfolds out of the ground, the drill string spins and sinks through the
    /// platform, a ring of dust churns at its foot and the ground around it cracks open with a warm glow from below.
    /// A deep rumble loop follows the drilling. Posed from progress and time alone (dust excepted).
    /// </summary>
    public sealed class FieldDrill
    {
        private const float MaxDepth = 2.4f;
        private const float SpinDegreesPerSecond = 540f;
        private const float BitTop = 0.2f;
        private const float DustRate = 26f;
        private const float RumbleShare = 0.6f;
        private const int Cracks = 6;
        private const float CrackLength = 2.6f;
        private const float CrackWidth = 0.22f;
        private const float CrackGlow = 1.6f;
        private const float DepositGlow = 0.5f;
        private static readonly Color DustColour = new Color(0.5f, 0.5f, 0.52f, 0.45f);

        private readonly Transform _rig;
        private readonly Transform _bit;
        private readonly float _bitLength;
        private readonly ParticleSystem _dust;
        private readonly Transform[] _cracks;
        private readonly Material _crackMaterial;
        private readonly Color _crackColour;
        private readonly GameObject _deposit;
        private readonly AudioSource _rumble;
        private readonly CutsceneAudio _audio;
        private readonly ArtModel _art;

        public FieldDrill(Transform parent, FieldLayout layout, CutsceneContext context)
        {
            VisualTheme theme = layout.Level.Theme;
            _audio = context.Audio;
            Root = new GameObject("Drill").transform;
            Root.SetParent(parent, false);
            Root.SetPositionAndRotation(layout.Drill, layout.DrillFacing);
            _crackColour = Color.Lerp(theme.AccentColor, new Color(1f, 0.55f, 0.2f), 0.5f);

            GameObject custom = context.Cast != null ? context.Cast.Drill : null;
            DrillModelFactory.Parts parts = DrillModelFactory.Create();
            Material[] materials = ModelPalette.Create(theme.BaseColor);
            _rig = new GameObject("Rig").transform;
            _rig.SetParent(Root, false);
            ModelArtDefinition art = context.Cast != null ? context.Cast.DrillArt : null;
            if (art != null)
            {
                // The imported derrick has its own drill string: its Sink clip drives it down, then Spin turns it.
                _art = ArtModel.CreateSampled(_rig, art, ownMaterial: false);
            }
            else if (custom != null)
            {
                Object.Instantiate(custom, _rig);
            }
            else
            {
                _rig.gameObject.AddComponent<MeshFilter>().sharedMesh = parts.Rig;
                _rig.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            }

            _bit = new GameObject("Bit").transform;
            _bit.SetParent(Root, false);
            _bit.gameObject.AddComponent<MeshFilter>().sharedMesh = parts.Bit;
            _bit.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            _bitLength = parts.BitLength;

            _deposit = GlowDecal.PlacePool(Root, _crackColour * DepositGlow, 2.4f);
            _crackMaterial = new Material(GlowDecal.RadialMaterial(_crackColour * CrackGlow));
            _cracks = new Transform[Cracks];
            for (int i = 0; i < Cracks; i++)
            {
                var crack = new GameObject($"Crack{i}");
                crack.transform.SetParent(Root, false);
                crack.transform.localPosition = Vector3.up * 0.04f;
                crack.transform.localRotation = Quaternion.Euler(90f, i * (360f / Cracks) + CutsceneMath.Hash(i, 3) * 25f, 0f);
                crack.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
                var renderer = crack.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _crackMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                _cracks[i] = crack.transform;
            }

            _dust = ParticleKit.Create(Root, "Dust", Vector3.up * 0.3f, DustColour, 1.2f, 2.4f, 0.4f, 0.9f, 0.8f, 2.2f, -0.04f, context.Scaled(160), 1.3f);
            _rumble = context.Audio.CreateLoop("DrillRumble", CutsceneSound.Rumble);
        }

        public Transform Root { get; }

        /// <summary>Where the haulers pick up: just above the drill's platform.</summary>
        public Vector3 Pickup => Root.position + Root.rotation * new Vector3(-1.3f, 1.2f, 0.3f);

        /// <summary>
        /// <paramref name="build"/>: the rig out of the ground (0..1). <paramref name="depth"/>: how far the string has
        /// sunk (0..1). <paramref name="crack"/>: how far the ground has cracked open (0..1).
        /// </summary>
        public void Pose(float build, float depth, float crack, float time, bool loud)
        {
            Root.gameObject.SetActive(build > 0f);
            if (build <= 0f)
            {
                _audio.SetLoopVolume(_rumble, 0f);
                return;
            }

            float grow = CutsceneMath.OutBack(build);
            _rig.localScale = new Vector3(1f, Mathf.Max(0.01f, grow), 1f);
            _bit.gameObject.SetActive(build >= 1f && _art == null);
            if (_art != null)
            {
                PoseArt(depth, time);
            }

            _bit.localPosition = Vector3.up * (_bitLength + BitTop - depth * MaxDepth);
            _bit.localRotation = Quaternion.Euler(0f, time * SpinDegreesPerSecond * Mathf.Min(1f, depth * 4f), 0f);

            for (int i = 0; i < _cracks.Length; i++)
            {
                float open = CutsceneMath.Span(crack, i * 0.08f, 0.5f + i * 0.08f);
                float length = CrackLength * (0.6f + 0.4f * CutsceneMath.Hash(i, 9)) * open;
                _cracks[i].localScale = new Vector3(Mathf.Max(0.001f, CrackWidth * open), Mathf.Max(0.001f, length), 1f);
                _cracks[i].gameObject.SetActive(open > 0f);
                _cracks[i].localPosition = Vector3.up * 0.04f;
            }

            float flicker = 0.8f + 0.2f * Mathf.Sin(time * 7f);
            _crackMaterial.SetColor("_EmissionColor", _crackColour * (CrackGlow * flicker));

            _deposit.SetActive(crack > 0f);
            bool drilling = depth > 0f;
            ParticleKit.SetRate(_dust, drilling ? DustRate * (loud ? 1f : 0.35f) : 0f);
            _audio.SetLoopVolume(_rumble, drilling ? RumbleShare * (loud ? 1f : 0.3f) : 0f);
        }

        private void PoseArt(float depth, float time)
        {
            if (depth < 1f)
            {
                _art.Sample(ModelClipRole.Deploy, depth * _art.LengthOf(ModelClipRole.Deploy), loop: false);
                return;
            }

            _art.Sample(ModelClipRole.Idle, time, loop: true);
        }

        public void Silence() => _audio.SetLoopVolume(_rumble, 0f);
    }
}
