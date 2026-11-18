using System;
using System.Collections.Generic;
using Game.Balance;
using Game.Core;
using Game.Enemies;

namespace Game.Levels
{
    /// <summary>
    /// The validator of a level's own surprises (round 81, <see cref="SurpriseSpec"/>) against the level they are written
    /// for: every entry names a creature, a number of at least one and a spawn point that exists, has the surprise role
    /// (<see cref="SpawnRole"/>) and can send that creature (a nest of its element, a ground path for a walker). Any
    /// combination of surprise points in one wave is fine. That a nest leaves its walkers a real walk to the base is the
    /// level validator's own check on every nest. The solve stage and the tests run it.
    /// </summary>
    public static class SurpriseSchedule
    {
        public static void Check(LevelDefinition level, IReadOnlyList<SurpriseSpec> surprises, int waves, Func<string, EnemyDefinition> enemyById, List<string> errors)
        {
            foreach (SurpriseSpec surprise in surprises)
            {
                string where = $"{level.name}, surprise of wave {surprise.Wave}";
                if (surprise.Wave < 1 || surprise.Wave > waves)
                {
                    errors.Add($"{where}: the level has waves 1 to {waves}.");
                }

                if (surprise.MinDifficulty > surprise.MaxDifficulty)
                {
                    errors.Add($"{where}: applies to no difficulty ({surprise.MinDifficulty} to {surprise.MaxDifficulty}).");
                }

                foreach (SurpriseEntrySpec entry in surprise.Entries)
                {
                    CheckEntry(level, where, entry, enemyById(entry.Enemy), errors);
                }
            }
        }

        private static void CheckEntry(LevelDefinition level, string where, SurpriseEntrySpec entry, EnemyDefinition enemy, List<string> errors)
        {
            if (enemy == null)
            {
                errors.Add($"{where}: unknown creature '{entry.Enemy}'.");
                return;
            }

            if (entry.Count < 1)
            {
                errors.Add($"{where}: an entry of {entry.Count} {entry.Enemy} is an empty group.");
            }

            if (entry.Kind == SpawnPointKind.Nest)
            {
                CheckNest(level, where, entry, enemy, errors);
                return;
            }

            if (entry.Point < 0 || entry.Point >= level.Paths.Length || level.Paths[entry.Point].Flying)
            {
                errors.Add($"{where}: path {entry.Point} is not a ground path of the level.");
            }
            else if (level.RoleOfPath(entry.Point) != SpawnRole.Surprise)
            {
                errors.Add($"{where}: path {entry.Point} is a main path; a surprise comes down a surprise path.");
            }
            else if (enemy.Flies)
            {
                errors.Add($"{where}: {entry.Enemy} flies and cannot walk down path {entry.Point}.");
            }
        }

        private static void CheckNest(LevelDefinition level, string where, SurpriseEntrySpec entry, EnemyDefinition enemy, List<string> errors)
        {
            if (entry.Point < 1 || entry.Point > level.Nests.Length)
            {
                errors.Add($"{where}: nest {entry.Point} does not exist (the level has {level.Nests.Length}).");
            }
            else if (level.RoleOfNest(entry.Point) != SpawnRole.Surprise)
            {
                errors.Add($"{where}: nest {entry.Point} is a main nest; a surprise comes out of a surprise point.");
            }
            else if (enemy.Element == Element.None || level.Nests[entry.Point - 1].Element != enemy.Element)
            {
                errors.Add($"{where}: {entry.Enemy} cannot come out of the {level.Nests[entry.Point - 1].Element} nest {entry.Point}.");
            }
        }
    }
}
