using Game.Combat;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The ice block a frozen gate becomes (Docs/BOSSES.md, Frost Warden): a block of the level's own ice sheet (the
    /// Blender liquid material) round the gate, which grows over it, holds, and melts away on a thaw or bursts when the
    /// gate shatters. It follows the gate's own events, so whatever ends the freeze, the block goes with it. Until the ice
    /// block has art of its own (Docs/MODEL_TODO.md) it is a box in the ice material.
    /// </summary>
    public sealed class IceBlockView : MonoBehaviour
    {
        private const float GrowSeconds = 0.5f;
        private const float MeltSeconds = 0.8f;
        private const float Height = 1.5f;
        private const float Depth = 1.1f;
        private const float SpanMargin = 1.1f;
        private const float ShardRadius = 1.6f;
        private static readonly Color Shards = new Color(0.8f, 0.95f, 1f);

        private Tower _gate;
        private Transform _block;
        private ProjectilePool _projectiles;
        private Vector3 _full;
        private float _age;
        private float _melt = -1f;

        /// <summary>Freezes the gate's look into a block, oriented across the road.</summary>
        public static IceBlockView Create(Transform parent, Tower gate, Vector3 along, Material ice, ProjectilePool projectiles)
        {
            var go = new GameObject("IceBlock");
            go.transform.SetParent(parent, false);
            go.transform.position = gate.Position;
            go.transform.rotation = Quaternion.LookRotation(along.sqrMagnitude > 0.0001f ? along : Vector3.forward, Vector3.up);
            var view = go.AddComponent<IceBlockView>();
            view._gate = gate;
            view._projectiles = projectiles;
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(block.GetComponent<Collider>());
            block.name = "Ice";
            block.transform.SetParent(go.transform, false);
            view._full = new Vector3(gate.Span * SpanMargin, Height, Depth);
            block.transform.localPosition = Vector3.up * (Height * 0.5f);
            block.transform.localScale = new Vector3(view._full.x, 0.01f, view._full.z);
            var renderer = block.GetComponent<MeshRenderer>();
            if (ice != null)
            {
                renderer.sharedMaterial = ice;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            view._block = block.transform;
            gate.FrozenChanged += view.OnFrozenChanged;
            gate.Smashed += view.OnSmashed;
            return view;
        }

        private void OnDestroy()
        {
            if (_gate != null)
            {
                _gate.FrozenChanged -= OnFrozenChanged;
                _gate.Smashed -= OnSmashed;
            }
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
            _projectiles?.PlayBurst(gate.Position + Vector3.up * 0.6f, ShardRadius, _projectiles.Vfx.Death, Shards);
            Destroy(gameObject);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            float grown = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / GrowSeconds));
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

            float height = _full.y * grown * left;
            _block.localScale = new Vector3(_full.x, Mathf.Max(0.01f, height), _full.z);
            _block.localPosition = Vector3.up * (height * 0.5f);
        }
    }
}
