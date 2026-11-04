using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One Burrower eye from Blender (README round 26): the mesh (two sub-meshes, eye and cornea, pivot at the eyeball
    /// centre, looking along +Z), its materials per profile and the rig its <c>EyeRig</c> vertex colours drive: the
    /// lids turn about <see cref="Axis"/> by the blink degrees, the eyeball by the look degrees.
    /// </summary>
    [Serializable]
    public sealed class BurrowerEyeArt
    {
        [SerializeField] private string _name;
        [SerializeField] private Mesh _mesh;
        [Tooltip("The mesh node's transform inside its FBX (the axis change); put it under every placement.")]
        [SerializeField] private Matrix4x4 _node = Matrix4x4.identity;
        [SerializeField] private Material _eye;
        [SerializeField] private Material _lowEye;
        [Tooltip("The wet cornea over the iris; the Low profile leaves it out.")]
        [SerializeField] private Material _cornea;
        [SerializeField] private float _upperLidDegrees = 32f;
        [SerializeField] private float _lowerLidDegrees = 22f;
        [SerializeField] private float _lookDegrees = 22f;
        [SerializeField] private float _lookUpDownDegrees = 15f;
        [Tooltip("The lid hinge in the eye's own frame (Unity axes).")]
        [SerializeField] private Vector3 _axis = Vector3.left;

        public BurrowerEyeArt(string name, Mesh mesh, Matrix4x4 node, Material eye, Material lowEye, Material cornea, float upper, float lower, float look, float lookUpDown, Vector3 axis)
        {
            _name = name;
            _mesh = mesh;
            _node = node;
            _eye = eye;
            _lowEye = lowEye;
            _cornea = cornea;
            _upperLidDegrees = upper;
            _lowerLidDegrees = lower;
            _lookDegrees = look;
            _lookUpDownDegrees = lookUpDown;
            _axis = axis;
        }

        public string Name => _name;
        public Mesh Mesh => _mesh;
        public Matrix4x4 Node => _node;
        public Material Cornea => _cornea;
        public float UpperLidDegrees => _upperLidDegrees;
        public float LowerLidDegrees => _lowerLidDegrees;
        public float LookDegrees => _lookDegrees;
        public float LookUpDownDegrees => _lookUpDownDegrees;
        public Vector3 Axis => _axis.sqrMagnitude > 0.0001f ? _axis.normalized : Vector3.left;
        public bool IsValid => _mesh != null && _eye != null;

        public Material EyeFor(bool low) => low && _lowEye != null ? _lowEye : _eye;
    }
}
