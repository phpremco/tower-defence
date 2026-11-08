using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// One jetpack flight of the hero from where he is to a post: a moment on the spot while the pack lifts him, then a
    /// straight run over the field at the flight speed on an arc that peaks in the middle, down to the post's stand.
    /// Plain logic with its own clock, so a test can fly it frame by frame; the commander moves his transform to
    /// <see cref="Position"/>. A flight started in the air (a new order while flying) skips the take-off.
    /// </summary>
    public sealed class HeroFlight
    {
        // A short hop between near gates stays a low hop: the arc never rises more than this per metre of ground.
        private const float MaxRisePerMetre = 0.3f;

        private readonly Vector3 _from;
        private readonly Vector3 _to;
        private readonly float _height;
        private readonly float _cruiseSeconds;
        private float _takeoffLeft;
        private float _cruiseAge;

        public HeroFlight(Vector3 from, Vector3 to, float speed, float takeoffSeconds, float height, bool airborne)
        {
            _from = from;
            _to = to;
            Vector3 flat = to - from;
            flat.y = 0f;
            Distance = flat.magnitude;
            _cruiseSeconds = speed > 0f ? Distance / speed : 0f;
            _takeoffLeft = airborne ? 0f : takeoffSeconds;
            _height = Mathf.Min(height, Distance * MaxRisePerMetre);
            Position = from;
            Heading = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
        }

        /// <summary>Ground distance from the start to the post.</summary>
        public float Distance { get; }

        /// <summary>Where the hero is now.</summary>
        public Vector3 Position { get; private set; }

        /// <summary>The way the flight goes on the ground plane.</summary>
        public Vector3 Heading { get; }

        /// <summary>True once the pack has lifted him (always for a flight started in the air).</summary>
        public bool IsAirborne => _takeoffLeft <= 0f;

        /// <summary>True when he has touched down at the post.</summary>
        public bool IsDone => IsAirborne && _cruiseAge >= _cruiseSeconds;

        /// <summary>Seconds left until he touches down at the post, the take-off included.</summary>
        public float SecondsToTouchdown => Mathf.Max(0f, _takeoffLeft) + Mathf.Max(0f, _cruiseSeconds - _cruiseAge);

        /// <summary>Advances the flight and returns where he is.</summary>
        public Vector3 Tick(float deltaTime)
        {
            if (_takeoffLeft > 0f)
            {
                _takeoffLeft -= deltaTime;
                if (_takeoffLeft > 0f)
                {
                    return Position;
                }

                // The rest of the frame already belongs to the flight.
                deltaTime = -_takeoffLeft;
            }

            _cruiseAge = Mathf.Min(_cruiseSeconds, _cruiseAge + deltaTime);
            float share = _cruiseSeconds > 0f ? _cruiseAge / _cruiseSeconds : 1f;
            Vector3 position = Vector3.Lerp(_from, _to, share);
            position.y += _height * Mathf.Sin(share * Mathf.PI);
            Position = position;
            return position;
        }
    }
}
