using System.Collections.Generic;
using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// The wave script of every campaign level, looked up by level key, and the helpers the level specs write them with.
    /// Pure data on creature ids, so the generator and the BalanceLab tool read the same scripts; the size of each wave
    /// comes from <see cref="WaveSolver"/>. Each script lives in its level's spec (<see cref="LevelSpecs"/>).
    /// Bosses per difficulty: every level keeps its bosses on all difficulties, Hard adds one boss wave and Psycho
    /// another (GAME_DESIGN: harder difficulties bring extra boss waves). Bosses of the first continent come through the
    /// main gate (Docs/LEVEL_GUIDE.md).
    /// </summary>
    public static class WaveScripts
    {
        /// <summary>Spawn source of the element nests, as the game stores it (Game.Levels.SpawnSource).</summary>
        public const int Nest = WaveComposer.NestSource;

        public const int Medium = 1;
        public const int Hard = 2;
        public const int Psycho = 3;

        private static readonly BossRoute[] Gate = { BossRoute.MainGate };

        public static WaveScriptSpec[] All
        {
            get
            {
                var scripts = new List<WaveScriptSpec>();
                foreach (CampaignLevelSpec spec in LevelSpecs.All)
                {
                    scripts.Add(spec.Waves);
                }

                // The builder's tuning (round 22) rides on the scripts, so every reader sees the same tuned curve.
                WaveTuning.Apply(scripts);
                return scripts.ToArray();
            }
        }

        public static WaveScriptSpec ByKey(string key)
        {
            foreach (WaveScriptSpec script in All)
            {
                if (script.Key == key)
                {
                    return script;
                }
            }

            return null;
        }

        public static TroopSpec Join(string enemy, int firstWave, float weight, float interval, int source = 0, float delay = 0f, int minDifficulty = 0, int pack = 0, bool loose = false, int maxDifficulty = int.MaxValue, int nest = 0) =>
            new TroopSpec { Enemy = enemy, FirstWave = firstWave, Weight = weight, Interval = interval, Source = source, Delay = delay, MinDifficulty = minDifficulty, Pack = pack, Loose = loose, MaxDifficulty = maxDifficulty, Nest = nest };

        public static BossSpec Boss(int wave, string enemy, int minDifficulty = 0, BossRoute[] routes = null) =>
            new BossSpec { Wave = wave, Enemy = enemy, MinDifficulty = minDifficulty, Routes = routes ?? Gate };

        public static TeaserSpec Teaser(string enemy, int wave, int count) => new TeaserSpec { Enemy = enemy, Wave = wave, Count = count };

        /// <summary>The level's own surprise on one wave, on every difficulty: its entries replace the grammar's default there.</summary>
        public static SurpriseSpec Surprise(int wave, params SurpriseEntrySpec[] entries) => new SurpriseSpec { Wave = wave, Entries = entries };

        /// <summary>The same for a range of difficulties (indices, Easy 0 to Psycho 3); the others keep the grammar's default.</summary>
        public static SurpriseSpec Surprise(int wave, int minDifficulty, int maxDifficulty, params SurpriseEntrySpec[] entries) =>
            new SurpriseSpec { Wave = wave, MinDifficulty = minDifficulty, MaxDifficulty = maxDifficulty, Entries = entries };

        /// <summary>The main roads the level writes for one wave, on every difficulty: they replace the grammar's choice there.</summary>
        public static RoadSpec OnRoads(int wave, MainRoads roads) => new RoadSpec { Wave = wave, Roads = roads };

        /// <summary>The same for a range of difficulties (indices, Easy 0 to Psycho 3); the others keep the grammar's choice.</summary>
        public static RoadSpec OnRoads(int wave, int minDifficulty, int maxDifficulty, MainRoads roads) =>
            new RoadSpec { Wave = wave, MinDifficulty = minDifficulty, MaxDifficulty = maxDifficulty, Roads = roads };

        /// <summary>A group of an authored surprise out of a nest of the level (counted from 1).</summary>
        public static SurpriseEntrySpec FromNest(int nest, string enemy, int count, WavePattern pattern = WavePattern.Stream, float interval = 0f, float delay = 0f) =>
            new SurpriseEntrySpec { Kind = SpawnPointKind.Nest, Point = nest, Enemy = enemy, Count = count, Pattern = pattern, Interval = interval, Delay = delay };

        /// <summary>A group of an authored surprise down a surprise path of the level (the path's index).</summary>
        public static SurpriseEntrySpec DownPath(int path, string enemy, int count, WavePattern pattern = WavePattern.Stream, float interval = 0f, float delay = 0f) =>
            new SurpriseEntrySpec { Kind = SpawnPointKind.Path, Point = path, Enemy = enemy, Count = count, Pattern = pattern, Interval = interval, Delay = delay };
    }
}
