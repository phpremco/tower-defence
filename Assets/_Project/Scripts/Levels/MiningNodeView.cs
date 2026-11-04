using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A mining node's look: golden crystals while it holds ore, dull and shrunken once the drones have emptied it. With
    /// imported art the ore vein shows its full mesh, its sawn-off half once the drones have taken half the ore, and the
    /// dull stumps once it is empty.
    /// </summary>
    public sealed class MiningNodeView : MonoBehaviour
    {
        private const float DepletedScale = 0.55f;
        private const string FullSuffix = "_Full";
        private const string HalfSuffix = "_Half";
        private const string EmptySuffix = "_Empty";
        private static readonly Color DepletedColor = new Color(0.35f, 0.33f, 0.3f);

        private int _index;
        private LevelContext _context;
        private ArtModel _art;
        private Renderer[] _full;
        private Renderer[] _half;
        private Renderer[] _empty;

        public void Initialize(int index, ModelArtDefinition art = null)
        {
            _index = index;
            if (art == null)
            {
                return;
            }

            _art = ArtModel.Create(transform, art, ownMaterial: false);
            _full = _art.RenderersEndingWith(FullSuffix);
            _half = _art.RenderersEndingWith(HalfSuffix);
            _empty = _art.RenderersEndingWith(EmptySuffix);
            Show(_full);
        }

        // The context is registered after the visuals are built, so the subscription waits for Start.
        private void Start()
        {
            if (Services.TryGet(out _context))
            {
                _context.MineDepleted.Subscribe(OnDepleted);
                _context.MineHalved.Subscribe(OnHalved);
            }
        }

        private void OnDestroy()
        {
            _context?.MineDepleted.Unsubscribe(OnDepleted);
            _context?.MineHalved.Unsubscribe(OnHalved);
            _art?.Dispose();
        }

        private void OnHalved(int index)
        {
            if (index == _index && _art != null)
            {
                Show(_half);
            }
        }

        private void OnDepleted(int index)
        {
            if (index != _index)
            {
                return;
            }

            if (_art != null)
            {
                Show(_empty);
                return;
            }

            GetComponent<MeshRenderer>().sharedMaterials = ModelPalette.Create(DepletedColor);
            transform.localScale = Vector3.one * DepletedScale;
        }

        private void Show(Renderer[] stage)
        {
            Enable(_full, stage == _full);
            Enable(_half, stage == _half);
            Enable(_empty, stage == _empty);
        }

        private static void Enable(Renderer[] renderers, bool enabled)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = enabled;
            }
        }
    }
}
