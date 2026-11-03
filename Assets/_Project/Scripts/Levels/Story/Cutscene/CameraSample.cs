using UnityEngine;

namespace Game.Levels
{
    /// <summary>The camera at one moment: where it is, what it looks at and how wide it sees.</summary>
    public readonly struct CameraSample
    {
        public CameraSample(Vector3 position, Vector3 lookAt, float fieldOfView)
        {
            Position = position;
            LookAt = lookAt;
            FieldOfView = fieldOfView;
        }

        public Vector3 Position { get; }
        public Vector3 LookAt { get; }
        public float FieldOfView { get; }
    }
}
