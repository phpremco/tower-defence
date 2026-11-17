using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// Which nest every creature of a wave that comes out of a nest uses, drawn once when the level starts: a creature
    /// takes one of the nests of its element at random, as before, but the choice is known ahead, so an origin that will
    /// be used is marked from the first moment and only opens in the waves that really send something through it.
    /// A group with an authored nest (<see cref="SpawnGroup.Nest"/>) is not drawn: all of it uses that nest.
    /// Pure data, no scene objects.
    /// </summary>
    public sealed class NestAssignment
    {
        /// <summary>No nest: the creature does not come out of a nest (or its element has none on this level).</summary>
        public const int None = -1;

        private readonly int[][] _nests;

        private NestAssignment(int[][] nests)
        {
            _nests = nests;
        }

        /// <summary>Draws the nest of every nest spawn of the wave from the level's nests of the creature's element.</summary>
        public static NestAssignment Draw(WaveDefinition wave, DifficultyProfile difficulty, IReadOnlyList<ElementNest> nests, System.Random random)
        {
            var result = new int[wave.Groups.Count][];
            var candidates = new List<int>(nests.Count);
            for (int g = 0; g < result.Length; g++)
            {
                SpawnGroup group = wave.Groups[g];
                int total = WaveScheduler.CountOf(group, difficulty);
                result[g] = new int[total];
                candidates.Clear();
                if (group.Source == SpawnSource.Nest && group.Enemy != null)
                {
                    for (int n = 0; n < nests.Count; n++)
                    {
                        if (nests[n].Element == group.Enemy.Element)
                        {
                            candidates.Add(n);
                        }
                    }
                }

                int authored = Authored(group, candidates);
                for (int k = 0; k < total; k++)
                {
                    result[g][k] = candidates.Count == 0 ? None : authored != None ? authored : candidates[random.Next(candidates.Count)];
                }
            }

            return new NestAssignment(result);
        }

        // The group's authored nest when it is one of the candidates (a nest of the creature's element); the validator
        // reports an authored nest that is not, and the game falls back on the draw.
        private static int Authored(SpawnGroup group, List<int> candidates) =>
            group.Nest > 0 && candidates.Contains(group.Nest - 1) ? group.Nest - 1 : None;

        /// <summary>The nest of spawn <paramref name="index"/> of a group, or <see cref="None"/>.</summary>
        public int NestOf(int group, int index) =>
            group >= 0 && group < _nests.Length && index >= 0 && index < _nests[group].Length ? _nests[group][index] : None;

        /// <summary>Whether any creature of the wave comes out of that nest.</summary>
        public bool Uses(int nest)
        {
            foreach (int[] group in _nests)
            {
                foreach (int assigned in group)
                {
                    if (assigned == nest)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int GroupCount => _nests.Length;

        public int CountOf(int group) => _nests[group].Length;
    }
}
