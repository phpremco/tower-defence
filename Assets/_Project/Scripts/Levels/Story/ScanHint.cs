using System;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// Dr. Voss's scan of a creature: the conversation that plays the first time it shows up in the player's campaign,
    /// naming the tower that counters it. The dialogue carries the Codex link (<see cref="DialogueDefinition.CodexEntry"/>).
    /// </summary>
    [Serializable]
    public struct ScanHint
    {
        public EnemyDefinition Enemy;
        public DialogueDefinition Dialogue;

        public ScanHint(EnemyDefinition enemy, DialogueDefinition dialogue)
        {
            Enemy = enemy;
            Dialogue = dialogue;
        }
    }
}
