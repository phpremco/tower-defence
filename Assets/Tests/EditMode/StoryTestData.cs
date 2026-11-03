using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Builds in-memory speakers, dialogues and enemies for the story tests and destroys them afterwards.</summary>
    internal sealed class StoryTestData : System.IDisposable
    {
        private readonly List<Object> _created = new List<Object>();

        public SpeakerDefinition Speaker()
        {
            var speaker = Track(ScriptableObject.CreateInstance<SpeakerDefinition>());
            speaker.name = "Speaker_Test";
            return speaker;
        }

        /// <summary>A dialogue with one line per text, all spoken by one speaker.</summary>
        public DialogueDefinition Dialogue(string id, params string[] texts)
        {
            SpeakerDefinition speaker = Speaker();
            var dialogue = Track(ScriptableObject.CreateInstance<DialogueDefinition>());
            dialogue.name = id;
            var so = new SerializedObject(dialogue);
            SerializedProperty lines = so.FindProperty("_lines");
            lines.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                SerializedProperty line = lines.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("Id").stringValue = $"{id}.{i + 1:00}";
                line.FindPropertyRelative("Speaker").objectReferenceValue = speaker;
                SerializedProperty segments = line.FindPropertyRelative("Segments");
                segments.arraySize = 1;
                segments.GetArrayElementAtIndex(0).FindPropertyRelative("Text").stringValue = texts[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return dialogue;
        }

        public EnemyDefinition Enemy(string id)
        {
            var enemy = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            enemy.name = id;
            return enemy;
        }

        public void Dispose()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
