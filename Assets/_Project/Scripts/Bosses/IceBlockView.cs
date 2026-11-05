using Game.Combat;
using Game.Core;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The ice block a frozen gate becomes (Docs/BOSSES.md, Frost Warden). With the round-29 art it is the painted block
    /// (clear ice with milky fractures, the gate's parts trapped inside) on the gate's own transform, sized to the gate it
    /// holds, with rime under it; it snaps out of the breath (scale 0.9 to 1), holds, melts away on a thaw or bursts into
    /// shards when the gate shatters. Without the art it is a box in the level's ice material. It follows the gate's own
    /// events, so whatever ends the freeze, the block goes with it.
    /// </summary>
    public sealed class IceBlockView : MonoBehaviour
    {
        private const float GrowSeconds = 0.3f;
        private const float BoxGrowSeconds = 0.5f;
        private const float MeltSeconds = 0.8f;
        private const float Height = 1.5f;
        private const float Depth = 1.1f;
        private const float SpanMargin = 1.1f;
        private const float ShardRadius = 1.6f;
        private const float ShatterRadius = 2.4f;
        private const float StartScale = 0.9f;
        // The block's height over the gate's: the export encloses the tallest gate with a little ice above it.
        private const float HeightOverGate = 1.08f;
        private const float MinScale = 0.55f;
        private const float MaxScale = 1.6f;
        // The block's export width (README round 29: 3.65 m).
        private const float ModelWidth = 3.65f;
        private const float RimeSize = 4.2f;
        private static readonly Color Shards = new Color(0.8f, 0.95f, 1f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Tower _gate;
        private Transform _block;
        private ProjectilePool _projectiles;
        private FrostCreepView _frost;
        private int _rime = -1;
        private Vector3 _full;
        private bool _painted;
        private Renderer[] _renderers = new Renderer[0];
        private MaterialPropertyBlock _props;
        private float _age;
        private float _melt = -1f;

        /// <summary>Freezes the gate's look into a block, oriented across the road.</summary>
        public static IceBlockView Create(Transform parent, Tower gate, Vector3 along, Material ice, ProjectilePool projectiles, FrostCreepView frost = null)
        {
            var go = new GameObject("IceBlock");
            var view = go.AddComponent<IceBlockView>();
            view._gate = gate;
            view._projectiles = projectiles;
            view._frost = frost;
            view._props = new MaterialPropertyBlock();
            BossFxArt art = BossFxArt.Active;
            if (art != null && art.IceBlock != null)
            {
                view.BuildPainted(gate, art.IceBlock);
            }
            else
            {
                go.transform.SetParent(parent, false);
                go.transform.position = gate.Position;
                go.transform.rotation = Quaternion.LookRotation(along.sqrMagnitude > 0.0001f ? along : Vector3.forward, Vector3.up);
                view.BuildBox(gate, ice);
            }

            if (frost != null)
            {
                view._rime = frost.Hold(gate.Position, RimeSize);
            }

            gate.FrozenChanged += view.OnFrozenChanged;
            gate.Smashed += view.OnSmashed;
            return view;
        }

        // The painted block under the gate itself, as tall as the gate it holds.
        private void BuildPainted(Tower gate, ModelArtDefinition art)
        {
            Bounds gateBounds = GateBounds(gate);
            transform.SetParent(gate.transform, false);
            var holder = new GameObject("Ice").transform;
            holder.SetParent(transform, false);
            ArtModel model = ArtModel.CreateStill(holder, art);
            _renderers = model.Renderers;
            foreach (Renderer renderer in _renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // As tall as the gate and wider than it, centred on it (the export encloses the tallest gate from its own pivot).
            float tall = (gateBounds.max.y - gate.Position.y) * HeightOverGate / Mathf.Max(0.1f, art.Height * art.Scale);
            float wide = Mathf.Max(gateBounds.size.x, gateBounds.size.z) * HeightOverGate / ModelWidth;
            float scale = Mathf.Clamp(Mathf.Max(tall, wide), MinScale, MaxScale);
            _full = Vector3.one * scale;
            _block = holder;
            _block.localScale = _full;
            Bounds ice = new Bounds(holder.position, Vector3.zero);
            foreach (Renderer renderer in _renderers)
            {
                ice.Encapsulate(renderer.bounds);
            }

            Vector3 shift = gateBounds.center - ice.center;
            holder.position += new Vector3(shift.x, 0f, shift.z);
            _block.localScale = _full * StartScale;
            _painted = true;
        }

        private void BuildBox(Tower gate, Material ice)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(block.GetComponent<Collider>());
            block.name = "Ice";
            block.transform.SetParent(transform, false);
            _full = new Vector3(gate.Span * SpanMargin, Height, Depth);
            block.transform.localPosition = Vector3.up * (Height * 0.5f);
            block.transform.localScale = new Vector3(_full.x, 0.01f, _full.z);
            var renderer = block.GetComponent<MeshRenderer>();
            if (ice != null)
            {
                renderer.sharedMaterial = ice;
            }

            _block = block.transform;
        }

        // The gate's model in the world (its mesh renderers), before the block is there.
        private static Bounds GateBounds(Tower gate)
        {
            var bounds = new Bounds(gate.Position + Vector3.up * 0.5f, Vector3.one * 0.5f);
            foreach (Renderer renderer in gate.GetComponentsInChildren<Renderer>())
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private void OnDestroy()
        {
            if (_gate != null)
            {
                _gate.FrozenChanged -= OnFrozenChanged;
                _gate.Smashed -= OnSmashed;
            }

            _frost?.Release(_rime);
        }

        // A thaw (the hero's repair, or the freeze running out without a boss to break it) melts the block away.
        private void OnFrozenChanged(Tower gate, bool frozen)
        {
            if (!frozen && _melt < 0f)
            {
                _melt = 0f;
            }
        }

        private void OnSmashed(Tower gate, Vector3 from)
        {
            Vector3 centre = gate.Position + Vector3.up * 0.6f;
            if (_projectiles != null)
            {
                _projectiles.PlayBurst(centre, _painted ? ShatterRadius : ShardRadius, _projectiles.Vfx.Death, Shards);
                if (_painted)
                {
                    _projectiles.PlayBurst(centre + Vector3.up * 0.6f, ShardRadius, _projectiles.Vfx.Default, Shards);
                }
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            float left = 1f;
            if (_melt >= 0f)
            {
                _melt += dt;
                left = 1f - Mathf.Clamp01(_melt / MeltSeconds);
                if (left <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (_painted)
            {
                // It snaps out of the breath at nine tenths and settles; a thaw sinks and clears it.
                float grown = Mathf.Lerp(StartScale, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / GrowSeconds)));
                _block.localScale = new Vector3(_full.x * grown, _full.y * grown * Mathf.Lerp(0.6f, 1f, left), _full.z * grown);
                if (_melt >= 0f)
                {
                    foreach (Renderer renderer in _renderers)
                    {
                        renderer.GetPropertyBlock(_props);
                        _props.SetColor(BaseColorId, new Color(1f, 1f, 1f, left));
                        renderer.SetPropertyBlock(_props);
                    }
                }

                return;
            }

            float boxGrown = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / BoxGrowSeconds));
            float height = _full.y * boxGrown * left;
            _block.localScale = new Vector3(_full.x, Mathf.Max(0.01f, height), _full.z);
            _block.localPosition = Vector3.up * (height * 0.5f);
        }
    }
}
