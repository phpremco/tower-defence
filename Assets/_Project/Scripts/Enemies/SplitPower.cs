using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The numbers of a boss that splits into shards on separate routes (the Shattered, Docs/BOSSES.md "Splitting"): which
    /// shards and which route each takes, when it splits and pulls back together, and what a dead shard costs the giant.
    /// </summary>
    [Serializable]
    public sealed class SplitPower
    {
        [Tooltip("The phase in which it bursts into its shards (Split, at two thirds of its health).")]
        [SerializeField] [Min(1)] private int _splitPhase = 1;
        [Tooltip("The phase in which the surviving shards pull back together where they are (Rejoined, at a third).")]
        [SerializeField] [Min(1)] private int _rejoinPhase = 2;
        [Tooltip("Its shards, each with the route it takes and where it leaves the giant.")]
        [SerializeField] private ShardSpec[] _shards = new ShardSpec[0];
        [Tooltip("Share each dead shard takes off the giant that rejoins (BOSSES.md: 30%).")]
        [SerializeField] [Range(0f, 0.33f)] private float _deadShardPenalty = 0.3f;
        [Tooltip("Seconds each shard's route lights up when it splits.")]
        [SerializeField] [Min(0.2f)] private float _routeLightSeconds = 2.5f;
        [Tooltip("Share of its health above the split at which the light cracks on its body start to show.")]
        [SerializeField] [Range(0f, 0.3f)] private float _warnShare = 0.08f;
        [Tooltip("Seconds a surviving shard takes to fly back into the giant.")]
        [SerializeField] [Min(0.1f)] private float _flyBackSeconds = 0.6f;
        [Tooltip("Seconds the split lasts in one fight, for the balance ledger.")]
        [SerializeField] [Min(0f)] private float _expectedSplitSeconds = 18f;

        [Header("Entrance through the ruin gate (round 55)")]
        [Tooltip("Metres the gate stands behind the spot the giant appears on, along the way it walks out: enough that its whole body stands clear of the gate's iris while the petals fold, so it never cuts the stone.")]
        [SerializeField] [Min(0f)] private float _gateSetBack = 3f;

        [Header("Look per phase (README round 28)")]
        [Tooltip("Shell bones, off once it has rejoined.")]
        [SerializeField] private string[] _shellBones = { "Shell_1", "Shell_2", "Shell_3", "Shell_4", "Shell_5", "Shell_6", "Shell_7", "Shell_8" };
        [Tooltip("Crystal bones and their scale once it has rejoined.")]
        [SerializeField] private string[] _crystalBones = { "Crys_1", "Crys_2", "Crys_3", "Crys_4", "Crys_5", "Crys_6" };
        [SerializeField] [Min(0f)] private float _rejoinedCrystalScale = 1.35f;
        [Tooltip("Scale of a dead shard's core spire on the rejoined giant.")]
        [SerializeField] [Range(0f, 1f)] private float _deadCoreScale = 0.35f;

        public int SplitPhase => _splitPhase;
        public int RejoinPhase => _rejoinPhase;
        public ShardSpec[] Shards => _shards ?? new ShardSpec[0];
        public float DeadShardPenalty => _deadShardPenalty;
        public float RouteLightSeconds => _routeLightSeconds;
        public float WarnShare => _warnShare;
        public float FlyBackSeconds => _flyBackSeconds;
        public float ExpectedSplitSeconds => _expectedSplitSeconds;
        public float GateSetBack => _gateSetBack;
        public string[] ShellBones => _shellBones ?? new string[0];
        public string[] CrystalBones => _crystalBones ?? new string[0];
        public float RejoinedCrystalScale => _rejoinedCrystalScale;
        public float DeadCoreScale => _deadCoreScale;
    }
}
