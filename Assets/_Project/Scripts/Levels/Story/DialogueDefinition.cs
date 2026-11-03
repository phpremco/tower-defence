using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>A conversation: lines played in order. The asset name is the id under which the save remembers it.</summary>
    [CreateAssetMenu(menuName = "Game/Story/Dialogue", fileName = "Dlg_")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField] private DialogueLine[] _lines = new DialogueLine[0];

        public string Id => name;
        public IReadOnlyList<DialogueLine> Lines => _lines;

        /// <summary>Adds every data problem to the list; returns true when there is none.</summary>
        public bool Validate(List<string> errors)
        {
            if (_lines == null || _lines.Length == 0)
            {
                errors.Add("has no lines.");
                return false;
            }

            int before = errors.Count;
            var ids = new HashSet<string>();
            for (int i = 0; i < _lines.Length; i++)
            {
                ValidateLine(_lines[i], i + 1, ids, errors);
            }

            return errors.Count == before;
        }

        private static void ValidateLine(DialogueLine line, int number, HashSet<string> ids, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(line.Id))
            {
                errors.Add($"line {number} has no id.");
            }
            else if (!ids.Add(line.Id))
            {
                errors.Add($"line {number} repeats the id '{line.Id}'.");
            }

            if (line.Speaker == null)
            {
                errors.Add($"line {number} has no speaker.");
            }

            if (line.Segments == null || line.Segments.Length == 0)
            {
                errors.Add($"line {number} has no text.");
                return;
            }

            bool hasVisibleText = false;
            for (int s = 0; s < line.Segments.Length; s++)
            {
                DialogueSegment segment = line.Segments[s];
                // An empty segment would give a redacted bar or a strike line of zero width.
                if (segment.Length == 0)
                {
                    errors.Add($"line {number} segment {s + 1} is empty.");
                }

                hasVisibleText |= !string.IsNullOrWhiteSpace(segment.Text);
            }

            if (!hasVisibleText)
            {
                errors.Add($"line {number} has no text.");
            }
        }

        private void OnValidate()
        {
            var errors = new List<string>();
            if (!Validate(errors))
            {
                Debug.LogWarning($"Dialogue '{name}': {string.Join(" ", errors)}", this);
            }
        }
    }
}
