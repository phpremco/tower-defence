using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.PathObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Starts a level behind the loading screen (round 64). Loading the Level scene directly built the whole level in one
    /// frame, a freeze of seconds on the map. Here the screen fades in first, the scene is loaded in the background once
    /// it covers everything, and the level's bootstrapper, which finds the screen as <see cref="ILevelLoadProgress"/>,
    /// builds the level a step a frame and tells the screen when it stands. Every way into a level that is not a cut of
    /// its own (the map, "play again", a restart, a practice fight) goes through here.
    /// </summary>
    public static class LevelLoader
    {
        /// <summary>The scene change's share of the bar; the level's build reports the rest.</summary>
        private const float SceneShare = 0.03f;

        /// <summary>True while a level loads behind the screen: a second request is ignored.</summary>
        public static bool Busy => Services.TryGet(out ILevelLoadProgress _);

        /// <summary>A campaign level from the map, with its briefing.</summary>
        public static void Begin(LevelDefinition level, DifficultyProfile difficulty, IReadOnlyList<PathObjectDefinition> loadout) =>
            BeginSession(new LevelSession(level, difficulty, true, loadout));

        /// <summary>The level the session describes.</summary>
        public static void BeginSession(LevelSession session)
        {
            if (Busy || session == null)
            {
                return;
            }

            StoryDefinition story = Services.TryGet(out CampaignCatalog campaign) ? campaign.Story : null;
            LoadingScreen screen = LoadingScreen.Show(session.Level, session.Difficulty, LoadingTips.For(story, session.Level, session.Difficulty), LoadingTips.AboutLevel(story, session.Level, session.Difficulty), PlaceOf(campaign, session.Level));
            screen.StartCoroutine(Load(screen, session));
        }

        // The continent the level lies on, from the map's own data; empty for a level that is not on the map (a practice arena).
        private static string PlaceOf(CampaignCatalog campaign, LevelDefinition level)
        {
            if (campaign == null)
            {
                return string.Empty;
            }

            foreach (MapNode node in campaign.Nodes)
            {
                if (node.Level == level && node.Region >= 0 && node.Region < campaign.Regions.Count)
                {
                    return campaign.Regions[node.Region].DisplayName;
                }
            }

            return string.Empty;
        }

        /// <summary>The level that is registered once more (play again, restart); without one, nothing.</summary>
        public static void Reload()
        {
            if (Services.TryGet(out LevelSession session))
            {
                BeginSession(session);
            }
        }

        private static IEnumerator Load(LoadingScreen screen, LevelSession session)
        {
            while (!screen.Covered)
            {
                yield return null;
            }

            // A paused or ended level left the clock stopped; the new level starts on a running one.
            Time.timeScale = 1f;
            Services.Register(session);
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneNames.Level);
            while (load != null && !load.isDone)
            {
                screen.Report(load.progress * SceneShare);
                yield return null;
            }
        }
    }
}
