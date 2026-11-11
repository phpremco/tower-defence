using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// When the dialogue box makes its sounds, as pure logic: the opener when a speaker starts talking (the first line in a
    /// fresh box, or another speaker's turn; never on closing), and one typing blip every few revealed letters or digits
    /// (spaces, punctuation and redacted bars do not count). At most one blip per reveal step, so a slow frame never bursts,
    /// and none once the line jumps to its end (the player confirmed mid-line). A paused dialogue reveals nothing, so it
    /// stays silent by itself.
    /// </summary>
    public sealed class TypingCadence
    {
        // A natural reveal moves a character or two per frame (45 per second); finishing with more than this at once is
        // the player's confirm showing the rest of the line.
        private const int MaxNaturalFinishStep = 3;
        private const int NoVariant = -1;

        private readonly int _blipEvery;
        private readonly System.Random _random;
        private DialogueLine _line;
        private bool _spoken;
        private SpeakerDefinition _lastSpeaker;
        private int _shown;
        private int _letters;
        private bool _stopped;
        private int _lastVariant = NoVariant;

        public TypingCadence(int blipEveryLetters, int seed)
        {
            _blipEvery = Mathf.Max(1, blipEveryLetters);
            _random = new System.Random(seed);
        }

        /// <summary>The box has closed: the next line, whoever speaks it, opens with the opener again.</summary>
        public void BoxClosed()
        {
            _spoken = false;
            _lastSpeaker = null;
        }

        /// <summary>A new line begins; true when its speaker starts talking now and the opener should play.</summary>
        public bool BeginLine(DialogueLine line)
        {
            bool opener = !_spoken || line.Speaker != _lastSpeaker;
            _spoken = true;
            _lastSpeaker = line.Speaker;
            _line = line;
            _shown = 0;
            _letters = 0;
            _stopped = false;
            return opener;
        }

        /// <summary>The reveal moved on; true when one blip should play now.</summary>
        public bool Reveal(int visible, bool complete)
        {
            if (_stopped || visible <= _shown)
            {
                _shown = Mathf.Max(_shown, visible);
                return false;
            }

            if (complete && visible - _shown > MaxNaturalFinishStep)
            {
                _stopped = true;
                _shown = visible;
                return false;
            }

            int before = _letters;
            for (int i = _shown; i < visible; i++)
            {
                if (Counts(i))
                {
                    _letters++;
                }
            }

            _shown = visible;
            return _letters / _blipEvery > before / _blipEvery;
        }

        /// <summary>A variant index in 0..count-1, never the one picked last (unless there is only one).</summary>
        public int NextVariant(int count)
        {
            if (count <= 1)
            {
                _lastVariant = 0;
                return 0;
            }

            int index = _random.Next(count - 1);
            if (_lastVariant != NoVariant && index >= _lastVariant)
            {
                index++;
            }

            _lastVariant = index;
            return index;
        }

        // A letter or digit of plain text; a redacted span shows bars, which make no sound.
        private bool Counts(int index)
        {
            DialogueSegment[] segments = _line.Segments;
            if (segments == null)
            {
                return false;
            }

            foreach (DialogueSegment segment in segments)
            {
                if (index < segment.Length)
                {
                    return segment.Style != SegmentStyle.Redacted && char.IsLetterOrDigit(segment.Text[index]);
                }

                index -= segment.Length;
            }

            return false;
        }
    }
}
