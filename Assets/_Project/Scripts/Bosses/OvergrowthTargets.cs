using System.Collections.Generic;
using Game.Enemies;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Which build pads a spore volley goes for (Docs/BOSSES.md, Rotbloom): the cluster with the most towers within its
    /// reach, at most a few pads a volley, and never so many that more than about a quarter of the towers stand
    /// overgrown at once. Pure logic over tower positions.
    /// </summary>
    public static class OvergrowthTargets
    {
        /// <summary>The most pads that may be overgrown at once for that many towers: a quarter, at least one.</summary>
        public static int Cap(int towers) => Mathf.Max(1, Mathf.FloorToInt(towers * BossDefinition.MaxFieldShare));

        /// <summary>
        /// Picks the towers (by index) whose pads the volley spores: the fullest cluster within reach, its towers nearest
        /// its middle first. <paramref name="eligible"/> leaves out towers already overgrown, wrecked or upset.
        /// </summary>
        public static void Choose(IReadOnlyList<Vector3> towers, IReadOnlyList<bool> eligible, Vector3 from, float reach, float clusterRadius, int maxPads, int overgrown, List<int> picked)
        {
            picked.Clear();
            int allowed = Mathf.Min(maxPads, Cap(towers.Count) - overgrown);
            if (allowed <= 0)
            {
                return;
            }

            int centre = -1;
            int most = 0;
            float nearest = float.PositiveInfinity;
            float clusterSqr = clusterRadius * clusterRadius;
            for (int i = 0; i < towers.Count; i++)
            {
                if (!eligible[i] || FlatSqr(towers[i], from) > reach * reach)
                {
                    continue;
                }

                int count = 0;
                for (int j = 0; j < towers.Count; j++)
                {
                    count += eligible[j] && FlatSqr(towers[i], towers[j]) <= clusterSqr ? 1 : 0;
                }

                float distance = FlatSqr(towers[i], from);
                if (count > most || (count == most && distance < nearest))
                {
                    centre = i;
                    most = count;
                    nearest = distance;
                }
            }

            if (centre < 0)
            {
                return;
            }

            var members = new List<int>(most);
            for (int j = 0; j < towers.Count; j++)
            {
                if (eligible[j] && FlatSqr(towers[centre], towers[j]) <= clusterSqr)
                {
                    members.Add(j);
                }
            }

            members.Sort((a, b) => FlatSqr(towers[a], towers[centre]).CompareTo(FlatSqr(towers[b], towers[centre])));
            for (int k = 0; k < members.Count && picked.Count < allowed; k++)
            {
                picked.Add(members[k]);
            }
        }

        private static float FlatSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
