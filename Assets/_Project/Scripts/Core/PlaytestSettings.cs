using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Switches for testing a level quickly. Lives in an asset so nothing is hardcoded in logic.</summary>
    [CreateAssetMenu(menuName = "Game/Playtest Settings", fileName = "PlaytestSettings")]
    public sealed class PlaytestSettings : ScriptableObject
    {
        [SerializeField] private bool _unlockAllWeapons;
        [SerializeField] private bool _infiniteMoney;

        [Tooltip("Developer tools (the story viewer under Options). Only honoured in the editor and development builds; " +
                 "a development build also takes the -devmode command-line argument.")]
        [SerializeField] private bool _developerMode;

        [Header("Lighting trial (round 15b)")]
        [Tooltip("Keeps the display stages' lights (codex, upgrade screen, drone cards) on their own layer and makes the " +
                 "level's sun the scene's sun. Off is the look as it was: the stage lights reach the whole scene and one " +
                 "of their key lights can become the main light.")]
        [SerializeField] private bool _isolatePreviewLights;
        [Tooltip("The layer the display stages render on while the trial is on; no other object uses it.")]
        [SerializeField] [Range(8, 31)] private int _previewLayer = 31;

        public bool UnlockAllWeapons => _unlockAllWeapons;
        public bool IsolatePreviewLights => _isolatePreviewLights;
        public int PreviewLayer => _previewLayer;

        /// <summary>The preview layer while the lighting trial is on, or -1 when it is off (or there are no settings).</summary>
        public static int IsolatedPreviewLayer() =>
            Services.TryGet(out PlaytestSettings settings) && settings._isolatePreviewLights ? settings._previewLayer : -1;
        public bool InfiniteMoney => _infiniteMoney;

        /// <summary>Whether the developer tools show. Never true in a release build.</summary>
        public bool DeveloperModeOn => DeveloperMode.Resolve(_developerMode, Environment.GetCommandLineArgs(), Application.isEditor, Debug.isDebugBuild);
    }
}
