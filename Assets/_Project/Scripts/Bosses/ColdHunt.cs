using System.Collections.Generic;
using Game.Enemies;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Which tower a boss that hates the cold goes for (Docs/BOSSES.md, Cinder Matriarch): the nearest standing tower that
    /// chills, or none (then she walks the road), and how many she may smash in one fight: one per approach and never more
    /// than about a quarter of the towers. Pure logic over plain candidates, so the tests need no scene.
    /// </summary>
    public static class ColdHunt
    {
        /// <summary>One tower as the hunt sees it.</summary>
        public readonly struct Candidate
        {
            public Candidate(Vector3 position, bool cold, bool standing)
            {
                Position = position;
                Cold = cold;
                Standing = standing;
            }

            public Vector3 Position { get; }

            /// <summary>It slows what it hits (a Cryo, a Cryo Beam): what she hunts.</summary>
            public bool Cold { get; }

            /// <summary>Not wrecked, not thrown, not already marked for another boss effect.</summary>
            public bool Standing { get; }
        }

        /// <summary>True for a tower that chills what it hits, at any of its levels.</summary>
        public static bool IsCold(float slowFactor, IReadOnlyList<float> levelSlowFactors)
        {
            if (slowFactor < 1f)
            {
                return true;
            }

            for (int i = 0; i < levelSlowFactors.Count; i++)
            {
                if (levelSlowFactors[i] > 0f && levelSlowFactors[i] < 1f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Index of the nearest standing cold tower, or -1: no cold on the field, she takes the road.</summary>
        public static int Choose(Vector3 from, IReadOnlyList<Candidate> towers)
        {
            int best = -1;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < towers.Count; i++)
            {
                Candidate tower = towers[i];
                if (!tower.Cold || !tower.Standing)
                {
                    continue;
                }

                float dx = tower.Position.x - from.x;
                float dz = tower.Position.z - from.z;
                float sqr = dx * dx + dz * dz;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>How many towers she may smash in one fight with this many towers standing: at least one, else a quarter.</summary>
        public static int SmashCap(int towers) => Mathf.Max(1, Mathf.FloorToInt(towers * BossDefinition.MaxFieldShare));

        /// <summary>True while she may set out again: below the cap and her cooldown since the last smash run out.</summary>
        public static bool MayHunt(int smashed, int towersAtStart, float sinceLastSmash, float cooldown) =>
            smashed < SmashCap(towersAtStart) && sinceLastSmash >= cooldown;
    }
}
