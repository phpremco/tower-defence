# Balance critic – 2026-11-19 (after round 86, 4dd233d6)

Independent read-only review. Verdict: the architecture is sound (rules -> solver -> one measured correction), but the correction does the real work and is fitted to a bot that plays a different game (no Laser). Round 86 moved difficulty out of the waves into a boss wall. Do not generate level 7 yet.

## Findings (most important first)
1. **Bot is no human proxy**: at x1 the bot builds Gun 51-68%, Cannon 20-36%, Flak 12-17%, Laser 0-3%; "skilled" breaks before "sensible" in 7 Medium/Hard cells. Add Laser-first and Cannon-heavy archetype rungs; target the spread between archetypes.
2. **Levels 1-2 are a no-fail corridor + boss wall**: all Landing Site losses in wave 12/12, Glass Meadows 47/50, Easy 11/12 in the final wave. LS Easy boss 1014 -> 2109 while crowds got softer. Cap boss-wave loss share ~35%, add 2-3 mid-level peaks.
3. **24 scalars ~ level effect + noise**: level explains 63% of log variance, difficulty 2%, residual 35% (~±11%, one ladder step). Stop rules fail (76% monotone vs 95%). DifficultyScale moved 25-30% in one round. Use one scalar per level; fix mispriced flying boss (FL) and long road + late swarms (QF).
4. **Boss rule = three regimes**: standard x3 light / x6.5 bar; early book-floor bosses x15-36 bar; after surprise x1.46 (CV Psycho). Hard bosses smaller than Medium on 5/6 levels (LS 2614 vs 3070). Size boss monotone in level index and difficulty; keep 3x toughest walker as floor only.
5. **Boss armour too light to ask for a tower**: boss 20% armour vs Cragback 73%; LS Easy boss costs light fire ~3797 dmg (42/100 coins) vs Cannon 2109 (21/100): roughly a tie. Raise BossArmourShare toward 1.0; a human should look at phases before more HP staging (Gravemaw 46400 -> 21000 in one round; Sealed One by hand). Three of six final bosses are the same Brood Mother.
6. **Tower roles**: Laser = Gun Turret's job at x1.08-1.09 per coin; Cannon L3 ~ Laser L3 single target plus splash; Railgun clears armour slower than Cannon (45.3 s vs 17.0 s per 100 coins). Give beams their own armour row (0.35) or the boss-body job; re-measure after finding 1.
7. **Gates following data**: 36/94 grace rows late with 19 allowances (up to 7 waves on a 3-wave window); boss cap ceiling 2.6 set just over measured 2.50; "better rung >= x1.06" accepted with both rungs <= x0.95 (LS Hard); Easy censored ("over x1.86") with no upper bound; NoStoredGroundWave_IsPaper redefined on failure; pressure stop-rule FAILs unconsumed. Freeze thresholds before measuring, add Easy upper bound, delete measured-only allowances.
8. **Lever count**: ~95 BalanceRules fields (~14 per-difficulty arrays), 8 solver constants, WaveGrammarRules, 24 scalars, 4 DifficultyScale, 19 allowances, 6 hand-staged boss HPs, per-level surprises. Simplest model: health = book x difficulty x ramp(wave phase) x level scalar; boss = f(level, difficulty); per-difficulty composition biases (~15 numbers + one scalar per level).

## Next three, in order
1. Remco plays round 86 (LS Easy and Hard, GM Medium): boss too big, waves before it too flat?
2. Fix the proxy (Laser/Cannon archetype rungs), boss-wave loss-share cap, mid-level peaks; refit with per-level scalars only.
3. Clean the gates, make boss size monotone, then generate level 7 as the real "no hand work" test.
