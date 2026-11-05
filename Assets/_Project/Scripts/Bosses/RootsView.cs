using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// A clump of a boss's roots on the ground (README round 28: RotbloomRoots, three stages in one model): one stage shows
    /// at a time and grows in from a smaller scale when it changes, so the player sees the roots spread wave by wave. View
    /// only; without the art it shows nothing.
    /// </summary>
    public sealed class RootsView : MonoBehaviour
    {
        private const float GrowFrom = 0.6f;
        private const float GrowSeconds = 1.2f;

        private ArtModel _model;
        private Transform[] _stages = new Transform[0];
        private float _grow = 1f;
        private float _baseScale = 1f;

        /// <summary>The stage showing, or -1.</summary>
        public int Stage { get; private set; } = -1;

        public static RootsView Create(Transform parent, ModelArtDefinition art, string[] stageObjects, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<RootsView>();
            if (art != null && art.Model != null)
            {
                view._model = ArtModel.CreateStill(go.transform, art);
                view._baseScale = view._model.Root.localScale.x;
                view._stages = new Transform[stageObjects.Length];
                for (int i = 0; i < stageObjects.Length; i++)
                {
                    view._stages[i] = view._model.Find(stageObjects[i]);
                }
            }

            view.Show(-1, Vector3.zero, Vector3.forward);
            return view;
        }

        /// <summary>Shows one stage at a point facing a direction (-1 hides the roots); a new stage grows in.</summary>
        public void Show(int stage, Vector3 at, Vector3 facing)
        {
            bool changed = stage != Stage;
            Stage = stage;
            transform.position = at;
            if (facing.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.z));
            }

            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stages[i] != null)
                {
                    _stages[i].gameObject.SetActive(i == stage);
                }
            }

            gameObject.SetActive(stage >= 0 && _model != null);
            if (changed && stage >= 0)
            {
                _grow = 0f;
            }
        }

        private void Update()
        {
            if (_model == null || _grow >= 1f)
            {
                return;
            }

            _grow = Mathf.Min(1f, _grow + Time.deltaTime / GrowSeconds);
            float eased = 1f - (1f - _grow) * (1f - _grow);
            _model.Root.localScale = Vector3.one * (_baseScale * Mathf.Lerp(GrowFrom, 1f, eased));
        }
    }
}
