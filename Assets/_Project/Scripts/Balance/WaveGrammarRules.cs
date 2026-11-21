namespace Game.Balance
{
    /// <summary>
    /// The numbers of the wave grammar (round 79, Docs/WAVE_GRAMMAR.md): how long a cluster is, how long a breather, where a
    /// surprise may fall and how a boss wave is laid out. General factors per difficulty (Easy to Psycho; a shorter array
    /// repeats its last entry), never per level: a level's script says which creatures it has, these rules say how they arrive.
    /// </summary>
    public sealed class WaveGrammarRules
    {
        /// <summary>The validator's bounds on the stored waves: the longest silence inside a wave and the longest wave, in seconds.</summary>
        public const float MaxPauseSeconds = 16f;
        public const float MaxWaveSeconds = 120f;

        /// <summary>Seconds of spawning (on the script's clock) one cluster of a stream lasts before a breather.</summary>
        public float BeatSeconds = 7f;

        /// <summary>The same for a swarm, a combo swarm and a tank with its escort: those stay whole for longer.</summary>
        public float SwarmBeatSeconds = 14f;
        public float ComboBeatSeconds = 12f;
        public float EscortBeatSeconds = 22f;

        /// <summary>A wave is cut only when every cluster keeps at least this share of a beat.</summary>
        public float MinBeatShare = 0.7f;

        /// <summary>The most clusters one wave is cut into.</summary>
        public int MaxBeats = 4;

        /// <summary>Seconds of silence between two clusters, per difficulty.</summary>
        public float[] Breather = { 6f, 5f, 4.5f, 4f };

        /// <summary>
        /// Seconds of a pause the defence still fires through (the last creatures of a cluster are still walking past the guns);
        /// the rest of a pause is idle time and no part of the wave's window in the balance model (<see cref="WaveLoad.Span"/>).
        /// </summary>
        public float PauseCredit = 2f;

        /// <summary>Tank escort: cells of road the tanks have walked when the escort runs into their back.</summary>
        public float EscortCatchCells = 10f;

        /// <summary>The longest the escort waits behind its tanks, in seconds.</summary>
        public float MaxEscortLag = 14f;

        /// <summary>The escort's spacing against the troops' own (below 1 closer together).</summary>
        public float EscortTempo = 0.7f;

        /// <summary>Combo swarm: from this share of the level on, every second plain wave sends all troops at once.</summary>
        public float ComboFrom = 0.45f;
        public float ComboTempo = 1f;

        /// <summary>
        /// A level that forks (round 82): the share of its waves whose front comes down every main road at once, per
        /// difficulty; the other waves keep to one road. Spread evenly over the level, so Easy is mostly one road at a time
        /// and Psycho mostly both. One on every difficulty is the behaviour before round 82: every wave down every road.
        /// </summary>
        public float[] MultiRoadShare = { 0.3f, 0.45f, 0.6f, 0.75f };

        /// <summary>Of the waves that keep to one road, every so-manieth takes the second road; the others the first.</summary>
        public int SecondRoadEvery = 2;

        /// <summary>
        /// Off only to read a level without its surprises (a lab run, the test that a surprise lifts no other wave); never
        /// off in the rules a level is composed with.
        /// </summary>
        public bool Surprises = true;

        /// <summary>Share of a surprise wave's budget that comes as the surprise, per difficulty.</summary>
        public float[] SurpriseShare = { 0.12f, 0.18f, 0.22f, 0.26f };

        /// <summary>
        /// The fewest creatures a surprise is, all its groups together, before the difficulty's count multiplier (round 81,
        /// Remco: one stray creature behind a gate is no surprise). A wave whose share buys fewer carries none. The groups may
        /// come out of several nests at once; the number is the whole surprise's, not one nest's.
        /// </summary>
        public int MinSurpriseGroup = 3;

        /// <summary>Waves from one surprise to the next at the least, per difficulty: never more than one surprise in so many waves.</summary>
        public int[] SurpriseSpacing = { 4, 3, 3, 2 };

        /// <summary>Opening waves that never carry a surprise, per difficulty.</summary>
        public int[] SurpriseFreeWaves = { 4, 3, 2, 2 };

        /// <summary>A flank needs a level that has run this share of its waves: the second road is first met by the plain streams.</summary>
        public float FlankFrom = 0.35f;

        /// <summary>The surprise opens this far into the front's spawning, never sooner than <see cref="SurpriseMinDelay"/> seconds: the player is committed at the front.</summary>
        public float SurpriseAt = 0.5f;
        public float SurpriseMinDelay = 12f;

        /// <summary>Clusters of a boss wave's crowd; the boss walks in with the last one. The last wave of a level takes <see cref="FinaleBeats"/>.</summary>
        public int BossBeats = 2;
        public int FinaleBeats = 3;

        /// <summary>Seconds of silence between the clusters of a boss wave, per difficulty: time to mend and to bring the hero back.</summary>
        public float[] BossBreather = { 9f, 8f, 7f, 6f };

        /// <summary>The shortest cluster a boss wave is cut into, in seconds; a thinner crowd is cut into fewer.</summary>
        public float MinBossBeatSeconds = 3f;

        /// <summary>
        /// A boss walks in behind its last cluster, not inside it (round 88c, Remco: "those Cragbacks were on top of the
        /// boss at the same speed"). Until then it came out at the moment the last cluster started; on a heavy boss wave
        /// that cluster opens with the plated walkers, which walk at the boss's own pace (Cragback 0.6, Brood Mother 0.55),
        /// so they covered it from the entrance to the base. Now the boss comes out this many seconds after the last
        /// creature of that cluster: the crowd is some five cells down the road, the boss closes the wave in plain view,
        /// and its own brood runs into the space between (BalanceRules.EscortLeadCells).
        /// </summary>
        public float BossEntranceGap = 8f;

        /// <summary>
        /// The last cluster's length is read on the script's clock; in play it runs up to this much longer (the
        /// difficulty's head count times its spawn tempo: 1.16 at most on the four difficulties), so the gap holds on all.
        /// </summary>
        public float BossTailStretch = 1.2f;

        /// <summary>When the boss of a wave comes out: after the tail of the cluster that starts at <paramref name="clusterStart"/> and ends at <paramref name="tail"/> on the script's clock.</summary>
        public float BossEntrance(float clusterStart, float tail) => clusterStart + System.Math.Max(0f, tail - clusterStart) * BossTailStretch + BossEntranceGap;

        public float BreatherFor(int difficulty) => At(Breather, difficulty);

        public float BossBreatherFor(int difficulty) => At(BossBreather, difficulty);

        public float SurpriseShareFor(int difficulty) => At(SurpriseShare, difficulty);

        public float MultiRoadShareFor(int difficulty) => At(MultiRoadShare, difficulty);

        public int SurpriseSpacingFor(int difficulty) => At(SurpriseSpacing, difficulty);

        public int SurpriseFreeWavesFor(int difficulty) => At(SurpriseFreeWaves, difficulty);

        public float BeatSecondsFor(WavePattern pattern)
        {
            switch (pattern)
            {
                case WavePattern.Swarm:
                    return SwarmBeatSeconds;
                case WavePattern.ComboSwarm:
                    return ComboBeatSeconds;
                case WavePattern.TankEscort:
                    return EscortBeatSeconds;
                default:
                    return BeatSeconds;
            }
        }

        private static float At(float[] values, int difficulty) => values == null || values.Length == 0 ? 0f : values[difficulty < values.Length ? difficulty : values.Length - 1];

        private static int At(int[] values, int difficulty) => values == null || values.Length == 0 ? 0 : values[difficulty < values.Length ? difficulty : values.Length - 1];
    }
}
