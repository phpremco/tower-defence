using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Turns its object steadily around the vertical axis: rotors, radar rings, portals. Pure view code. A part of an
    /// imported model turns around world up (its own axes carry the FBX axis conversion, so its local Y is not vertical).
    /// </summary>
    public sealed class Spinner : MonoBehaviour
    {
        private float _degreesPerSecond;
        private bool _aroundWorldUp;

        public void Initialize(float degreesPerSecond) => Initialize(degreesPerSecond, false);

        /// <summary>Turns around world up through its pivot instead of its own Y axis (an imported model part).</summary>
        public void Initialize(float degreesPerSecond, bool aroundWorldUp)
        {
            _degreesPerSecond = degreesPerSecond;
            _aroundWorldUp = aroundWorldUp;
        }

        private void Update()
        {
            float degrees = _degreesPerSecond * Time.deltaTime;
            if (_aroundWorldUp)
            {
                transform.Rotate(Vector3.up, degrees, Space.World);
            }
            else
            {
                transform.Rotate(0f, degrees, 0f, Space.Self);
            }
        }
    }
}
