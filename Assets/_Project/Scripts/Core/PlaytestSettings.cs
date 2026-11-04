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

        public bool UnlockAllWeapons => _unlockAllWeapons;
        public bool InfiniteMoney => _infiniteMoney;

        /// <summary>Whether the developer tools show. Never true in a release build.</summary>
        public bool DeveloperModeOn => DeveloperMode.Resolve(_developerMode, Environment.GetCommandLineArgs(), Application.isEditor, Debug.isDebugBuild);
    }
}
