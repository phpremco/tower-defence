using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Bosses
{
    /// <summary>
    /// The basalt slabs a burrow crack heaves up along its lips (README round 30, <c>Gravemaw_CrackSlabs</c>), kept for
    /// reuse: a crack takes slabs when it is laid and gives them back when it is gone, so a fight of many burrows builds
    /// each slab once. A slab is a holder on its hinge (the slab's low edge on the ground) whose +Z points where the slab
    /// rises and glows, whatever the export's own axes; its local X is the hinge it tips up about.
    /// </summary>
    public sealed class CrackSlabPool : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Dictionary<int, Stack<Slab>> _free = new Dictionary<int, Stack<Slab>>();
        private ModelArtDefinition _art;
        private string[] _parts;

        /// <summary>One pooled slab: its holder and its one renderer.</summary>
        public sealed class Slab
        {
            public Transform Holder;
            public Renderer Renderer;
            public int Variant;
        }

        public int Variants => _parts.Length;

        /// <summary>The shared glow the slabs' material carries (the hot crack); views scale it as it cools.</summary>
        public Color HotGlow { get; private set; }

        /// <summary>The pool under a parent (one per field), or null without the art.</summary>
        public static CrackSlabPool For(Transform parent)
        {
            BossFxArt art = BossFxArt.Active;
            if (art == null || art.CrackSlabs == null || art.CrackSlabParts.Length == 0)
            {
                return null;
            }

            var pool = parent.GetComponent<CrackSlabPool>();
            if (pool == null)
            {
                pool = parent.gameObject.AddComponent<CrackSlabPool>();
                pool._art = art.CrackSlabs;
                pool._parts = art.CrackSlabParts;
                Material material = art.CrackSlabs.MaterialFor(ModelQuality.IsLow);
                pool.HotGlow = material != null && material.HasProperty(EmissionColorId) ? material.GetColor(EmissionColorId) : Color.black;
            }

            return pool;
        }

        /// <summary>A slab of a variant (wrapped), inactive, under the pool until the caller places it.</summary>
        public Slab Take(int variant)
        {
            variant = Mathf.Abs(variant) % _parts.Length;
            if (_free.TryGetValue(variant, out Stack<Slab> free) && free.Count > 0)
            {
                return free.Pop();
            }

            return Build(variant);
        }

        public void Give(Slab slab)
        {
            if (slab == null || slab.Holder == null)
            {
                return;
            }

            slab.Holder.gameObject.SetActive(false);
            slab.Holder.SetParent(transform, false);
            if (!_free.TryGetValue(slab.Variant, out Stack<Slab> free))
            {
                free = new Stack<Slab>();
                _free[slab.Variant] = free;
            }

            free.Push(slab);
        }

        // The export holds all variants at their own spots: keep this one, its hinge on the holder, its rise along +Z.
        private Slab Build(int variant)
        {
            string part = _parts[variant];
            var holder = new GameObject(part).transform;
            holder.SetParent(transform, false);
            var turn = new GameObject("Turn").transform;
            turn.SetParent(holder, false);
            ArtModel model = ArtModel.CreateStill(turn, _art);
            Renderer mine = null;
            foreach (Renderer renderer in model.Renderers)
            {
                bool match = mine == null && renderer.name.StartsWith(part, System.StringComparison.Ordinal);
                renderer.gameObject.SetActive(match);
                if (match)
                {
                    mine = renderer;
                }
            }

            if (mine != null)
            {
                Vector3 hinge = mine.transform.position;
                turn.position -= hinge - holder.position;
                Vector3 rise = mine.bounds.center - holder.position;
                rise.y = 0f;
                if (rise.sqrMagnitude > 0.0001f)
                {
                    // Turn the model about the hinge so its rise lies along the holder's +Z.
                    Quaternion yaw = Quaternion.FromToRotation(rise.normalized, Vector3.forward);
                    turn.localPosition = yaw * turn.localPosition;
                    turn.localRotation = yaw * turn.localRotation;
                }

                mine.shadowCastingMode = ShadowCastingMode.On;
            }

            holder.gameObject.SetActive(false);
            return new Slab { Holder = holder, Renderer = mine, Variant = variant };
        }
    }
}
