using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The version text shown in the main menu corner and written into every crash log entry: the project's version
    /// number plus what kind of build it is, so a report says at once whether it came from a release player.
    /// </summary>
    public static class BuildVersion
    {
        /// <summary>The version of the running game.</summary>
        public static string Current => Describe(Application.version, Debug.isDebugBuild, Application.isEditor, IsIl2Cpp);

        /// <summary>Pure, so the rule is tested without a build: "0.1.0", "0.1.0 dev", "0.1.0 editor", plus " il2cpp".</summary>
        public static string Describe(string version, bool developmentBuild, bool editor, bool il2Cpp)
        {
            string number = string.IsNullOrEmpty(version) ? "0" : version;
            string kind = editor ? " editor" : developmentBuild ? " dev" : string.Empty;
            return number + kind + (il2Cpp ? " il2cpp" : string.Empty);
        }

        private static bool IsIl2Cpp
        {
            get
            {
#if ENABLE_IL2CPP
                return true;
#else
                return false;
#endif
            }
        }
    }
}
