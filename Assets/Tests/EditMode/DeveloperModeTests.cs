using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>Developer mode never reaches a release build, and the story viewer lists every piece of story in the data.</summary>
    public sealed class DeveloperModeTests
    {
        private static readonly string[] NoArguments = { "TowerDefense.exe" };
        private static readonly string[] DevArgument = { "TowerDefense.exe", "-DevMode" };

        [Test]
        public void AReleaseBuild_NeverTurnsItOn_WhateverTheDataOrTheCommandLineSay()
        {
            Assert.IsFalse(DeveloperMode.Resolve(true, DevArgument, isEditor: false, isDevelopmentBuild: false));
        }

        [Test]
        public void TheEditorOrADevelopmentBuild_FollowsTheDataFlagOrTheArgument()
        {
            Assert.IsTrue(DeveloperMode.Resolve(true, NoArguments, isEditor: true, isDevelopmentBuild: true), "data flag in the editor");
            Assert.IsTrue(DeveloperMode.Resolve(false, DevArgument, isEditor: false, isDevelopmentBuild: true), "argument on a development build");
            Assert.IsFalse(DeveloperMode.Resolve(false, NoArguments, isEditor: true, isDevelopmentBuild: true), "neither");
            Assert.IsFalse(DeveloperMode.Resolve(false, null, isEditor: true, isDevelopmentBuild: true), "no command line");
        }

        [Test]
        public void TheStoryIndex_ListsEveryDialogueAsset_TheOpeningCutscene_AndEveryBriefing()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            var index = new StoryIndex(catalog);

            var listed = new HashSet<DialogueDefinition>(index.Dialogues());
            foreach (string guid in AssetDatabase.FindAssets("t:DialogueDefinition", new[] { "Assets/_Project/Data/Story/Dialogues" }))
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsTrue(listed.Contains(dialogue), $"the story viewer misses {dialogue.name}");
            }

            Assert.AreEqual(StoryIndex.CutscenePageTitle, index.Pages[0].Title);
            Assert.AreEqual(StoryIndex.EntryKind.Cutscene, index.Pages[0].Entries[0].Kind);
            int briefings = 0;
            foreach (StoryIndex.Page page in index.Pages)
            {
                briefings += page.Entries.FindAll(e => e.Kind == StoryIndex.EntryKind.Briefing).Count;
            }

            Assert.Greater(briefings, 0, "no level briefing listed");
        }
    }
}
