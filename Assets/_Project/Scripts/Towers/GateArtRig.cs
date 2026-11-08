using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// The imported gate model inside a <see cref="GateView"/>: the model of the gate's level stretched across the road,
    /// its clips (a flinch on a hit, the panels popping back on a repair, the collapse, rising again on a rebuild), the
    /// damage overlay and the loose panels that fall off when the gate is badly hurt, and the wreck that replaces it once
    /// the collapse has played. Plain class: the view tells it what happened and ticks it.
    /// </summary>
    public sealed class GateArtRig
    {
        private const string FieldSocket = "Field";
        private const string SparkPrefix = "Spark";
        private const string DamageSuffix = "_Damage";
        private const float LooseRestEpsilon = 0.0001f;

        private readonly GateArtDefinition _art;
        private readonly Transform _holder;
        private readonly Transform _parent;
        private ArtModel _model;
        private ArtModel _wreck;
        private Renderer[] _overlay = new Renderer[0];
        private Transform[] _loose = new Transform[0];
        private Vector3[] _looseRest = new Vector3[0];
        private int _hit = -1;
        private int _repair = -1;
        private int _build = -1;
        private int _collapse = -1;
        private bool _damaged;
        private bool _collapsing;
        private float _collapseLeft;

        public GateArtRig(Transform parent, GateArtDefinition art, float width)
        {
            _art = art;
            _parent = parent;
            _holder = new GameObject("GateModel").transform;
            _holder.SetParent(parent, false);
            // The model fits the corridor it was built for; a wider road stretches the beam, not the height.
            _holder.localScale = new Vector3(width / art.ModelWidth, 1f, 1f);
        }

        /// <summary>Height of the field socket above the road, where the energy field is centred.</summary>
        public float FieldHeight { get; private set; } = 0.5f;

        /// <summary>The torn sockets that spark while the gate is damaged.</summary>
        public Transform[] Sparks { get; private set; } = new Transform[0];

        public bool IsDamaged => _damaged;

        /// <summary>Width of the energy field as a share of the gate's width.</summary>
        public float FieldShare => _art.FieldShare;

        /// <summary>Health share below which the gate shows its damage.</summary>
        public float DamagedBelow => _art.DamagedBelow;

        /// <summary>How far above the damage line a damaged gate must heal before it looks whole again.</summary>
        public float RecoverMargin => _art.RecoverMargin;

        /// <summary>Wears the model of a level; a new level replaces the old model.</summary>
        public void ShowLevel(int levelIndex)
        {
            ModelArtDefinition wanted = _art.ArtFor(levelIndex);
            if (wanted == null || (_model != null && _model.Art == wanted))
            {
                return;
            }

            bool hidden = _model != null && !_model.Root.gameObject.activeSelf;
            _model?.Dispose();
            _model = ArtModel.Create(_holder, wanted, ownMaterial: false);
            WriteRestPoseOnDisable(_model.Root);
            _model.SetActive(!hidden);
            _hit = wanted.IndexOf(ModelClipRole.Hit);
            _repair = wanted.IndexOf(ModelClipRole.Repair);
            _build = wanted.IndexOf(ModelClipRole.Build);
            _collapse = wanted.IndexOf(ModelClipRole.Death);
            _overlay = _model.RenderersEndingWith(DamageSuffix);
            _loose = FindAll(_art.LooseParts);
            _looseRest = RestScales(_loose);
            Sparks = _model.Numbered(SparkPrefix);
            Transform field = _model.Find(FieldSocket);
            FieldHeight = field != null ? _parent.InverseTransformPoint(field.position).y : FieldHeight;
            ShowOverlay(_damaged);
        }

        /// <summary>A flinch, unless a bigger clip is already playing.</summary>
        public void Hit()
        {
            ModelClipPlayer player = _model?.Player;
            if (player != null && player.Once < 0 && !_collapsing)
            {
                player.PlayOnce(_hit);
            }
        }

        /// <summary>Below the damage line the overlay shows and the loose panels drop; back above it they pop back in.</summary>
        public void SetDamaged(bool damaged)
        {
            if (damaged == _damaged)
            {
                return;
            }

            _damaged = damaged;
            ShowOverlay(damaged);
            if (!damaged && !_collapsing)
            {
                _model?.Player?.PlayOnce(_repair);
            }
        }

        /// <summary>The gate falls: the collapse plays and holds, then the wreck takes its place.</summary>
        public void Collapse()
        {
            _collapsing = true;
            ModelClipPlayer player = _model?.Player;
            if (player == null || _collapse < 0)
            {
                _collapseLeft = 0f;
                return;
            }

            player.PlayOnce(_collapse, hold: true);
            _collapseLeft = _model.Art.Clips[_collapse].Length;
        }

        /// <summary>Rebuilt: the wreck goes and the gate rises again, whole.</summary>
        public void Rebuild()
        {
            _collapsing = false;
            _damaged = false;
            ShowOverlay(false);
            _wreck?.SetActive(false);
            if (_model == null)
            {
                return;
            }

            _model.SetActive(true);
            _model.Player?.Restart();
            _model.Player?.PlayOnce(_build);
        }

        public void Tick(float deltaTime)
        {
            if (_model == null)
            {
                return;
            }

            if (_collapsing && _collapseLeft > 0f)
            {
                _collapseLeft -= deltaTime;
                if (_collapseLeft <= 0f)
                {
                    ShowWreck();
                }
            }
            else if (_collapsing && _model.Root.gameObject.activeSelf)
            {
                ShowWreck();
            }

            if (_model.Root.gameObject.activeSelf)
            {
                _model.Player?.Tick(deltaTime);
            }
        }

        // After the clips have posed the rig, the panels follow the damage state and nothing else: off while the gate is
        // hurt (unless a clip that puts them back, repair or build, is playing), on at their rest scale while it is whole
        // and no clip is on top. The gate has no idle cycle, so with no clip playing the animation writes nothing and the
        // panels used to keep whatever the last clip or the last damaged frame left (round 23c: a whole gate without panels).
        public void LateTick()
        {
            if (_model == null || _collapsing)
            {
                return;
            }

            ModelClipPlayer player = _model.Player;
            if (_damaged)
            {
                if (player == null || !(player.IsPlaying(ModelClipRole.Repair) || player.IsPlaying(ModelClipRole.Build)))
                {
                    SetLoose(false);
                }
            }
            else if (player == null || player.Once < 0)
            {
                SetLoose(true);
            }
        }

        private void SetLoose(bool shown)
        {
            for (int i = 0; i < _loose.Length; i++)
            {
                _loose[i].localScale = shown ? _looseRest[i] : Vector3.zero;
            }
        }

        // The panels as the model was built, whole; a bone exported collapsed to nothing counts as unscaled.
        // The collapse holds its last frame and the model is then switched off for the wreck. An Animator takes the pose it
        // finds on being switched on again as its rest pose, so a rebuilt gate fell back to the collapsed pose whenever no
        // clip was playing: whole during a build, hit or repair clip, broken between them (round 23c, the repair flicker).
        // Writing the rest pose back on disable makes the rebuilt gate rebind to its whole pose.
        private static void WriteRestPoseOnDisable(Transform root)
        {
            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.writeDefaultValuesOnDisable = true;
            }
        }

        private static Vector3[] RestScales(Transform[] parts)
        {
            var rest = new Vector3[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                Vector3 scale = parts[i].localScale;
                rest[i] = scale.sqrMagnitude < LooseRestEpsilon ? Vector3.one : scale;
            }

            return rest;
        }

        public void Dispose()
        {
            _model?.Dispose();
            _wreck?.Dispose();
        }

        private void ShowWreck()
        {
            _model.SetActive(false);
            if (_wreck == null && _art.Wreck != null)
            {
                _wreck = ArtModel.Create(_holder, _art.Wreck, ownMaterial: false);
            }

            _wreck?.SetActive(true);
        }

        private void ShowOverlay(bool shown)
        {
            for (int i = 0; i < _overlay.Length; i++)
            {
                _overlay[i].enabled = shown;
            }
        }

        private Transform[] FindAll(string[] names)
        {
            var found = new System.Collections.Generic.List<Transform>(names.Length);
            foreach (string name in names)
            {
                Transform bone = _model.Find(name);
                if (bone != null)
                {
                    found.Add(bone);
                }
            }

            return found.ToArray();
        }
    }
}
