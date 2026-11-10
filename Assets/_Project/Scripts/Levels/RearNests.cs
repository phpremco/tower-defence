using System;
using System.Collections.Generic;
using Game.Balance;

namespace Game.Levels
{
    /// <summary>
    /// Which of a level's nests join the road behind the defence gate, by the balance model's own rule (round 31,
    /// <see cref="RouteKill"/>: a walker out of a nest joins the main road at the cell nearest the nest; past the defence gate's
    /// cell it never meets the gate's queue). Remco 2026-11-10 keeps such rifts on purpose and wants the player warned the first
    /// time one opens, so the game reads the same geometry the balance model plays.
    /// </summary>
    public static class RearNests
    {
        /// <summary>Per nest of the level (same order as <see cref="LevelDefinition.Nests"/>): true when it joins behind the defence gate.</summary>
        public static bool[] BehindGate(LevelDefinition level)
        {
            if (level == null || level.Nests == null || level.Nests.Length == 0)
            {
                return new bool[0];
            }

            return BehindGate(BalanceLedger.Geometry(level), BalanceLedger.Shape(level).NestRemaining);
        }

        /// <summary>The rule on the model's inputs: the main road with its defence gate, and each nest's share of the road still ahead.</summary>
        public static bool[] BehindGate(LevelGeometry geometry, IReadOnlyList<float> nestRemaining)
        {
            var behind = new bool[nestRemaining.Count];
            if (geometry == null || geometry.GroundRoutes.Count == 0 || !geometry.DefenceGate.HasValue)
            {
                return behind;
            }

            LevelGeometry.Route route = geometry.GroundRoutes[0];
            for (int i = 0; i < behind.Length; i++)
            {
                // As RouteKill.Add: a lane that never meets the defence gate joins behind it as well.
                behind[i] = route.GateIndex < 0 || JoinIndex(route.Cells.Count, nestRemaining[i]) > route.GateIndex;
            }

            return behind;
        }

        /// <summary>The road cell a nest's walkers join at, from the share of the road still ahead (RouteKill.JoinIndex).</summary>
        public static int JoinIndex(int routeCells, float remaining) =>
            Math.Max(0, Math.Min(routeCells - 1, (int)Math.Round((1f - Math.Max(0f, Math.Min(1f, remaining))) * (routeCells - 1))));
    }
}
