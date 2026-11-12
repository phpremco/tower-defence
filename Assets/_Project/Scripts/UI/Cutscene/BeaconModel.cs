using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The survey beacon the commander plants by the crystals: a tripod with a mast that telescopes up, a lamp that
    /// pulses in the colony's colour, a slowly turning ring and a point light that throws the pulse on the ground.
    /// Posed from its deploy share and the time alone.
    /// </summary>
    public sealed class BeaconModel
    {
        private const float MastHeight = 1.1f;
        private const float LampEmission = 4f;
        private const float LightIntensity = 2.6f;
        private const float LightRange = 4.5f;
        private const float PulseSpeed = 3.2f;
        private const float RingSpin = 70f;
        private const float PoolRadius = 1.4f;
        private const string LampSocket = "Lamp";
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Transform _mast;
        private readonly Transform _ring;
        private readonly Transform _legs;
        private readonly Light _light;
        private readonly Material _lamp;
        private readonly Color _colour;
        private readonly GameObject _pool;
        private readonly ArtModel _art;
        private readonly Color _artGlow;

        /// <param name="custom">A finished model that replaces the procedural one; the light, its pulse and the glow pool stay.</param>
        /// <param name="art">The imported model, which wins over <paramref name="custom"/>: it unfolds by its Deploy clip and idles, with
        /// the light on its lamp.</param>
        public BeaconModel(Transform parent, Color colour, GameObject custom = null, ModelArtDefinition art = null)
        {
            _colour = colour;
            Root = new GameObject("Beacon").transform;
            Root.SetParent(parent, false);
            Material[] materials = ModelPalette.Create(colour);
            _lamp = SurfaceMaterials.Create(colour, LampEmission, 0.6f);

            var legs = new PropMeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                float angle = i / 3f * Mathf.PI * 2f;
                var around = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                legs.AddTube(new[] { Vector3.up * 0.42f, around * 0.42f + Vector3.up * 0.02f }, new[] { 0.03f, 0.022f }, 5, ModelPalette.Metal, false);
                legs.AddBox(around * 0.44f + Vector3.up * 0.02f, new Vector3(0.08f, 0.03f, 0.08f), ModelPalette.Dark, -angle * Mathf.Rad2Deg);
            }

            legs.AddBox(Vector3.up * 0.42f, new Vector3(0.2f, 0.14f, 0.2f), ModelPalette.Body);
            legs.AddBox(new Vector3(0f, 0.42f, 0.105f), new Vector3(0.1f, 0.04f, 0.01f), ModelPalette.Glow);
            _legs = Part("Tripod", Root, legs.ToMesh("BeaconTripod"), materials);

            var mast = new PropMeshBuilder();
            mast.AddTube(new[] { Vector3.zero, Vector3.up * MastHeight }, new[] { 0.035f, 0.025f }, 6, ModelPalette.Metal, false);
            mast.AddBox(Vector3.up * (MastHeight * 0.55f), new Vector3(0.08f, 0.16f, 0.08f), ModelPalette.Body);
            _mast = Part("Mast", _legs, mast.ToMesh("BeaconMast"), materials);
            _mast.localPosition = Vector3.up * 0.42f;

            var lampMesh = new PropMeshBuilder(1);
            lampMesh.AddBlob(Vector3.zero, new Vector3(0.09f, 0.12f, 0.09f), 4, 8, 0f, 0, 0, false);
            Transform lamp = Part("Lamp", _mast, lampMesh.ToMesh("BeaconLamp"), new[] { _lamp });
            lamp.localPosition = Vector3.up * (MastHeight + 0.08f);
            lamp.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var ring = new PropMeshBuilder();
            ring.AddTorus(Vector3.zero, Vector3.up, 0.2f, 0.018f, 20, 5, ModelPalette.Glow);
            ring.AddBox(new Vector3(0.2f, 0f, 0f), new Vector3(0.05f, 0.05f, 0.05f), ModelPalette.Metal);
            _ring = Part("Ring", _mast, ring.ToMesh("BeaconRing"), materials);
            _ring.localPosition = Vector3.up * (MastHeight - 0.12f);

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(lamp, false);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = colour;
            _light.range = LightRange;
            _light.shadows = LightShadows.None;
            _pool = GlowDecal.PlacePool(Root, colour * 0.25f, PoolRadius);
            if (art != null)
            {
                _art = ArtModel.CreateSampled(Root, art, ownMaterial: true);
                _artGlow = _art.Material.GetColor(EmissionColorId);
                Transform lampSocket = _art.Find(LampSocket);
                if (lampSocket != null)
                {
                    lightGo.transform.SetParent(lampSocket, false);
                }
            }
            else if (custom != null)
            {
                Object.Instantiate(custom, Root);
            }

            if (art != null || custom != null)
            {
                foreach (MeshRenderer part in new[] { _legs.GetComponent<MeshRenderer>(), _mast.GetComponent<MeshRenderer>(), lamp.GetComponent<MeshRenderer>(), _ring.GetComponent<MeshRenderer>() })
                {
                    part.enabled = false;
                }
            }
        }

        public Transform Root { get; }

        /// <summary>
        /// Unfolds the beacon (0 folded and dark, 1 standing and lit) and pulses its lamp; <paramref name="stood"/> is how long it has
        /// stood unfolded (round 48: its idle blends in from the unfolding's last frame over the first moment).
        /// </summary>
        public void Pose(float deploy, float time, float stood = float.PositiveInfinity)
        {
            deploy = Mathf.Clamp01(deploy);
            float legs = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(deploy * 2f));
            float mast = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(deploy * 2f - 1f));
            _legs.localScale = new Vector3(1f, Mathf.Max(0.05f, legs), 1f);
            _mast.localScale = new Vector3(1f, Mathf.Max(0.05f, mast), 1f);
            _ring.localRotation = Quaternion.Euler(0f, time * RingSpin, 0f);
            float pulse = 0.55f + 0.45f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(time * PulseSpeed), 3f);
            float lit = mast * pulse;
            _light.intensity = LightIntensity * lit;
            _lamp.SetColor("_EmissionColor", _colour * (LampEmission * Mathf.Max(0.05f, lit)));
            _pool.SetActive(mast > 0.5f);
            if (_art != null)
            {
                PoseArt(deploy, time, lit, stood);
            }
        }

        // Unfolding follows the Deploy clip over the deploy share, then the idle loops; the lamp's glow pulses with the light.
        private void PoseArt(float deploy, float time, float lit, float stood)
        {
            // Folded in the commander's hands until he plants it.
            _art.SetActive(deploy > 0f);
            if (deploy < 1f)
            {
                _art.Sample(ModelClipRole.Deploy, deploy * _art.LengthOf(ModelClipRole.Deploy), loop: false);
            }
            else
            {
                float share = CutsceneMath.Fade(stood, ClipMixer.DefaultCycleBlendSeconds);
                if (share < 1f)
                {
                    _art.Sample(ModelClipRole.Deploy, _art.LengthOf(ModelClipRole.Deploy), loop: false);
                }

                _art.SampleOver(ModelClipRole.Idle, time, true, share);
            }

            _art.Material.SetColor(EmissionColorId, _artGlow * Mathf.Max(0.15f, lit));
        }

        private static Transform Part(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }
    }
}
