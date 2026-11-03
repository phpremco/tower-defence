namespace Game.Editor
{
    /// <summary>
    /// A story trigger as a level spec writes it, resolved by the generator into the S1 <c>StoryTrigger</c>. Names instead
    /// of engine enums keep the spec pure: <see cref="Kind"/> and <see cref="Repeat"/> are <c>StoryTriggerKind</c> and
    /// <c>StoryRepeat</c> names, the dialogue and enemy are asset names. A name that resolves to nothing fails the step.
    /// </summary>
    public sealed class StorySpec
    {
        public string Kind = "LevelStart";
        public int Wave;
        public string Enemy;

        /// <summary>FirstUse: the path object id (asset name) whose first placement starts the dialogue.</summary>
        public string PathObject;
        public string Dialogue = string.Empty;
        public string Repeat = "OncePerSave";
    }
}
