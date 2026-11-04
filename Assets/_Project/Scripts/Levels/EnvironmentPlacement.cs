using UnityEngine;

namespace Game.Levels
{
    /// <summary>One kit piece where the grid wants it: its kind, its pivot on the ground and its turn about the up axis.</summary>
    public readonly struct EnvironmentPlacement
    {
        public EnvironmentPlacement(EnvironmentPieceKind kind, Vector3 position, float yaw)
        {
            Kind = kind;
            Position = position;
            Yaw = yaw;
        }

        public EnvironmentPieceKind Kind { get; }
        public Vector3 Position { get; }
        public float Yaw { get; }

        public Matrix4x4 Matrix => Matrix4x4.TRS(Position, Quaternion.Euler(0f, Yaw, 0f), Vector3.one);
    }
}
