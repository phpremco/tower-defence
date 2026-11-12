using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// Shows what a boss does to a structure without moving the structure itself: a tower flung up that lands tilted and
    /// rights itself when its upset time runs out, and a gate that sags under the weight of a boss standing on it. The
    /// whole look (model, stars, bar, smoke) is lifted onto a pivot only while it is displaced and handed back after, so
    /// the tower's logic position and everything that rebuilds its look stay untouched.
    /// </summary>
    public sealed class TowerDisplacementView : MonoBehaviour
    {
        private const float AirSeconds = 1.1f;
        private const float ArcHeight = 2.6f;
        private const float SpinTurns = 1f;
        private const float LandTiltDegrees = 38f;
        private const float RightSeconds = 0.6f;
        private const float SettleWobbleDegrees = 3f;
        private const float SettleSpeed = 9f;
        private const float SagSquash = 0.16f;
        private const float SagDrop = 0.12f;
        private const float SagWobbleDegrees = 1.6f;
        private const float SagWobbleSpeed = 5f;
        private const int LandDust = 26;
        private static readonly Color DustColour = new Color(0.42f, 0.36f, 0.3f, 0.7f);

        private readonly List<Transform> _moved = new List<Transform>(16);
        private Tower _tower;
        private Transform _pivot;
        private ParticleSystem _dust;
        private Vector3 _axis = Vector3.right;
        private float _tossAge = float.PositiveInfinity;
        private bool _landed;

        private void Awake()
        {
            _tower = GetComponent<Tower>();
            _tower.Tossed += OnTossed;
            _tower.BurdenChanged += OnBurdenChanged;
        }

        private void OnDestroy()
        {
            _tower.Tossed -= OnTossed;
            _tower.BurdenChanged -= OnBurdenChanged;
        }

        private void OnTossed(Tower tower, Vector3 away, float seconds)
        {
            away.y = 0f;
            // It tips over away from the boss: the turn runs about the horizontal axis across the throw.
            _axis = away.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, away.normalized) : Vector3.right;
            _tossAge = 0f;
            _landed = false;
            Lift();
        }

        private void OnBurdenChanged(Tower tower)
        {
            if (tower.Burden > 0f)
            {
                Lift();
            }
        }

        private bool Tossing => _tossAge < float.PositiveInfinity;

        private void LateUpdate()
        {
            if (_pivot == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            Quaternion turn = Quaternion.identity;
            Vector3 offset = Vector3.zero;
            float squash = 1f;
            if (Tossing)
            {
                _tossAge += dt;
                TossPose(out turn, out offset);
            }

            float burden = _tower.Burden;
            if (burden > 0f)
            {
                squash = 1f - SagSquash * burden;
                offset += Vector3.down * (SagDrop * burden);
                float wobble = Mathf.Sin(Time.time * SagWobbleSpeed) * SagWobbleDegrees * burden;
                turn *= Quaternion.AngleAxis(wobble, Vector3.forward);
            }

            _pivot.localPosition = offset;
            _pivot.localRotation = turn;
            _pivot.localScale = new Vector3(1f, squash, 1f);
            if (!Tossing && burden <= 0f)
            {
                Lower();
            }
        }

        // Up in an arc with one tumble, a thump down at a tilt, a small settle, and upright again at the end.
        private void TossPose(out Quaternion turn, out Vector3 offset)
        {
            Vector3 localAxis = transform.InverseTransformDirection(_axis);
            if (_tossAge < AirSeconds)
            {
                float t = _tossAge / AirSeconds;
                offset = Vector3.up * (4f * ArcHeight * t * (1f - t));
                turn = Quaternion.AngleAxis(Mathf.Lerp(0f, 360f * SpinTurns + LandTiltDegrees, t), localAxis);
                return;
            }

            if (!_landed)
            {
                _landed = true;
                Dust();
            }

            offset = Vector3.zero;
            float remaining = _tower.UpsetRemaining;
            float settle = Mathf.Exp(-(_tossAge - AirSeconds) * 2f) * Mathf.Sin((_tossAge - AirSeconds) * SettleSpeed) * SettleWobbleDegrees;
            float tilt = LandTiltDegrees + settle;
            if (remaining < RightSeconds)
            {
                tilt *= Mathf.SmoothStep(0f, 1f, remaining / RightSeconds);
            }

            turn = Quaternion.AngleAxis(tilt, localAxis);
            if (remaining <= 0f)
            {
                _tossAge = float.PositiveInfinity;
            }
        }

        private void Dust()
        {
            if (_dust == null)
            {
                _dust = ParticleKit.UseSmoke(ParticleKit.Create(transform, "TossDust", Vector3.up * 0.1f, DustColour, 0.6f, 1.3f, 0.25f, 0.6f, 1.2f, 2.6f, 0.2f, 60, 0.5f));
                ParticleKit.SetRate(_dust, 0f);
            }

            _dust.Emit(LandDust);
        }

        // Every part of the look moves onto the pivot while the structure is displaced.
        private void Lift()
        {
            if (_pivot != null)
            {
                return;
            }

            _pivot = new GameObject("Displaced").transform;
            _pivot.SetParent(transform, false);
            _moved.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != _pivot && (_dust == null || child != _dust.transform))
                {
                    _moved.Add(child);
                }
            }

            foreach (Transform child in _moved)
            {
                child.SetParent(_pivot, false);
            }
        }

        // Back where the view keeps them, so anything rebuilt later lands in the usual place.
        private void Lower()
        {
            foreach (Transform child in _moved)
            {
                if (child != null)
                {
                    child.SetParent(transform, false);
                }
            }

            _moved.Clear();
            Destroy(_pivot.gameObject);
            _pivot = null;
        }
    }
}
