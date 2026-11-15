using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Round 63 (Remco: "Spencer is totally still" while the ground shakes): how the commander braces against a tremor at
    /// full strength, laid procedurally over whatever pose he holds. The knees give (the hips sink so the feet stay planted),
    /// the torso leans in and sways on two slow beats that never line up, the arms come out from the body for balance and
    /// the head counters the sway, so his eyes stay on the ground he scans.
    /// </summary>
    [Serializable]
    public sealed class QuakeBrace
    {
        [Tooltip("How far each knee bends further at full strength (the thigh forward, the shin back twice as far).")]
        [SerializeField] [Range(0f, 40f)] private float _kneeDegrees = 14f;
        [Tooltip("How far the torso leans forward into the brace.")]
        [SerializeField] [Range(0f, 30f)] private float _leanDegrees = 7f;
        [Tooltip("The side-to-side sway of the torso at full strength.")]
        [SerializeField] [Range(0f, 30f)] private float _swayDegrees = 6f;
        [Tooltip("The two sway beats (Hz); unrelated rates, so the sway never repeats as a clean wobble.")]
        [SerializeField] [Min(0f)] private float _swayHertz = 1.1f;
        [SerializeField] [Min(0f)] private float _swayHertzSecond = 2.1f;
        [Tooltip("Share of the second, faster beat in the sway.")]
        [SerializeField] [Range(0f, 1f)] private float _swaySecondShare = 0.35f;
        [Tooltip("How far each arm comes out from the body for balance.")]
        [SerializeField] [Range(0f, 60f)] private float _armOutDegrees = 18f;
        [Tooltip("Share of the torso's sway the head turns back against, so the gaze stays steady.")]
        [SerializeField] [Range(0f, 1f)] private float _headCounter = 0.6f;

        public static QuakeBrace Default { get; } = new QuakeBrace();

        public float KneeDegrees => _kneeDegrees;
        public float LeanDegrees => _leanDegrees;
        public float SwayDegrees => _swayDegrees;
        public float SwayHertz => _swayHertz;
        public float SwayHertzSecond => _swayHertzSecond;
        public float SwaySecondShare => _swaySecondShare;
        public float ArmOutDegrees => _armOutDegrees;
        public float HeadCounter => _headCounter;

        /// <summary>The torso's sway at a moment, -1..1 before <see cref="SwayDegrees"/>: two beats that never line up.</summary>
        public float Sway(float time)
        {
            float first = Mathf.Sin(time * _swayHertz * Mathf.PI * 2f);
            float second = Mathf.Sin(time * _swayHertzSecond * Mathf.PI * 2f + 1.3f);
            return Mathf.Lerp(first, second, _swaySecondShare);
        }
    }
}
