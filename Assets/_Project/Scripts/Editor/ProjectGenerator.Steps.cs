using System;
using Game.Core;

namespace Game.Editor
{
    public static partial class ProjectGenerator
    {
        /// <summary>One named part of the generator run.</summary>
        private readonly struct GeneratorStep
        {
            public GeneratorStep(string name, Action<GeneratorContext> run)
            {
                Name = name;
                Run = run;
            }

            public string Name { get; }
            public Action<GeneratorContext> Run { get; }
        }

        // The generator run, in order. A feature adds its own partial file with a step method and one line here, after
        // the steps whose results it reads from the context. Order matters: the waves need every level, tower, creature
        // and upgrade, and the scenes come last because creating a scene unloads assets nothing references yet.
        private static readonly GeneratorStep[] Steps =
        {
            new GeneratorStep("TextMeshPro essentials", _ => EnsureTextMeshProEssentials()),
            new GeneratorStep("Models", _ => CustomModelBuilder.BuildAll()),
            new GeneratorStep("Towers", GenerateCoreTowers),
            new GeneratorStep("Bestiary", c => c.Bestiary = CreateBestiary()),
            // The Blender models: synced from ArtSource, turned into materials and art data, and worn by the bestiary.
            new GeneratorStep("Creature art", _ => BlenderArtBuilder.ApplyToEnemies(BlenderArtBuilder.SyncAndBuildCreatures())),
            new GeneratorStep("Difficulties", GenerateDifficulties),
            new GeneratorStep("Hero", GenerateHero),
            new GeneratorStep("Hero perks", GenerateHeroPerks),
            new GeneratorStep("Roster", c => c.Roster = CreateRoster(c.Pulse, c.Cannon, c.Rocket)),
            new GeneratorStep("Path objects", GeneratePathObjects),
            new GeneratorStep("Drones", GenerateDrones),
            new GeneratorStep("Recipes", GenerateRecipes),
            new GeneratorStep("Retire old levels", _ => RetireOldLevels()),
            new GeneratorStep("Campaign levels", GenerateCampaignLevels),
            new GeneratorStep("Terrain", BakeAllTerrain),
            new GeneratorStep("Unlocks", GenerateUnlocks),
            new GeneratorStep("Playtest settings", c => c.Playtest = LoadOrCreate<PlaytestSettings>(DebugAssetPath)),
            new GeneratorStep("Tower looks", GenerateTowerLooks),
            new GeneratorStep("Upgrades", c => c.Upgrades = CreateUpgrades(c.Roster.Standard, c.Roster.Hybrids, c.Drones)),
            new GeneratorStep("Codex", c => c.Codex = CreateCodex(c.Recipes, c.Bestiary)),
            new GeneratorStep("UI skin", c => c.Skin = CreateSkin()),
            new GeneratorStep("Campaign", c => c.Campaign = CreateCampaign(c)),
            new GeneratorStep("Waves", c => GenerateCampaignWaves(c.Campaign, c.Upgrades, c.Unlocks, c.Bestiary)),
            new GeneratorStep("Globe", c => BakeGlobe(c.Campaign)),
            new GeneratorStep("Menu stage", c => CreateMenuStage(c.Levels, c.Bestiary)),
            new GeneratorStep("Story", c => CreateStory(c.Campaign)),
            new GeneratorStep("Level story", GenerateLevelStory),
            new GeneratorStep("Cutscene", GenerateCutscene),
            new GeneratorStep("Render pipeline", _ => ConfigureRenderPipeline()),
            new GeneratorStep("Sound library", _ => CreateSfxLibrary()),
            new GeneratorStep("Effect library", _ => CreateVfxLibrary()),
            new GeneratorStep("Scenes", GenerateScenes),
        };

        private static void RunSteps()
        {
            var context = new GeneratorContext();
            foreach (GeneratorStep step in Steps)
            {
                try
                {
                    step.Run(context);
                }
                catch (Exception exception)
                {
                    // Name the step, because a null field in the context usually means an earlier step was moved or dropped.
                    throw new InvalidOperationException($"Generator step '{step.Name}' failed.", exception);
                }
            }
        }
    }
}
