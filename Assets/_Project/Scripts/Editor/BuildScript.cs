using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Command line and menu entry for making a playable Windows build. Builds the scenes listed in the build
    /// settings (the generator keeps that list up to date) into Builds/Windows, which git ignores.
    /// </summary>
    public static class BuildScript
    {
        private const string OutputFolder = "Builds/Windows";
        private const string ExecutableName = "TowerDefense.exe";

        [MenuItem("Tools/Build/Windows Player")]
        public static void BuildWindows()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No scenes in the build settings. Run Tools/Generate/Scenes and Starter Data first.");
            }

            // A borderless full-screen window at the desktop resolution; Alt+Enter switches to windowed.
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;

            Directory.CreateDirectory(OutputFolder);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputFolder, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[Build] {summary.result}: {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime.TotalSeconds:0}s -> {summary.outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed: {summary.result}");
            }
        }
    }
}
