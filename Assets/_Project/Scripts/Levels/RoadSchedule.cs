using System.Collections.Generic;
using Game.Balance;

namespace Game.Levels
{
    /// <summary>
    /// The validator of the main roads a level writes per wave (round 82, <see cref="RoadSpec"/>) against the level they
    /// are written for: the wave exists, the entry names at least one road, and a second road is only named on a level
    /// that has a second main ground path. The solve stage and the tests run it.
    /// </summary>
    public static class RoadSchedule
    {
        public static void Check(LevelDefinition level, IReadOnlyList<RoadSpec> plan, int waves, List<string> errors)
        {
            int mainRoads = 0;
            for (int i = 0; i < level.Paths.Length; i++)
            {
                mainRoads += !level.Paths[i].Flying && level.RoleOfPath(i) == SpawnRole.Main ? 1 : 0;
            }

            foreach (RoadSpec roads in plan)
            {
                string where = $"{level.name}, roads of wave {roads.Wave}";
                if (roads.Wave < 1 || roads.Wave > waves)
                {
                    errors.Add($"{where}: the level has waves 1 to {waves}.");
                }

                if (roads.MinDifficulty > roads.MaxDifficulty)
                {
                    errors.Add($"{where}: applies to no difficulty ({roads.MinDifficulty} to {roads.MaxDifficulty}).");
                }

                if (roads.Roads == MainRoads.None)
                {
                    errors.Add($"{where}: a wave needs a road.");
                }
                else if ((roads.Roads & MainRoads.Second) != 0 && mainRoads < 2)
                {
                    errors.Add($"{where}: the level has no second main road.");
                }
            }
        }
    }
}
