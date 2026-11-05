using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Shows a <see cref="LavaTrail"/>. With the round-29 art each live patch is a piece of the painted trail strip (lava:
    /// basalt crust over a molten channel that cools from its edges in; rot: slime and mould that decays from its edges),
    /// turned along the way the boss went and started at a random point of its tile so the pieces never repeat in step.
    /// Without the art each patch is a pool of light. A few embers or spores rise while it is fresh. The pieces are made
    /// once, one per slot of the trail's ring, and reused.
    /// </summary>
    public sealed class LavaTrailView : MonoBehaviour
    {
        private const float PoolScale = 1.5f;
        private const float EmberRate = 6f;
        // A piece reaches this far past the patch spacing on each side, so the pieces overlap into one trail.
        private const float PieceOverlap = 1.9f;
        // The hot lava starts a little cooled (README round 29: the fresh strip glows over too much of its crack net).
        private const float FirstCool = 0.2f;
        private const float FadeFrom = 0.7f;
        // The newest and the oldest piece round off over most of their half length (the trail's two ends); the ones
        // between only over the share that the next piece covers anyway (GroundMark: no square ends).
        private const float TrailEndTaper = 2f;
        private const float TailNarrowest = 0.4f;
        private const float TailPieces = 2.5f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Hot = new Color(1f, 0.45f, 0.08f, 1f) * 2.2f;
        private static readonly Color Ember = new Color(1f, 0.6f, 0.2f, 0.9f);

        private LavaTrail _trail;
        private Color _hot;
        private Renderer[] _pools;
        private MaterialPropertyBlock _block;
        private ParticleSystem _embers;
        private bool _marked;
        private bool _heat;
        private Vector3 _lastLaid;
        private bool _hasLast;
        private int _newest = -1;

        public static LavaTrailView Create(Transform parent, LavaTrail trail, float radius) => Create(parent, trail, radius, Hot, Ember, "LavaTrail", BossFxArt.Mark.LavaTrail);

        /// <summary>A trail in other colours and another mark (the rot an uprooted flower leaves behind it).</summary>
        public static LavaTrailView Create(Transform parent, LavaTrail trail, float radius, Color hot, Color ember, string name, BossFxArt.Mark mark = BossFxArt.Mark.RotTrail)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<LavaTrailView>();
            view._trail = trail;
            view._hot = hot;
            view._block = new MaterialPropertyBlock();
            view._pools = new Renderer[trail.Capacity];
            BossFxArt art = BossFxArt.Active;
            Material material = art != null ? art.Material(mark) : null;
            view._marked = material != null;
            view._heat = mark == BossFxArt.Mark.LavaTrail;
            for (int i = 0; i < trail.Capacity; i++)
            {
                var slot = new GameObject("Patch").transform;
                slot.SetParent(go.transform, false);
                view._pools[i] = view._marked
                    ? GroundMark.Piece(slot, "Piece", trail.Spacing * PieceOverlap, art.Width(mark), art.TileLength(mark), material)
                    : GlowDecal.PlacePool(slot, hot, radius * PoolScale).GetComponent<MeshRenderer>();
                slot.gameObject.SetActive(false);
            }

            view._embers = ParticleKit.Create(go.transform, "Embers", Vector3.zero, ember, 0.6f, 1.2f, 0.04f, 0.1f, 0.4f, 1f, -0.4f, 80, radius);
            ParticleKit.SetRate(view._embers, 0f);
            trail.Laid += view.OnLaid;
            return view;
        }

        private void OnDestroy()
        {
            if (_trail != null)
            {
                _trail.Laid -= OnLaid;
            }
        }

        private void OnLaid(int slot)
        {
            Transform piece = _pools[slot].transform.parent;
            Vector3 at = _trail.PointAt(slot);
            piece.position = at;
            Vector3 along = _hasLast ? at - _lastLaid : Vector3.zero;
            along.y = 0f;
            if (along.sqrMagnitude > 0.0001f && along.sqrMagnitude < 4f * _trail.Spacing * _trail.Spacing)
            {
                piece.rotation = Quaternion.LookRotation(along.normalized, Vector3.up);
            }
            else if (_marked)
            {
                piece.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            _lastLaid = at;
            _hasLast = true;
            _newest = slot;
            piece.gameObject.SetActive(true);
            _embers.transform.position = at;
            _embers.Emit(2);
        }

        private void Update()
        {
            int oldest = OldestLive();
            for (int i = 0; i < _pools.Length; i++)
            {
                Transform piece = _pools[i].transform.parent;
                if (!piece.gameObject.activeSelf)
                {
                    continue;
                }

                if (!_trail.IsLive(i))
                {
                    piece.gameObject.SetActive(false);
                    continue;
                }

                float age = _trail.AgeAt(i) / _trail.Lifetime;
                _pools[i].GetPropertyBlock(_block);
                if (_marked)
                {
                    _block.SetFloat(GroundMark.Progress, _heat ? Mathf.Lerp(FirstCool, 1f, age) : age);
                    _block.SetFloat(GroundMark.Fade, 1f - Mathf.Clamp01((age - FadeFrom) / (1f - FadeFrom)));
                    _block.SetFloat(GroundMark.EndTaper, i == _newest || i == oldest ? TrailEndTaper : 1f);
                    piece.localScale = new Vector3(TailWidth(i), 1f, 1f);
                }
                else
                {
                    float heat = 1f - age;
                    Color glow = _hot * (heat * heat);
                    _block.SetColor(MarkerArt.Tint, glow);
                    _block.SetColor(EmissionColorId, glow);
                }

                _pools[i].SetPropertyBlock(_block);
            }
        }

        // The oldest pieces narrow towards the end of the trail, so it tapers off over a few pieces instead of stopping.
        private float TailWidth(int slot)
        {
            int older = 0;
            float age = _trail.AgeAt(slot);
            for (int i = 0; i < _pools.Length; i++)
            {
                if (i != slot && _pools[i].transform.parent.gameObject.activeSelf && _trail.IsLive(i) && _trail.AgeAt(i) > age)
                {
                    older++;
                }
            }

            return Mathf.Lerp(TailNarrowest, 1f, Mathf.Clamp01(older / TailPieces));
        }

        private int OldestLive()
        {
            int oldest = -1;
            float age = -1f;
            for (int i = 0; i < _pools.Length; i++)
            {
                if (_pools[i].transform.parent.gameObject.activeSelf && _trail.IsLive(i) && _trail.AgeAt(i) > age)
                {
                    age = _trail.AgeAt(i);
                    oldest = i;
                }
            }

            return oldest;
        }

        /// <summary>Puts every patch out at once (the boss left the field).</summary>
        public void Clear()
        {
            _hasLast = false;
            _newest = -1;
            foreach (Renderer pool in _pools)
            {
                pool.transform.parent.gameObject.SetActive(false);
            }
        }

        public void SetEmbers(bool on) => ParticleKit.SetRate(_embers, on ? EmberRate : 0f);
    }
}
