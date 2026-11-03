using System;
using Game.Enemies;
using Game.PathObjects;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A dialogue tied to a moment in a level. Plain fields read by kind, so the data stays flat and IL2CPP-plain;
    /// fields a kind does not use are ignored.
    /// </summary>
    [Serializable]
    public struct StoryTrigger
    {
        public StoryTriggerKind Kind;

        /// <summary>WaveStart: the wave number as the HUD shows it, starting at 1.</summary>
        [Min(0)] public int Wave;

        /// <summary>FirstSighting: the creature to spot. BossSpawn and BossDefeated: optional, empty means any boss.</summary>
        public EnemyDefinition Enemy;

        /// <summary>FirstUse: the path object (gadget) whose first placement starts the dialogue.</summary>
        public PathObjectDefinition PathObject;

        public DialogueDefinition Dialogue;
        public StoryRepeat Repeat;

        /// <summary>Null when the trigger has every field its kind needs, otherwise what is missing.</summary>
        public string Problem()
        {
            if (Dialogue == null)
            {
                return "has no dialogue.";
            }

            switch (Kind)
            {
                case StoryTriggerKind.WaveStart when Wave < 1:
                    return "is a wave start without a wave number (1 or more).";
                case StoryTriggerKind.FirstSighting when Enemy == null:
                    return "is a first sighting without a creature.";
                case StoryTriggerKind.FirstUse when PathObject == null:
                    return "is a first use without a path object.";
                case StoryTriggerKind.BossSpawn when Enemy != null && !Enemy.IsBoss:
                case StoryTriggerKind.BossDefeated when Enemy != null && !Enemy.IsBoss:
                    return $"names '{Enemy.name}' as its boss, which is not a boss.";
                default:
                    return null;
            }
        }
    }
}
