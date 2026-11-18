using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// The wave grammar's picking rules (round 79, Docs/WAVE_GRAMMAR.md): which pattern a wave's front takes and which waves
    /// carry a surprise. Pure and deterministic, from the script (the level's roster and rhythm) and the difficulty alone, so
    /// a new level needs a roster and a rhythm and nothing else. <see cref="WaveComposer"/> lays the groups out by it.
    /// </summary>
    public static class WaveGrammar
    {
        /// <summary>
        /// The front pattern of a wave: an air raid where the script has one, else by the rhythm's letter (R a swarm, H a
        /// tank with its escort); a plain wave is a stream cut into clusters, and from <see cref="WaveGrammarRules.ComboFrom"/>
        /// of the level on every second plain wave is a combo swarm.
        /// </summary>
        public static WavePattern PatternOf(WaveScriptSpec script, int difficulty, int number, WaveGrammarRules grammar)
        {
            switch (WaveComposer.KindOf(script, difficulty, number))
            {
                case WaveComposer.Kind.Air:
                    return WavePattern.AirRaid;
                case WaveComposer.Kind.Rush:
                    return WavePattern.Swarm;
                case WaveComposer.Kind.Heavy:
                    return WavePattern.TankEscort;
                default:
                    int late = LatePlainWaves(script, difficulty, number, grammar);
                    return late > 0 && late % 2 == 0 ? WavePattern.ComboSwarm : WavePattern.SpreadClusters;
            }
        }

        // Plain waves from the combo phase of the level up to and including this one.
        private static int LatePlainWaves(WaveScriptSpec script, int difficulty, int number, WaveGrammarRules grammar)
        {
            int count = 0;
            for (int w = 1; w <= number; w++)
            {
                count += Phase(script, w) >= grammar.ComboFrom && WaveComposer.KindOf(script, difficulty, w) == WaveComposer.Kind.Plain ? 1 : 0;
            }

            return count;
        }

        /// <summary>How far into the level a wave is: 0 the first, 1 the last.</summary>
        public static float Phase(WaveScriptSpec script, int number) => script.Waves > 1 ? (number - 1) / (float)(script.Waves - 1) : 0f;

        /// <summary>
        /// The surprise a wave carries, if any. The level's own schedule comes first (<see cref="WaveScriptSpec.Surprises"/>);
        /// the rest is the default plan: a wave can carry one when the level has a source for it (rift troops that have
        /// joined, else a second road once the level is under way), and it is no air raid, no swarm, no boss wave and not the
        /// last wave: those are events of their own, and its share of the budget buys a real group
        /// (<see cref="WaveGrammarRules.MinSurpriseGroup"/>). The first such wave after the difficulty's surprise-free opening takes one, then the
        /// first again after the difficulty's spacing: never two surprises closer than that, never two in one wave.
        /// </summary>
        public static SurpriseKind SurpriseOn(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int number, int secondRoad, BalanceRules rules)
        {
            SurpriseKind[] plan = Plan(script, enemies, difficulty, secondRoad, rules);
            return number >= 1 && number <= plan.Length ? plan[number - 1] : SurpriseKind.None;
        }

        // The level's surprises on one difficulty, wave by wave. Remembered on the script per difficulty, flyer floors and the
        // rules that place them, because the solver composes a wave thousands of times.
        private static SurpriseKind[] Plan(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int secondRoad, BalanceRules rules)
        {
            WaveGrammarRules grammar = rules.Grammar;
            int spacing = System.Math.Max(1, grammar.SurpriseSpacingFor(difficulty));
            int free = grammar.SurpriseFreeWavesFor(difficulty);
            long key = ((long)grammar.MinSurpriseGroup << 59) | ((long)(int)(grammar.SurpriseShareFor(difficulty) * 1000f) << 49) | (grammar.Surprises ? 0L : 1L << 48) | ((long)script.FlyingTroopFloorFor(difficulty) << 40) | ((long)script.FirstAirWaveFloorFor(difficulty) << 32) | ((long)(int)(grammar.FlankFrom * 100f) << 24)
                | ((long)spacing << 16) | ((long)free << 8) | ((long)difficulty << 1) | (secondRoad >= 0 ? 1L : 0L);
            if (script.SurprisePlans.TryGetValue(key, out SurpriseKind[] plan))
            {
                return plan;
            }

            plan = new SurpriseKind[script.Waves];
            int next = free + 1;
            for (int w = 1; w <= script.Waves; w++)
            {
                // The level's own surprise for the wave comes first (SurpriseSpec): where it will, as small as it will. One
                // without entries keeps the wave free of the default too.
                SurpriseSpec authored = script.SurpriseFor(w, difficulty);
                SurpriseKind source;
                if (authored != null)
                {
                    source = grammar.Surprises && authored.Entries.Length > 0 ? SurpriseKind.Authored : SurpriseKind.None;
                }
                else
                {
                    source = grammar.Surprises && w >= next && CanCarry(script, difficulty, w) ? SourceOn(script, enemies, difficulty, w, secondRoad, grammar) : SurpriseKind.None;
                    // The default is a real group out of the level's spawn points: a wave whose share buys fewer creatures
                    // carries none, and the next wave that can takes it (round 81: never a stray creature as filler).
                    source = WaveComposer.SurpriseHeads(script, enemies, difficulty, w, secondRoad, rules, source) >= grammar.MinSurpriseGroup ? source : SurpriseKind.None;
                }

                plan[w - 1] = source;
                next = source != SurpriseKind.None ? w + spacing : next;
            }

            script.SurprisePlans[key] = plan;
            return plan;
        }

        /// <summary>Every wave (from 1) of the difficulty that carries a surprise, in order.</summary>
        public static List<int> SurpriseWaves(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int secondRoad, BalanceRules rules)
        {
            var waves = new List<int>();
            for (int w = 1; w <= script.Waves; w++)
            {
                if (SurpriseOn(script, enemies, difficulty, w, secondRoad, rules) != SurpriseKind.None)
                {
                    waves.Add(w);
                }
            }

            return waves;
        }

        // A swarm is out as well: its creatures are many and feeble, so the solver gives the wave a high health multiplier, and a
        // rift's tanks would carry it too (Glass Meadows Easy wave 11: three Cragbacks at x5.4 behind the gate ended every run).
        private static bool CanCarry(WaveScriptSpec script, int difficulty, int number)
        {
            WaveComposer.Kind kind = WaveComposer.KindOf(script, difficulty, number);
            return number < script.Waves && WaveComposer.BossOn(script, difficulty, number) == null && kind != WaveComposer.Kind.Air && kind != WaveComposer.Kind.Rush;
        }

        // Where a surprise would come from on this wave. A level with rift troops surprises through its rifts only (they are its
        // own set piece; until they have joined there is none), a level without them down its second road.
        private static SurpriseKind SourceOn(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int number, int secondRoad, WaveGrammarRules grammar)
        {
            if (HasRiftTroops(script, difficulty))
            {
                return WaveComposer.Unlocked(script, enemies, number, difficulty, true).Count > 0 ? SurpriseKind.Rift : SurpriseKind.None;
            }

            return secondRoad >= 0 && Phase(script, number) >= grammar.FlankFrom ? SurpriseKind.Flank : SurpriseKind.None;
        }

        private static bool HasRiftTroops(WaveScriptSpec script, int difficulty)
        {
            foreach (TroopSpec troop in script.Troops)
            {
                if (troop.Source != 0 && troop.MinDifficulty <= difficulty && difficulty <= troop.MaxDifficulty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
