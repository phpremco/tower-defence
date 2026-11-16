using System;

namespace Game.Core
{
    /// <summary>
    /// Whether the scripted smoke test of a player build runs (round 72). It needs the <c>-autotest</c> argument and a
    /// build that allows it: the editor, a development build, or a smoke build (compiled with the <c>TD_SMOKE_TEST</c>
    /// define by <c>BuildScript.BuildWindowsIl2CppSmoke</c>). A release build never runs it, whatever the command line says.
    /// Pure, so the rule is tested without a build.
    /// </summary>
    public static class SmokeTestGate
    {
        /// <summary>The command-line switch, e.g. <c>TowerDefense.exe -autotest</c>.</summary>
        public const string Argument = "-autotest";

        /// <summary>Adds a deliberate exception at the main menu, to prove the crash log writes in that build.</summary>
        public const string CrashProbeArgument = "-autotest-crashprobe";

        /// <summary>True in a build made by the smoke build method; false in every release build.</summary>
        public static bool SmokeBuild
        {
            get
            {
#if TD_SMOKE_TEST
                return true;
#else
                return false;
#endif
            }
        }

        public static bool Resolve(string[] arguments, bool isEditor, bool isDevelopmentBuild, bool smokeBuild)
        {
            if (!isEditor && !isDevelopmentBuild && !smokeBuild)
            {
                return false;
            }

            return HasArgument(arguments, Argument) || HasArgument(arguments, CrashProbeArgument);
        }

        public static bool HasArgument(string[] arguments, string argument)
        {
            if (arguments == null)
            {
                return false;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], argument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
