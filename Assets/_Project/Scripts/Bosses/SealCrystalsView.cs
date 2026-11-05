using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The sealed crystals round the Sealed One's rift (Docs/BOSSES.md, README round 29 SealCrystal): a ring of crystals in
    /// their collars, claws and sockets, dormant lilac, taking the colour of the weapon it has learned, and white at its
    /// release. They stand where nothing else does (clear of towers and the road) at a random turn and size. The emission
    /// map is a white mask: one colour per crystal through a property block. Nothing without the art.
    /// </summary>
    public sealed class SealCrystalsView : MonoBehaviour
    {
        private const int Count = 5;
        private const float Radius = 3.4f;
        private const float MinScale = 0.8f;
        private const float MaxScale = 1.2f;
        private const float RiseSeconds = 1.2f;
        private const float SinkDepth = 1.6f;
        private const float BlendSeconds = 0.8f;
        public static readonly Color Dormant = new Color(0.62f, 0.45f, 1f) * 1.6f;
        public static readonly Color Released = Color.white * 3f;
        private const float LearnedGlow = 2.4f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly List<Transform> _crystals = new List<Transform>(Count);
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private MaterialPropertyBlock _block;
        private Color _from;
        private Color _to;
        private float _blend = 1f;
        private float _age;

        /// <summary>The ring round a centre, its crystals on spots the predicate allows; null without the art.</summary>
        public static SealCrystalsView Create(Transform parent, Vector3 centre, System.Func<Vector3, bool> free, System.Func<Vector3, float> ground)
        {
            BossFxArt art = BossFxArt.Active;
            if (art == null || art.SealCrystals == null || art.SealCrystalParts.Length == 0)
            {
                return null;
            }

            var go = new GameObject("SealCrystals");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<SealCrystalsView>();
            view._block = new MaterialPropertyBlock();
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < Count * 3 && view._crystals.Count < Count; i++)
            {
                float angle = (start + i * (360f / Count) + (i >= Count ? 360f / Count * 0.5f : 0f)) * Mathf.Deg2Rad;
                float reach = Radius * (i >= Count * 2 ? 1.35f : 1f);
                Vector3 at = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach;
                if (!free(at))
                {
                    continue;
                }

                at.y = ground(at);
                view.Place(art, at, art.SealCrystalParts[view._crystals.Count % art.SealCrystalParts.Length]);
            }

            view._from = view._to = Dormant;
            view.Apply(Dormant);
            return view;
        }

        /// <summary>Glow towards a colour (a learned weapon's x 2.4, or <see cref="Dormant"/> / <see cref="Released"/>).</summary>
        public void Tint(Color colour, bool learned)
        {
            _from = Current;
            _to = learned ? colour * LearnedGlow : colour;
            _blend = 0f;
        }

        private Color Current => Color.Lerp(_from, _to, Mathf.Clamp01(_blend));

        private void Place(BossFxArt art, Vector3 at, string part)
        {
            var holder = new GameObject(part).transform;
            holder.SetParent(transform, false);
            holder.position = at;
            holder.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            holder.localScale = Vector3.one * Random.Range(MinScale, MaxScale);
            ArtModel model = ArtModel.CreateStill(holder, art.SealCrystals);
            foreach (Renderer renderer in model.Renderers)
            {
                bool mine = renderer.name.StartsWith(part, System.StringComparison.Ordinal);
                renderer.gameObject.SetActive(mine);
                if (mine)
                {
                    // Each variant sits at its own spot in the export: centre it on the holder.
                    Vector3 offset = renderer.bounds.center - holder.position;
                    renderer.transform.position -= new Vector3(offset.x, 0f, offset.z);
                    _renderers.Add(renderer);
                }
            }

            _crystals.Add(holder);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            // They push up out of the ground as the rift opens.
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / RiseSeconds));
            foreach (Transform crystal in _crystals)
            {
                Transform model = crystal.childCount > 0 ? crystal.GetChild(0) : null;
                if (model != null)
                {
                    model.localPosition = Vector3.down * (SinkDepth * (1f - rise));
                }
            }

            if (_blend < 1f)
            {
                _blend += dt / BlendSeconds;
                Apply(Current);
            }
        }

        private void Apply(Color colour)
        {
            foreach (Renderer renderer in _renderers)
            {
                renderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, colour);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
