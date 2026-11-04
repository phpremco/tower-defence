using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One imported environment mesh with its material per quality profile (High: 512 maps and detail, Low: no detail).</summary>
    [Serializable]
    public sealed class EnvironmentMesh
    {
        [SerializeField] private string _name;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;
        [SerializeField] private Material _lowMaterial;
        [Tooltip("Big pieces (walls, rocks, plants) throw a shadow; flat and tiny ones (pads, pebbles) do not.")]
        [SerializeField] private bool _castShadows = true;
        [Tooltip("The mesh node's transform inside its FBX (the axis change: Blender Z up becomes Unity Y up).")]
        [SerializeField] private Matrix4x4 _node = Matrix4x4.identity;

        public EnvironmentMesh(string name, Mesh mesh, Material material, Material lowMaterial, bool castShadows, Matrix4x4 node)
        {
            _name = name;
            _mesh = mesh;
            _material = material;
            _lowMaterial = lowMaterial;
            _castShadows = castShadows;
            _node = node;
        }

        /// <summary>The export the mesh came from, for messages.</summary>
        public string Name => _name;

        public Mesh Mesh => _mesh;
        public bool CastShadows => _castShadows;

        /// <summary>Mesh space to the piece's own frame (y up, pivot at the origin): put it under every placement.</summary>
        public Matrix4x4 Node => _node;
        public bool IsValid => _mesh != null && _material != null;

        public Material MaterialFor(bool low) => low && _lowMaterial != null ? _lowMaterial : _material;
    }
}
