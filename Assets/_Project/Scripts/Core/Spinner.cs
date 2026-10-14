using UnityEngine;

namespace Game.Core
{
    /// <summary>Turns its object steadily around the vertical axis: rotors, radar rings, portals. Pure view code.</summary>
    public sealed class Spinner : MonoBehaviour
    {
        private float _degreesPerSecond;

        public void Initialize(float degreesPerSecond) => _degreesPerSecond = degreesPerSecond;

        private void Update() => transform.Rotate(0f, _degreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}
