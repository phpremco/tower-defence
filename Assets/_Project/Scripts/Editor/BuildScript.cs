using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
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
        private const string Il2CppOutputFolder = "Builds/WindowsIl2Cpp";
        private const string Il2CppVariationsFolder = "PlaybackEngines/windowsstandalonesupport/Variations/win64_player_nondevelopment_il2cpp";

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

            Report(BuildPipeline.BuildPlayer(options));
        }

        /// <summary>
        /// The same player with the IL2CPP backend (what consoles require), into Builds/WindowsIl2Cpp. The backend and the
        /// stripping level are set for this build only and put back afterwards, so the project stays on Mono for the
        /// everyday build. Needs the editor's "Windows Build Support (IL2CPP)" module and the Visual Studio C++ tools.
        /// </summary>
        [MenuItem("Tools/Build/Windows Player (IL2CPP)")]
        public static void BuildWindowsIl2Cpp()
        {
            string variations = Path.Combine(EditorApplication.applicationContentsPath, Il2CppVariationsFolder);
            if (!Directory.Exists(variations))
            {
                throw new InvalidOperationException($"The IL2CPP Windows module is not installed ({variations} is missing). Add 'Windows Build Support (IL2CPP)' to this editor in Unity Hub.");
            }

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No scenes in the build settings. Run Tools/Generate/Scenes and Starter Data first.");
            }

            var target = NamedBuildTarget.Standalone;
            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(target);
            ManagedStrippingLevel stripping = PlayerSettings.GetManagedStrippingLevel(target);
            try
            {
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
                // Low strips only what is provably unused; link.xml keeps the JSON and Input System types on top of that.
                PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Low);
                PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerSettings.defaultIsNativeResolution = true;
                PlayerSettings.runInBackground = true;
                Directory.CreateDirectory(Il2CppOutputFolder);
                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(Il2CppOutputFolder, ExecutableName),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                };
                Report(BuildPipeline.BuildPlayer(options));
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(target, backend);
                PlayerSettings.SetManagedStrippingLevel(target, stripping);
            }
        }

        private static void Report(BuildReport report)
        {
            BuildSummary summary = report.summary;
            Debug.Log($"[Build] {summary.result}: {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime.TotalSeconds:0}s -> {summary.outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed: {summary.result}");
            }
        }
    }
}
