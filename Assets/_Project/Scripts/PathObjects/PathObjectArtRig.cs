using Game.Core;
using UnityEngine;

namespace Game.PathObjects
{
    /// <summary>
    /// The imported (Blender) model of a placed path object: it sets itself up when placed (the mine arms, the emitter
    /// unfolds, the ball drops), then idles; a roller turns the way it travels, drives its wheels or rolls its ball by the
    /// distance covered. Plain class; <see cref="PathObjectView"/>
    /// ticks it from the object's position, so it never reads or changes the object's logic.
    /// </summary>
    public sealed class PathObjectArtRig
    {
        private const string BallBone = "Ball";
        private const float TurnSpeed = 10f;
        private const float MinStep = 0.0001f;

        private readonly ArtModel _model;
        private readonly Transform _holder;
        private readonly Transform _ball;
        private readonly Quaternion _ballRest;
        private readonly float _ballRadius;
        private readonly int _idle;
        private readonly int _move;
        private Vector3 _last;
        private float _roll;
        private int _cycle = -1;

        public PathObjectArtRig(Transform parent, PathObjectDefinition definition)
        {
            _holder = new GameObject("Model").transform;
            _holder.SetParent(parent, false);
            _model = ArtModel.Create(_holder, definition.Art, ownMaterial: false);
            ModelArtDefinition art = definition.Art;
            _idle = art.IndexOf(ModelClipRole.Idle);
            _move = art.IndexOf(ModelClipRole.Move);
            _ball = _model.Find(BallBone);
            if (_ball != null)
            {
                _ballRest = _ball.localRotation;
                _ballRadius = Mathf.Max(MinStep, _ball.position.y - parent.position.y);
            }

            _model.Player?.Loop(_idle, 1f);
            _model.Player?.PlayOnce(art.IndexOf(ModelClipRole.Deploy));
            _last = parent.position;
        }

        /// <summary>Follows the object: a roller faces and drives the way it moved since the last frame.</summary>
        public void Tick(Vector3 position, float deltaTime)
        {
            Vector3 step = position - _last;
            step.y = 0f;
            _last = position;
            float distance = step.magnitude;
            bool moving = distance > MinStep * deltaTime;
            if (moving)
            {
                _holder.rotation = Quaternion.Slerp(_holder.rotation, Quaternion.LookRotation(step / distance), Mathf.Clamp01(TurnSpeed * deltaTime));
                _roll += distance / Mathf.Max(MinStep, _ballRadius);
            }

            ModelClipPlayer player = _model.Player;
            if (player == null)
            {
                return;
            }

            int wanted = moving && _move >= 0 ? _move : _idle;
            if (wanted != _cycle)
            {
                _cycle = wanted;
                player.Loop(wanted, 1f);
            }

            player.Tick(deltaTime);
        }

        // A rolling ball turns about the axis across its travel by the distance over its radius, whatever its clips did.
        public void LateTick()
        {
            if (_ball == null)
            {
                return;
            }

            _ball.rotation = Quaternion.AngleAxis(_roll * Mathf.Rad2Deg, _holder.right) * (_ball.parent.rotation * _ballRest);
        }

        public void Dispose() => _model.Dispose();

    }
}
