using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The health arithmetic of a boss that splits (Docs/BOSSES.md, the Shattered): each shard takes an equal share of what
    /// the giant had left, and the giant that pulls back together has what its survivors had, less a share for every
    /// shard that died. Pure logic.
    /// </summary>
    public static class ShardSplit
    {
        /// <summary>Each shard's health: an equal share of the giant's health at the split.</summary>
        public static float ShardHealth(float remaining, int shards) => shards > 0 ? Mathf.Max(0f, remaining) / shards : 0f;

        /// <summary>The rejoined giant's health: the survivors' health, each dead shard taking its share off.</summary>
        public static float RejoinHealth(float survivorsHealth, int deadShards, float penaltyPerDead) =>
            Mathf.Max(0f, survivorsHealth) * Mathf.Clamp01(1f - penaltyPerDead * Mathf.Max(0, deadShards));
    }
}
