using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// The gameplay markers, bars, projectiles, debris, scout drone and particle atlas painted in Blender (ArtSource
    /// README round 25b). Written by the generator (Tools/Generate/Blender Markers); views only place, scale, tint and
    /// animate the pieces. Every flat piece wears a <c>Game/MarkerPack</c> material that a view tints per renderer with
    /// <see cref="Tint"/> and cuts with <see cref="Fill"/>. Registered in <see cref="Services"/> at boot; without it a
    /// view keeps its plain fallback shape (tests that build views on their own).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Marker Art", fileName = "MarkerArt")]
    public sealed class MarkerArt : ScriptableObject
    {
        /// <summary>Particle atlas cells, in the atlas order (top-left first).</summary>
        public enum ParticleCell
        {
            Spore,
            Mote,
            Ember,
            Drip,
            IchorSplat,
            GooDecal,
            FrostMote,
            ChronoMote,
        }

        [Serializable]
        private sealed class Piece
        {
            [SerializeField] private MarkerKind _kind;
            [SerializeField] private Mesh _mesh;
            [SerializeField] private Material _material;
            [SerializeField] private Material _lowMaterial;

            public MarkerKind Kind => _kind;
            public Mesh Mesh => _mesh;
            public Material Material(bool low) => low && _lowMaterial != null ? _lowMaterial : _material;
        }

        /// <summary>A lit model (the tower map set) with two named sockets in its own space.</summary>
        [Serializable]
        public sealed class LitModel
        {
            [SerializeField] private Mesh _mesh;
            [SerializeField] private Material _material;
            [SerializeField] private Material _lowMaterial;
            [SerializeField] private Vector3 _socketA;
            [SerializeField] private Vector3 _socketB;

            public Mesh Mesh => _mesh;
            public Material Material(bool low) => low && _lowMaterial != null ? _lowMaterial : _material;

            /// <summary>Missile: the exhaust; scout: the trail start.</summary>
            public Vector3 SocketA => _socketA;

            /// <summary>Missile: the nose; scout: the light.</summary>
            public Vector3 SocketB => _socketB;

            public bool IsComplete => _mesh != null && _material != null;
        }

        public static readonly int Tint = Shader.PropertyToID("_Tint");
        public static readonly int Fill = Shader.PropertyToID("_Fill");
        public static readonly int CoverFromCore = Shader.PropertyToID("_CoverFromCore");
        public static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

        public const int ParticleColumns = 4;
        public const int ParticleRows = 2;

        [SerializeField] private Piece[] _pieces = new Piece[0];
        [SerializeField] private LitModel _missile = new LitModel();
        [SerializeField] private LitModel _scout = new LitModel();
        [Tooltip("Debris chunks; the first HotChunks of them are the glowing hex pieces.")]
        [SerializeField] private Mesh[] _debris = new Mesh[0];
        [SerializeField] [Min(0)] private int _hotChunks = 2;
        [SerializeField] private Material _debrisMaterial;
        [SerializeField] private Material _debrisLowMaterial;
        [Header("Particle atlas (4 x 2)")]
        [SerializeField] private Material _particleGlow;
        [SerializeField] private Material _particleGlowLow;
        [SerializeField] private Material _particleCover;
        [SerializeField] private Material _particleCoverLow;

        public LitModel Missile => _missile;
        public LitModel Scout => _scout;
        public int DebrisCount => _debris.Length;
        public int HotChunks => Mathf.Min(_hotChunks, _debris.Length);
        public Mesh Debris(int index) => _debris[index];
        public Material DebrisMaterial => ModelQuality.IsLow && _debrisLowMaterial != null ? _debrisLowMaterial : _debrisMaterial;

        /// <summary>The registered art, or null in tests and tools that run without it.</summary>
        public static MarkerArt Active => Services.TryGet(out MarkerArt art) ? art : null;

        public bool Has(MarkerKind kind) => Find(kind) != null;

        public Mesh Mesh(MarkerKind kind) => Find(kind)?.Mesh;

        public Material Material(MarkerKind kind) => Find(kind)?.Material(ModelQuality.IsLow);

        /// <summary>The particle material for a cell: additive for the glowing ones, covering for drips and splats.</summary>
        public Material Particles(ParticleCell cell) => ParticleMaterial(cell == ParticleCell.Drip || cell == ParticleCell.IchorSplat || cell == ParticleCell.GooDecal);

        /// <summary>
        /// The covering (alpha-blended) or the additive particle material of the atlas, for any cell: a soft mote drawn covering
        /// is a puff of smoke that hides a little of what lies behind it instead of glowing.
        /// </summary>
        public Material ParticleMaterial(bool covers)
        {
            bool low = ModelQuality.IsLow;
            return covers ? (low && _particleCoverLow != null ? _particleCoverLow : _particleCover) : (low && _particleGlowLow != null ? _particleGlowLow : _particleGlow);
        }

        /// <summary>A renderer of the piece under <paramref name="parent"/>, without shadows; null when the piece is missing.</summary>
        public MeshRenderer Place(MarkerKind kind, Transform parent, string name = null)
        {
            Piece piece = Find(kind);
            if (piece == null || piece.Mesh == null)
            {
                return null;
            }

            var go = new GameObject(name ?? kind.ToString());
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = piece.Mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = piece.Material(ModelQuality.IsLow);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        /// <summary>Sets the tint of one renderer through a property block (the shared material stays untouched).</summary>
        public static void SetTint(Renderer renderer, MaterialPropertyBlock block, Color tint)
        {
            renderer.GetPropertyBlock(block);
            block.SetColor(Tint, tint);
            renderer.SetPropertyBlock(block);
        }

        private Piece Find(MarkerKind kind)
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i] != null && _pieces[i].Kind == kind && _pieces[i].Mesh != null && _pieces[i].Material(false) != null)
                {
                    return _pieces[i];
                }
            }

            return null;
        }

        private void OnValidate()
        {
            foreach (MarkerKind kind in (MarkerKind[])Enum.GetValues(typeof(MarkerKind)))
            {
                if (Find(kind) == null)
                {
                    Debug.LogWarning($"MarkerArt '{name}': no piece for {kind}; run Tools/Generate/Blender Markers.", this);
                    return;
                }
            }
        }
    }
}
