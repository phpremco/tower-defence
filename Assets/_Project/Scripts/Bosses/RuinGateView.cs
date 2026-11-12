using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The sealed ruin gate a giant breaks out of (README round 29, RuinGate): it breathes shut (Closed_Idle) until the boss
    /// is due, then opens once (Open: the seal knot cracks at frame 30 and its shards fly, the seams light at 38, the petals
    /// fold back from 52 and stand open at 130) and sways open from then on. Its glyphs glow low while it is shut, flare as
    /// the light comes through and settle. Posed by sampling, so a paused editor shows the moment itself. Decor only.
    /// </summary>
    public sealed class RuinGateView : MonoBehaviour
    {
        private const float FramesPerSecond = 30f;
        public const float SealCrackSeconds = 30f / FramesPerSecond;
        public const float FirstLightSeconds = 38f / FramesPerSecond;
        public const float FullyOpenSeconds = 130f / FramesPerSecond;
        private const float ClosedGlow = 0.6f;
        private const float FlareGlow = 2.5f;
        private const float OpenGlow = 1.6f;
        private const float LightIntensity = 6f;
        private const float LightRange = 7f;
        private const float LightHeight = 1.8f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private ArtModel _model;
        private Material _material;
        private Color _glow;
        private Light _light;
        private Color _lightColour;
        private float _clock;
        private float _opened = -1f;

        /// <summary>The gate on the ground at a point, facing the way the giant will walk out; null without the art.</summary>
        public static RuinGateView Create(Transform parent, Vector3 at, Vector3 forward, Color light)
        {
            BossFxArt art = BossFxArt.Active;
            if (art == null || art.RuinGate == null)
            {
                return null;
            }

            var go = new GameObject("RuinGate");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.LookRotation(BossGeometry.Flat(forward), Vector3.up);
            go.transform.localScale = Vector3.one * art.RuinGateScale;
            var view = go.AddComponent<RuinGateView>();
            view._model = ArtModel.CreateSampled(go.transform, art.RuinGate, ownMaterial: true);
            view.FootprintHalfSize = Footprint(go.transform, art.RuinGateScale);
            view._material = view._model.Material;
            view._glow = view._material.GetColor(EmissionColorId);
            var lightObject = new GameObject("OpeningLight");
            lightObject.transform.SetParent(go.transform, false);
            lightObject.transform.localPosition = Vector3.up * LightHeight;
            view._light = lightObject.AddComponent<Light>();
            view._light.type = LightType.Point;
            view._light.range = LightRange;
            view._light.shadows = LightShadows.None;
            view._light.intensity = 0f;
            view._lightColour = light;
            view._light.color = light;
            view.Pose();
            return view;
        }

        /// <summary>
        /// Half the gate's size on the ground in world metres: x across the opening, y through it (its plinth's footprint),
        /// so the decoration standing there can be cleared before it rises.
        /// </summary>
        public Vector2 FootprintHalfSize { get; private set; }

        // The model's mesh bounds (its rest pose, petals shut) in the gate's own frame, so the turn on the field does not widen it.
        private static Vector2 Footprint(Transform gate, float scale)
        {
            Matrix4x4 toGate = gate.worldToLocalMatrix;
            float x = 0f;
            float z = 0f;
            foreach (SkinnedMeshRenderer skinned in gate.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh == null)
                {
                    continue;
                }

                Matrix4x4 toLocal = toGate * skinned.transform.localToWorldMatrix;
                Bounds bounds = skinned.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    Vector3 local = toLocal.MultiplyPoint3x4(point);
                    x = Mathf.Max(x, Mathf.Abs(local.x));
                    z = Mathf.Max(z, Mathf.Abs(local.z));
                }
            }

            return new Vector2(x, z) * scale;
        }

        /// <summary>Seconds since it began to open (negative while it is still shut).</summary>
        public float Opened => _opened;

        /// <summary>Where the seal knot sits, for the shard burst at its crack.</summary>
        public Vector3 SealPosition
        {
            get
            {
                Transform seal = _model.Find("Seal");
                return seal != null ? seal.position : transform.position + Vector3.up * LightHeight;
            }
        }

        public void Open()
        {
            if (_opened < 0f)
            {
                _opened = 0f;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _clock += dt;
            if (_opened >= 0f)
            {
                _opened += dt;
            }

            Pose();
        }

        private void Pose()
        {
            float glow;
            float light = 0f;
            if (_opened < 0f)
            {
                _model.Sample(ModelClipRole.Closed, _clock, loop: true);
                glow = ClosedGlow;
            }
            else if (_opened < _model.LengthOf(ModelClipRole.Open))
            {
                _model.Sample(ModelClipRole.Open, _opened, loop: false);
                float flare = Mathf.InverseLerp(FirstLightSeconds, FullyOpenSeconds, _opened);
                glow = _opened < FirstLightSeconds ? ClosedGlow : Mathf.Lerp(FlareGlow, OpenGlow, flare * flare);
                light = _opened < FirstLightSeconds ? 0f : Mathf.Lerp(1f, 0.35f, flare);
            }
            else
            {
                _model.Sample(ModelClipRole.Idle, _opened, loop: true);
                glow = OpenGlow;
                light = 0.35f;
            }

            _material.SetColor(EmissionColorId, _glow * glow);
            _light.intensity = LightIntensity * light;
        }

        private void OnDestroy()
        {
            _model?.Dispose();
        }
    }
}
