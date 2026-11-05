using System.Collections.Generic;
using Game.Enemies;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// How the hero meets one continent boss (Docs/BOSS_INTROS.md). The first time: a short slow-motion beat in which he
    /// recoils and says his line over his head while a visor cut-in slides in, then ORA and Dr. Voss answer in the
    /// dialogue box. Later fights only get a short bark from a pool; during the fight he calls out its signature moments,
    /// with a cooldown so it never spams. All of it is data, so the beat can be tuned without code.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Story/Boss Intro", fileName = "Intro_")]
    public sealed class BossIntroDefinition : ScriptableObject
    {
        // The beat must stay short (BOSS_INTROS: "Total under 6 s").
        public const float MaxBeatLimit = 6f;

        [Tooltip("The continent boss this intro belongs to.")]
        [SerializeField] private EnemyDefinition _boss;
        [Tooltip("Mercer's line over his head when he first sees it.")]
        [SerializeField] [TextArea(1, 2)] private string _heroLine = string.Empty;
        [Tooltip("The answer in the dialogue box: ORA first, then Dr. Voss. Auto-advances.")]
        [SerializeField] private DialogueDefinition _answer;
        [Tooltip("What Mercer says when he meets it again (one picked at random); no slow motion then.")]
        [SerializeField] private string[] _repeatBarks = new string[0];
        [Tooltip("What Mercer calls out at its signature moments during the fight.")]
        [SerializeField] private BossBark[] _barks = new BossBark[0];

        [Header("The beat")]
        [Tooltip("Rim light and frame colour of the visor cut-in: warm for a fire boss, cold for an ice boss.")]
        [SerializeField] private Color _cutInTint = new Color(1f, 0.55f, 0.2f);
        [Tooltip("Game speed during the beat (it slows down, it never pauses).")]
        [SerializeField] [Range(0.05f, 1f)] private float _slowFactor = 0.25f;
        [Tooltip("Real seconds the camera takes to frame the boss and the hero.")]
        [SerializeField] [Min(0f)] private float _focusSeconds = 0.5f;
        [Tooltip("Real seconds his line stays over his head before the answer starts.")]
        [SerializeField] [Min(0.1f)] private float _heroLineSeconds = 1.4f;
        [Tooltip("Real seconds an answer line stays once it has typed out.")]
        [SerializeField] [Min(0f)] private float _lineHoldSeconds = 0.6f;
        [Tooltip("Characters per second the answer types at (faster than story dialogue: the beat is short).")]
        [SerializeField] [Min(10f)] private float _typeSpeed = 75f;
        [Tooltip("Real seconds after which the beat ends whatever is still showing.")]
        [SerializeField] [Range(1f, MaxBeatLimit)] private float _maxBeatSeconds = 5.8f;

        [Header("Barks")]
        [Tooltip("Game seconds before this boss may draw another bark.")]
        [SerializeField] [Min(0f)] private float _barkCooldownSeconds = 14f;

        public EnemyDefinition Boss => _boss;
        public string HeroLine => _heroLine;
        public DialogueDefinition Answer => _answer;
        public IReadOnlyList<string> RepeatBarks => _repeatBarks ?? NoLines;
        public IReadOnlyList<BossBark> Barks => _barks ?? NoBarks;
        public Color CutInTint => _cutInTint;
        public float SlowFactor => _slowFactor;
        public float FocusSeconds => _focusSeconds;
        public float HeroLineSeconds => _heroLineSeconds;
        public float LineHoldSeconds => _lineHoldSeconds;
        public float TypeSpeed => _typeSpeed;
        public float MaxBeatSeconds => _maxBeatSeconds;
        public float BarkCooldownSeconds => _barkCooldownSeconds;

        private static readonly string[] NoLines = new string[0];
        private static readonly BossBark[] NoBarks = new BossBark[0];

        /// <summary>The line for a moment, or null when this boss has none for it.</summary>
        public string BarkFor(BossMoment moment)
        {
            foreach (BossBark bark in Barks)
            {
                if (bark.Moment == moment && !string.IsNullOrWhiteSpace(bark.Line))
                {
                    return bark.Line;
                }
            }

            return null;
        }

        /// <summary>Adds every data problem to the list; returns true when there is none.</summary>
        public bool Validate(List<string> errors)
        {
            int before = errors.Count;
            if (_boss == null || _boss.Boss == null)
            {
                errors.Add("names no continent boss.");
            }

            if (string.IsNullOrWhiteSpace(_heroLine))
            {
                errors.Add("has no line for the hero.");
            }

            if (_answer == null)
            {
                errors.Add("has no answer dialogue.");
            }

            if (_repeatBarks == null || _repeatBarks.Length == 0)
            {
                errors.Add("has no bark for a later meeting.");
            }

            float shortest = _focusSeconds + _heroLineSeconds;
            if (shortest >= _maxBeatSeconds)
            {
                errors.Add($"its focus and hero line ({shortest:0.0} s) leave no time for the answer within {_maxBeatSeconds:0.0} s.");
            }

            return errors.Count == before;
        }

        private void OnValidate()
        {
            var errors = new List<string>();
            if (!Validate(errors))
            {
                Debug.LogWarning($"Boss intro '{name}': {string.Join(" ", errors)}", this);
            }
        }
    }
}
