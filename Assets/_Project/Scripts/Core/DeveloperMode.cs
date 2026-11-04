using System;

namespace Game.Core
{
    /// <summary>
    /// Whether the developer tools (the story viewer, later more) are on. They switch on from the playtest data flag
    /// or the command-line argument, but only in the editor or a development build: a release build never shows them,
    /// whatever the data or the command line say. Pure, so the rule is tested without a build.
    /// </summary>
    public static class DeveloperMode
    {
        /// <summary>The command-line switch, e.g. <c>TowerDefense.exe -devmode</c> on a development build.</summary>
        public const string Argument = "-devmode";

        /// <param name="dataFlag">The playtest settings' developer mode switch.</param>
        /// <param name="arguments">The process command line.</param>
        /// <param name="isEditor">Running inside the editor.</param>
        /// <param name="isDevelopmentBuild">A development build (debug symbols, the development console).</param>
        public static bool Resolve(bool dataFlag, string[] arguments, bool isEditor, bool isDevelopmentBuild)
        {
            if (!isEditor && !isDevelopmentBuild)
            {
                return false;
            }

            return dataFlag || HasArgument(arguments);
        }

        private static bool HasArgument(string[] arguments)
        {
            if (arguments == null)
            {
                return false;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], Argument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
