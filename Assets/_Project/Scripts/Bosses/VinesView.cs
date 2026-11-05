using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The vines that grow over an overgrown pad and its tower (README round 28: RotOvergrowth, blend shape "Retracted",
    /// 1 flat on the pad and 0 grown): they creep up over about a second, and when the pad is burned clean they pull back
    /// down with a flash of fire and are gone. One per overgrown pad, reused. View only.
    /// </summary>
    public sealed class VinesView : MonoBehaviour
    {
        private const string RetractedShape = "Retracted";
        private const float FullWeight = 100f;
        private const float BurnSeconds = 1f;
        private const int BurnQuads = 4;
        private const float BurnRing = 0.55f;
        // A flame sheet is about 0.8 x 1.8 m (README round 29), its pivot at the bottom: the centre sits this high.
        private const float BurnSize = 1.6f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Burn = new Color(1f, 0.55f, 0.15f) * 3f;

        private ArtModel _model;
        private Material _material;
        private Color _restEmission;
        private SkinnedMeshRenderer[] _shaped = new SkinnedMeshRenderer[0];
        private int[] _shapes = new int[0];
        private float _growSeconds = 1f;
        private float _grown;
        private bool _burning;
        private FlipbookFx _flames;

        public bool Active => gameObject.activeSelf;

        public static VinesView Create(Transform parent, ModelArtDefinition art)
        {
            var go = new GameObject("Vines");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<VinesView>();
            view._flames = FlipbookFx.Create(parent, "VineBurn", BossFxArt.Sheet.VineBurn, false);
            if (art != null && art.Model != null)
            {
                view._material = new Material(art.MaterialFor(ModelQuality.IsLow)) { name = art.name + "_Vines" };
                view._restEmission = view._material.GetColor(EmissionColorId);
                view._model = ArtModel.CreateStill(go.transform, art, view._material);
                view.FindShapes();
            }

            go.SetActive(false);
            return view;
        }

        /// <summary>Grows the vines over a pad (the pad's centre on the ground) in the given seconds.</summary>
        public void Grow(Vector3 at, float seconds)
        {
            transform.position = at;
            _growSeconds = Mathf.Max(0.05f, seconds);
            _grown = 0f;
            _burning = false;
            SetEmission(_restEmission);
            gameObject.SetActive(_model != null);
            Apply(0f);
        }

        /// <summary>Burns the vines off: they flare and pull back down, then hide.</summary>
        public void BurnOff()
        {
            if (gameObject.activeSelf)
            {
                _burning = true;
                PlayFlames();
            }
        }

        public void Hide() => gameObject.SetActive(false);

        // Fire runs up the vines from a few points round the tower while they pull back (README round 29: VineBurn).
        private void PlayFlames()
        {
            if (_flames == null)
            {
                return;
            }

            for (int i = 0; i < BurnQuads; i++)
            {
                float angle = (i + Random.value * 0.5f) * Mathf.PI * 2f / BurnQuads;
                Vector3 at = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * BurnRing + Vector3.up * (BurnSize * 0.5f);
                _flames.Play(at, BurnSize * Random.Range(0.85f, 1.1f), 0f, Color.white);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_burning)
            {
                _grown -= dt / BurnSeconds;
                SetEmission(Color.Lerp(_restEmission, Burn, Mathf.Clamp01(_grown) * 4f));
                if (_grown <= 0f)
                {
                    Hide();
                    return;
                }
            }
            else if (_grown < 1f)
            {
                _grown = Mathf.Min(1f, _grown + dt / _growSeconds);
            }

            Apply(_grown);
        }

        // Grown 0 is the vines flat on the pad (Retracted 100), grown 1 over the whole tower (Retracted 0).
        private void Apply(float grown)
        {
            float weight = FullWeight * (1f - Mathf.Clamp01(grown));
            for (int i = 0; i < _shaped.Length; i++)
            {
                _shaped[i].SetBlendShapeWeight(_shapes[i], weight);
            }
        }

        private void SetEmission(Color colour)
        {
            if (_material != null)
            {
                _material.SetColor(EmissionColorId, colour);
            }
        }

        private void FindShapes()
        {
            SkinnedMeshRenderer[] all = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var renderers = new System.Collections.Generic.List<SkinnedMeshRenderer>(all.Length);
            var shapes = new System.Collections.Generic.List<int>(all.Length);
            foreach (SkinnedMeshRenderer renderer in all)
            {
                int shape = renderer.sharedMesh != null ? renderer.sharedMesh.GetBlendShapeIndex(RetractedShape) : -1;
                if (shape >= 0)
                {
                    renderers.Add(renderer);
                    shapes.Add(shape);
                }
            }

            _shaped = renderers.ToArray();
            _shapes = shapes.ToArray();
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
