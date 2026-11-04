using System;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// One export the menu places: a static mesh drawn instanced, or a rigged model (a tower, the beacon) that is
    /// instantiated and held in the first frame of its <c>Pose</c> clip.
    /// </summary>
    [Serializable]
    public sealed class MenuDioramaModel
    {
        /// <summary>The far LOD mesh of a tier file (renamed at import); the menu hides it.</summary>
        public const string FarSuffix = "_Far";

        [SerializeField] private string _name;
        [SerializeField] private EnvironmentMesh _mesh;
        [SerializeField] private GameObject _rigged;
        [SerializeField] private Material _riggedMaterial;
        [SerializeField] private Material _riggedLowMaterial;
        [SerializeField] private AnimationClip _pose;
        [Tooltip("Its glow joins the island's slow pulse (the hive mouth).")]
        [SerializeField] private bool _pulses;

        public MenuDioramaModel(string name, EnvironmentMesh mesh, GameObject rigged, Material riggedMaterial, Material riggedLowMaterial, AnimationClip pose, bool pulses)
        {
            _name = name;
            _mesh = mesh;
            _rigged = rigged;
            _riggedMaterial = riggedMaterial;
            _riggedLowMaterial = riggedLowMaterial;
            _pose = pose;
            _pulses = pulses;
        }

        public string Name => _name;
        public EnvironmentMesh Mesh => _mesh;
        public GameObject Rigged => _rigged;
        public AnimationClip Pose => _pose;
        public bool Pulses => _pulses;
        public bool IsRigged => _rigged != null && _riggedMaterial != null;
        public Material RiggedMaterial(bool low) => low && _riggedLowMaterial != null ? _riggedLowMaterial : _riggedMaterial;
    }
}
