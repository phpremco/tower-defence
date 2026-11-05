using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Rime an ice boss lays on the ground (round 29 frost atlas): ahead of it a fresh cell every stride, each growing out
    /// along its branches in about a second, lingering and thawing away; and a larger one under a gate it froze, which
    /// stays while the gate is ice. Random cell and turn per piece, so the frost never repeats. A ring of pieces is made
    /// once and reused. Nothing shows without the art (the boss's haze still marks the way).
    /// </summary>
    public sealed class FrostCreepView : MonoBehaviour
    {
        private const int Pieces = 14;
        private const float Spacing = 1.2f;
        private const float GrowSeconds = 1f;
        private const float HoldSeconds = 4.5f;
        private const float ThawSeconds = 1.5f;
        private const float SizeMin = 2.2f;
        private const float SizeMax = 3f;

        private readonly Renderer[] _pieces = new Renderer[Pieces];
        private readonly float[] _ages = new float[Pieces];
        private readonly bool[] _held = new bool[Pieces];
        private MaterialPropertyBlock _block;
        private float _cellSize = 1f;
        private int _next;
        private Vector3 _last;
        private bool _started;

        /// <summary>The view, or null without the frost art.</summary>
        public static FrostCreepView Create(Transform parent)
        {
            BossFxArt art = BossFxArt.Active;
            Material material = art != null ? art.Material(BossFxArt.Mark.FrostCreep) : null;
            if (material == null)
            {
                return null;
            }

            var go = new GameObject("FrostCreep");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<FrostCreepView>();
            view._block = new MaterialPropertyBlock();
            float cell = art.Width(BossFxArt.Mark.FrostCreep);
            view._cellSize = Mathf.Max(0.1f, cell);
            for (int i = 0; i < Pieces; i++)
            {
                var holder = new GameObject("Rime").transform;
                holder.SetParent(go.transform, false);
                view._pieces[i] = GroundMark.Piece(holder, "Cell", cell, cell, cell, material);
                holder.gameObject.SetActive(false);
            }

            return view;
        }

        /// <summary>The boss is here: a fresh cell once it has moved a stride from the last one.</summary>
        public void Creep(Vector3 at)
        {
            if (_started && (at - _last).sqrMagnitude < Spacing * Spacing)
            {
                return;
            }

            _started = true;
            _last = at;
            Lay(at, Random.Range(SizeMin, SizeMax), false);
        }

        /// <summary>A break in the creep (it climbed, it stopped): the next cell starts afresh.</summary>
        public void Lift() => _started = false;

        /// <summary>A large cell under a frozen gate that stays until <see cref="Release"/>; returns its slot.</summary>
        public int Hold(Vector3 at, float size) => Lay(at, size, true);

        /// <summary>Lets a held cell thaw away.</summary>
        public void Release(int slot)
        {
            if (slot >= 0 && slot < Pieces && _held[slot])
            {
                _held[slot] = false;
                _ages[slot] = GrowSeconds + HoldSeconds;
            }
        }

        private int Lay(Vector3 at, float size, bool hold)
        {
            // Skip a held cell: the gate keeps its frost.
            for (int tries = 0; tries < Pieces && _held[_next]; tries++)
            {
                _next = (_next + 1) % Pieces;
            }

            int slot = _next;
            _next = (_next + 1) % Pieces;
            Transform holder = _pieces[slot].transform.parent;
            holder.position = at;
            holder.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            holder.localScale = Vector3.one * (size / _cellSize);
            GroundMark.UseCell(_pieces[slot], _block, Random.Range(0, 4));
            _ages[slot] = 0f;
            _held[slot] = hold;
            holder.gameObject.SetActive(true);
            Apply(slot);
            return slot;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < Pieces; i++)
            {
                if (!_pieces[i].transform.parent.gameObject.activeSelf)
                {
                    continue;
                }

                _ages[i] += dt;
                if (_held[i])
                {
                    _ages[i] = Mathf.Min(_ages[i], GrowSeconds);
                }
                else if (_ages[i] > GrowSeconds + HoldSeconds + ThawSeconds)
                {
                    _pieces[i].transform.parent.gameObject.SetActive(false);
                    continue;
                }

                Apply(i);
            }
        }

        private void Apply(int slot)
        {
            float age = _ages[slot];
            _pieces[slot].GetPropertyBlock(_block);
            _block.SetFloat(GroundMark.Progress, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / GrowSeconds)));
            _block.SetFloat(GroundMark.Fade, 1f - Mathf.Clamp01((age - GrowSeconds - HoldSeconds) / ThawSeconds));
            _pieces[slot].SetPropertyBlock(_block);
        }
    }
}
