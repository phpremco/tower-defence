using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>A mining node's look: golden crystals while it holds ore, dull and shrunken once the drones have emptied it.</summary>
    public sealed class MiningNodeView : MonoBehaviour
    {
        private const float DepletedScale = 0.55f;
        private static readonly Color DepletedColor = new Color(0.35f, 0.33f, 0.3f);

        private int _index;
        private LevelContext _context;

        public void Initialize(int index)
        {
            _index = index;
        }

        // The context is registered after the visuals are built, so the subscription waits for Start.
        private void Start()
        {
            if (Services.TryGet(out _context))
            {
                _context.MineDepleted.Subscribe(OnDepleted);
            }
        }

        private void OnDestroy()
        {
            _context?.MineDepleted.Unsubscribe(OnDepleted);
        }

        private void OnDepleted(int index)
        {
            if (index != _index)
            {
                return;
            }

            GetComponent<MeshRenderer>().sharedMaterials = ModelPalette.Create(DepletedColor);
            transform.localScale = Vector3.one * DepletedScale;
        }
    }
}
