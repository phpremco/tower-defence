using System;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// A dead imported creature: a pooled copy of its model takes the pose and place of the body at the moment it
    /// died and plays the death clip, held on its last frame, while its glow dies away over the frames the art names.
    /// A flyer falls to the ground as it plays (the clip rolls it, so nothing else spins it); the body lies a moment
    /// and then sinks into the ground. Ichor splashes where it fell. Pure view code, pooled per model by
    /// <see cref="EnemyDeathEffectPool"/>.
    /// </summary>
    public sealed class ModelCorpse : MonoBehaviour
    {
        private const float Gravity = 9.5f;
        private const float RestSeconds = 0.7f;
        private const float SinkSeconds = 0.8f;
        private const float SinkDepth = 0.6f;
        private const float SlideFriction = 3f;
        private const int SplashBase = 10;
        private const int SplashPerSize = 10;
        private const float SplashBrightness = 1.6f;

        private CreatureModel _model;
        private CreatureClips _clips;
        private ParticleSystem _splash;
        private Action<ModelCorpse> _release;
        private EnemyDeathEffect.Request _request;
        private float _scale;
        private float _age;
        private float _clipSeconds;
        private float _fallSpeed;
        private Vector3 _slide;
        private bool _landed;

        public static ModelCorpse Create(Transform parent, ModelArtDefinition art, float scale, bool lowDetail)
        {
            var go = new GameObject($"{art.name}_corpse");
            go.transform.SetParent(parent, false);
            var corpse = go.AddComponent<ModelCorpse>();
            corpse._scale = scale;
            corpse._model = CreatureModel.Create(go.transform, art, scale, lowDetail);
            corpse._clips = new CreatureClips(corpse._model.Player, art, Array.Empty<string>());
            int death = art.IndexOf(ModelClipRole.Death);
            corpse._clipSeconds = death >= 0 ? art.Clips[death].Length : 0f;
            return corpse;
        }

        /// <summary>Lays the corpse where the body was, facing the way it faced, and starts the fall.</summary>
        public void Play(Vector3 position, Quaternion rotation, float scale, EnemyDeathEffect.Request request, Action<ModelCorpse> release)
        {
            _request = request;
            _release = release;
            _age = 0f;
            _fallSpeed = 0f;
            _landed = position.y <= request.GroundY + 0.01f;
            _slide = new Vector3(request.Momentum.x, 0f, request.Momentum.z);
            transform.SetPositionAndRotation(position, rotation);
            _model.Root.localScale = Vector3.one * scale;
            _model.SetGlow(1f);
            _model.SetOverlay(Color.white, 0f);
            _clips.Restart();
            _clips.Die();
            _model.Player.Evaluate();
            Splash(position);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            _clips.Tick(dt, 0f, false, false, _scale);
            FadeGlow();
            Fall(dt);
            float lieUntil = _clipSeconds + RestSeconds;
            if (_age <= lieUntil)
            {
                return;
            }

            float sink = Mathf.Clamp01((_age - lieUntil) / SinkSeconds);
            Vector3 position = transform.position;
            position.y = _request.GroundY - sink * sink * SinkDepth * _scale;
            transform.position = position;
            if (sink >= 1f)
            {
                _release?.Invoke(this);
            }
        }

        private void FadeGlow()
        {
            Vector2 fade = _model.Art.DeathFade;
            if (!_model.Art.FadesOnDeath)
            {
                return;
            }

            float t = Mathf.Clamp01((_age - fade.x) / (fade.y - fade.x));
            // Ease out: the glow drops quickly at first and lingers faintly at the end.
            float left = 1f - t;
            _model.SetGlow(left * left);
        }

        // A flyer drops out of the air under gravity; a walker slides a little with its last step.
        private void Fall(float dt)
        {
            Vector3 position = transform.position;
            position += _slide * dt;
            _slide = Vector3.MoveTowards(_slide, Vector3.zero, SlideFriction * dt);
            if (!_landed)
            {
                _fallSpeed += Gravity * dt;
                position.y -= _fallSpeed * dt;
                if (position.y <= _request.GroundY)
                {
                    position.y = _request.GroundY;
                    _landed = true;
                    Splash(position);
                }
            }

            transform.position = position;
        }

        private void Splash(Vector3 at)
        {
            if (_splash == null)
            {
                _splash = ParticleKit.Create(transform, "Splash", Vector3.zero, Color.white, 0.45f, 0.9f, 0.05f, 0.13f, 1.5f, 3.5f, 1.6f, 80, 0.2f);
            }

            ParticleSystem.MainModule main = _splash.main;
            Color glow = _request.Blood * SplashBrightness;
            glow.a = 1f;
            main.startColor = glow;
            _splash.transform.position = at + Vector3.up * (0.2f * _scale);
            _splash.Emit(SplashBase + Mathf.RoundToInt(SplashPerSize * _request.Size));
        }

        private void OnDestroy() => _model?.Dispose();
    }
}
