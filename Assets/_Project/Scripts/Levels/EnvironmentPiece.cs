using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One kit piece of an environment set: what it is for and its mesh.</summary>
    [Serializable]
    public sealed class EnvironmentPiece
    {
        [SerializeField] private EnvironmentPieceKind _kind;
        [SerializeField] private EnvironmentMesh _model;

        public EnvironmentPiece(EnvironmentPieceKind kind, EnvironmentMesh model)
        {
            _kind = kind;
            _model = model;
        }

        public EnvironmentPieceKind Kind => _kind;
        public EnvironmentMesh Model => _model;
    }
}
