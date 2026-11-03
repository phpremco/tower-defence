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
        public const int Nest = 4;

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

        public static TroopSpec Join(string enemy, int firstWave, float weight, float interval, int source = 0, float delay = 0f, int minDifficulty = 0, int pack = 0) =>
            new TroopSpec { Enemy = enemy, FirstWave = firstWave, Weight = weight, Interval = interval, Source = source, Delay = delay, MinDifficulty = minDifficulty, Pack = pack };

        public static BossSpec Boss(int wave, string enemy, int minDifficulty = 0, BossRoute[] routes = null) =>
            new BossSpec { Wave = wave, Enemy = enemy, MinDifficulty = minDifficulty, Routes = routes ?? Gate };

        public static TeaserSpec Teaser(string enemy, int wave, int count) => new TeaserSpec { Enemy = enemy, Wave = wave, Count = count };
    }
}
