using System;

namespace Game.Balance
{
    /// <summary>
    /// Flat per-hit plating as the game deals it (<c>Game.Combat.Health.ThroughPlating</c>): every hit loses the plating's
    /// points, but at least the floor share of the hit gets through. A few heavy hits pass it far better than many light ones,
    /// which is what makes a plated creature ask for a Cannon or a Railgun. The two copies are checked against each other in
    /// the EditMode tests (the balance model has no engine references, so it cannot call the game's).
    /// </summary>
    public static class Plating
    {
        /// <summary>What one hit leaves after the plating.</summary>
        public static float Through(float hit, float plating, float floor) =>
            plating <= 0f ? hit : Math.Max(hit - plating, hit * floor);

        /// <summary>The share of a hit that gets through (1 without plating).</summary>
        public static float Share(float hit, float plating, float floor) =>
            hit <= 0f ? 1f : Through(hit, plating, floor) / hit;
    }
}
