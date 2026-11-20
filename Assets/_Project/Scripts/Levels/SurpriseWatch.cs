namespace Game.Levels
{
    /// <summary>
    /// Which spawns of a level are worth a ping on the radar (round 88a, Remco missed a group that slipped in on Glass
    /// Meadows because he was not watching the radar): the first creature out of a surprise spawn point (a surprise path
    /// or nest, <see cref="SpawnRole"/>) or out of a nest that joins the road behind the defence gate
    /// (<see cref="RearNests"/>), after that point has been quiet for a while. A stream that keeps coming pings once; the
    /// next group after a silence pings again. Pure logic; the spawner asks it at every spawn.
    /// </summary>
    public sealed class SurpriseWatch
    {
        /// <summary>A point that sent nothing for this many seconds pings again with its next creature.</summary>
        public const float QuietSeconds = 6f;

        private readonly bool[] _paths;
        private readonly bool[] _nests;
        private readonly float[] _pathLast;
        private readonly float[] _nestLast;

        /// <param name="paths">Per path of the level: true when a creature coming down it is a surprise.</param>
        /// <param name="nests">Per nest of the level: true when a creature coming out of it is a surprise.</param>
        public SurpriseWatch(bool[] paths, bool[] nests)
        {
            _paths = paths ?? new bool[0];
            _nests = nests ?? new bool[0];
            _pathLast = Quiet(_paths.Length);
            _nestLast = Quiet(_nests.Length);
        }

        /// <summary>The watch of a level: its surprise ground paths, its surprise nests and its nests behind the defence gate.</summary>
        public static SurpriseWatch For(LevelDefinition level)
        {
            var paths = new bool[level.Paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = !level.Paths[i].Flying && level.RoleOfPath(i) == SpawnRole.Surprise;
            }

            bool[] behind = RearNests.BehindGate(level);
            var nests = new bool[level.Nests.Length];
            for (int i = 0; i < nests.Length; i++)
            {
                // Nests are numbered from one in the level's roles.
                nests[i] = level.RoleOfNest(i + 1) == SpawnRole.Surprise || (i < behind.Length && behind[i]);
            }

            return new SurpriseWatch(paths, nests);
        }

        /// <summary>True when the path is watched at all.</summary>
        public bool WatchesPath(int index) => index >= 0 && index < _paths.Length && _paths[index];

        /// <summary>True when the nest (counted from zero) is watched at all.</summary>
        public bool WatchesNest(int index) => index >= 0 && index < _nests.Length && _nests[index];

        /// <summary>A creature came down that path at <paramref name="now"/>: true when it is worth a ping.</summary>
        public bool PathSpawned(int index, float now) => WatchesPath(index) && Spawned(_pathLast, index, now);

        /// <summary>A creature came out of that nest (counted from zero) at <paramref name="now"/>: true when it is worth a ping.</summary>
        public bool NestSpawned(int index, float now) => WatchesNest(index) && Spawned(_nestLast, index, now);

        private static bool Spawned(float[] last, int index, float now)
        {
            bool ping = now - last[index] >= QuietSeconds;
            last[index] = now;
            return ping;
        }

        private static float[] Quiet(int count)
        {
            var last = new float[count];
            for (int i = 0; i < count; i++)
            {
                last[i] = float.NegativeInfinity;
            }

            return last;
        }
    }
}
