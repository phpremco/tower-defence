using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class StoryDataTests
    {
        private const string StoryFolder = "Assets/_Project/Data/Story";
        private const string LevelFolder = "Assets/_Project/Data/Levels";
        private const string LandingPath = LevelFolder + "/Level_C1_01.asset";

        private static IEnumerable<T> AssetsOf<T>(string folder) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        [Test]
        public void EverySpeakerAndDialogue_PassesValidation()
        {
            int speakers = 0;
            foreach (SpeakerDefinition speaker in AssetsOf<SpeakerDefinition>(StoryFolder))
            {
                Assert.IsTrue(speaker.IsValid(out string error), $"{speaker.name}: {error}");
                speakers++;
            }

            int dialogues = 0;
            foreach (DialogueDefinition dialogue in AssetsOf<DialogueDefinition>(StoryFolder))
            {
                var errors = new List<string>();
                Assert.IsTrue(dialogue.Validate(errors), $"{dialogue.name}: {string.Join(" ", errors)}");
                dialogues++;
            }

            Assert.GreaterOrEqual(speakers, 5, "run Tools/Generate/Scenes and Starter Data");
            Assert.GreaterOrEqual(dialogues, 1);
        }

        [Test]
        public void EveryLevelTrigger_HasTheFieldsItsKindNeeds()
        {
            foreach (LevelDefinition level in AssetsOf<LevelDefinition>(LevelFolder))
            {
                StoryTrigger[] triggers = level.StoryTriggers;
                for (int i = 0; i < triggers.Length; i++)
                {
                    Assert.IsNull(triggers[i].Problem(), $"{level.name} trigger {i + 1} ({triggers[i].Kind})");
                }
            }
        }

        [Test]
        public void FirstLevel_OpensWithADialogue_AndHasOneForTheWin()
        {
            var landing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LandingPath);
            Assert.IsNotNull(landing);
            var kinds = new HashSet<StoryTriggerKind>();
            foreach (StoryTrigger trigger in landing.StoryTriggers)
            {
                kinds.Add(trigger.Kind);
            }

            Assert.IsTrue(kinds.Contains(StoryTriggerKind.LevelStart));
            Assert.IsTrue(kinds.Contains(StoryTriggerKind.LevelWon));
        }
    }
}
