using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Boot
{
    /// <summary>
    /// Connects the <see cref="CrashLog"/> to Unity's log once per session. Boot is entered again after a restart, so the
    /// hook is installed only the first time. The level in the header comes from the running level session.
    /// </summary>
    public static class CrashLogHook
    {
        private static CrashLog _log;

        /// <summary>The installed log (tests and the autotest read its path), or null before boot.</summary>
        public static CrashLog Log => _log;

        /// <summary>Players only: the editor has its own console, and its persistent folder is the developer's real one.</summary>
        public static void Install()
        {
            if (_log != null || Application.isEditor)
            {
                return;
            }

            _log = new CrashLog(Application.persistentDataPath, () => Time.realtimeSinceStartupAsDouble, CurrentContext);
            Application.logMessageReceived += _log.Handle;
        }

        private static CrashContext CurrentContext()
        {
            string scene = SceneManager.GetActiveScene().name;
            string level = scene == SceneNames.Level && Services.TryGet(out LevelSession session)
                ? session.Level.name + " / " + session.Difficulty.name
                : null;
            return new CrashContext(BuildVersion.Current, scene, level);
        }
    }
}
