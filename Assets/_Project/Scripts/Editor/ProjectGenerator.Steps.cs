using System;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;

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
            // The Blender environment per continent (ground sets, kit, decor), worn by each campaign level's theme before
            // the terrain bake, so the bake sees the theme as it stays.
            new GeneratorStep("Environment art", c => BlenderEnvironmentBuilder.Apply(BlenderEnvironmentBuilder.SyncAndBuild(), ThemesByContinent(c))),
            // The pool surfaces and the ground mist from the Blender liquid textures (README round 24), on every theme.
            new GeneratorStep("Liquid art", _ => BlenderLiquidBuilder.ApplyBuilt(BlenderLiquidBuilder.SyncAndBuild())),
            new GeneratorStep("Terrain", BakeAllTerrain),
            new GeneratorStep("Unlocks", GenerateUnlocks),
            new GeneratorStep("Playtest settings", c => c.Playtest = LoadOrCreate<PlaytestSettings>(DebugAssetPath)),
            new GeneratorStep("Tower looks", GenerateTowerLooks),
            // The Blender tower models: synced, turned into materials and tier art, and worn by the tower definitions.
            new GeneratorStep("Tower art", _ => BlenderTowerBuilder.ApplyToTowers(BlenderTowerBuilder.SyncAndBuild())),
            // Our own side from Blender: the gate, the base, the resource node, the drones and the path objects.
            new GeneratorStep("Colony art", _ => BlenderColonyBuilder.Apply(BlenderColonyBuilder.SyncAndBuild())),
            new GeneratorStep("Upgrades", c => c.Upgrades = CreateUpgrades(c.Roster.Standard, c.Roster.Hybrids, c.Drones)),
            new GeneratorStep("Codex", c => c.Codex = CreateCodex(c.Recipes, c.Bestiary)),
            new GeneratorStep("UI skin", c => c.Skin = CreateSkin()),
            // The upgrade screen's display stage from Blender (README round 27), worn through the UI skin.
            new GeneratorStep("Display stage art", _ => BlenderDisplayStageBuilder.Apply(BlenderDisplayStageBuilder.SyncAndBuild())),
            new GeneratorStep("Campaign", c => c.Campaign = CreateCampaign(c)),
            // The continent bosses (Docs/BOSSES.md), their Codex rows and the developer boss arena on the campaign.
            new GeneratorStep("Continent bosses", _ => GenerateContinentBosses()),
            new GeneratorStep("Waves", c => GenerateCampaignWaves(c.Campaign, c.Upgrades, c.Unlocks, c.Bestiary)),
            new GeneratorStep("Planet style", c => ApplyPlanetStyles(c.Campaign)),
            // S-69-CRYS painted in Blender: worn by the map globe and the cutscene planets (colony lights drawn over it by the shader).
            new GeneratorStep("Planet art", _ => BlenderPlanetBuilder.Apply(BlenderPlanetBuilder.SyncAndBuild())),
            new GeneratorStep("Menu stage", c => CreateMenuStage(c.Levels, c.Bestiary)),
            // The menu island from Blender: the island, its props, pools, lights and camera (README round 26).
            new GeneratorStep("Menu art", _ => BlenderMenuBuilder.Apply(BlenderMenuBuilder.SyncAndBuild())),
            new GeneratorStep("Story", c => CreateStory(c.Campaign)),
            new GeneratorStep("Level story", GenerateLevelStory),
            new GeneratorStep("Cutscene", GenerateCutscene),
            // The story from Blender: Mercer and his weapons, the dialogue busts and the cutscene's machines and drones.
            new GeneratorStep("Story art", _ => BlenderStoryBuilder.Apply(BlenderStoryBuilder.SyncAndBuild())),
            // README round 26: the cave kit and eyes, the Earth maps, the cutscene sprites and the theme skies.
            new GeneratorStep("Cutscene art", _ => BlenderCutsceneBuilder.Apply(BlenderCutsceneBuilder.SyncAndBuild())),
            new GeneratorStep("Colony rig", _ => ColonyRigBuilder.Apply()),
            new GeneratorStep("Render pipeline", _ => ConfigureRenderPipeline()),
            new GeneratorStep("Sound library", _ => CreateSfxLibrary()),
            new GeneratorStep("Effect library", _ => CreateVfxLibrary()),
            // Markers, bars, projectiles, debris and particles (README round 25b), handed to the effect library.
            new GeneratorStep("Marker art", _ => BlenderMarkerBuilder.Apply(BlenderMarkerBuilder.SyncAndBuild())),
            // The living spawn origins and route gates (README rounds 23, 25b), after the effect library for their burst.
            new GeneratorStep("Origin art", _ => BlenderOriginBuilder.Apply(BlenderOriginBuilder.SyncAndBuild())),
            new GeneratorStep("Scenes", GenerateScenes),
        };

        // Every campaign level's theme with the key of its continent (C1 ... C6).
        private static IEnumerable<(VisualTheme Theme, string Key)> ThemesByContinent(GeneratorContext context)
        {
            foreach (SpecLevel level in context.CampaignLevels)
            {
                yield return (level.Level.Theme, ContinentTable[level.Continent].Key);
            }
        }

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
