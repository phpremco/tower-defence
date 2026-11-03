using Game.Boot;
using Game.Core;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>Generator part for the scenes and the build list. Runs last: creating a scene unloads assets nothing references yet.</summary>
    public static partial class ProjectGenerator
    {
        private static void GenerateScenes(GeneratorContext context)
        {
            CreateBootScene(context.Campaign, context.Upgrades, context.Codex, context.Unlocks, context.Playtest, context.Skin, CreateShaderKeepers());
            CreateLevelScene();
            CreateMainMenuScene();
            CreateIntroScene();
            CreateMenuScene<WorldMapScreen>(SceneNames.WorldMap);
            CreateMenuScene<UpgradeScreen>(SceneNames.Base);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Boot}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Intro}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.MainMenu}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.WorldMap}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Level}.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/{SceneNames.Base}.unity", true),
            };
        }

        // The Intro scene holds only the cutscene stage; the opening cutscene comes from the campaign story at runtime.
        private static void CreateIntroScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var stage = new GameObject(nameof(CutsceneStage)).AddComponent<CutsceneStage>();
            var so = new SerializedObject(stage);
            so.FindProperty("_tiltShift").objectReferenceValue = _tiltShift;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{SceneNames.Intro}.unity");
        }
    }
}
