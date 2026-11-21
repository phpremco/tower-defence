using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The clock of a lobbed bolt's burst (round 88c): the bolt switches itself off when it lands, so the shell that
    /// swells over the gun it hit runs on its own object and asks the bolt to draw it each frame.
    /// </summary>
    public sealed class BoltBurst : MonoBehaviour
    {
        private EnemyBoltView _bolt;

        public void Initialize(EnemyBoltView bolt) => _bolt = bolt;

        private void Update()
        {
            if (_bolt != null)
            {
                _bolt.DriveBurst(Time.deltaTime);
            }
        }
    }
}
