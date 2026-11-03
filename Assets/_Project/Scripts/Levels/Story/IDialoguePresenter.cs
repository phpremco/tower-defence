namespace Game.Levels
{
    /// <summary>
    /// What draws a dialogue. The <see cref="DialoguePlayer"/> owns the state (queue, current line, typewriter) and
    /// tells the presenter what to show; the presenter never decides when to advance. Mouse buttons on the box call
    /// <see cref="DialoguePlayer.Advance"/> and <see cref="DialoguePlayer.SkipConversation"/>; keyboard and gamepad
    /// are bound by <see cref="DialogueInput"/>, so a presenter must not listen to Confirmed or Cancelled itself.
    /// </summary>
    public interface IDialoguePresenter
    {
        /// <summary>
        /// A conversation begins: show the box. When queued conversations follow on, this comes again without a
        /// <see cref="Close"/> in between, so the box stays up.
        /// </summary>
        void Open(DialogueDefinition dialogue);

        /// <summary>A new line begins with nothing revealed yet. Lay out the full text once so it never jumps.</summary>
        void ShowLine(DialogueLine line, int lineIndex);

        /// <summary>How many characters of the current line are visible; <paramref name="complete"/> once all are.</summary>
        void Reveal(int visibleCharacters, bool complete);

        /// <summary>The conversation is over, finished or skipped: hide the box.</summary>
        void Close();
    }
}
