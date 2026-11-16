namespace Game.Core
{
    /// <summary>Where the game was when an exception was logged: the crash log's header line.</summary>
    public readonly struct CrashContext
    {
        public CrashContext(string version, string scene, string level)
        {
            Version = version;
            Scene = scene;
            Level = level;
        }

        public string Version { get; }

        public string Scene { get; }

        public string Level { get; }
    }
}
