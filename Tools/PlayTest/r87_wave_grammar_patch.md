# Patch for Docs/WAVE_GRAMMAR.md (round 87)

Replace the section "## The solver (round 83)" up to "## What a wave is made of" by the text below, and in
"## How to balance a new level" apply the edits listed at the end.

## The solver (round 87: a formula)

A wave's health is no longer solved against the paper ledger. `WaveSolver.Solve` applies `WaveFormula`
(called by `CampaignModel.BuildWaves`); no purse, no defence and no other wave is read.

1. **A wave.** The multiplier its creatures walk in with (after the composer's lift for a capped troop, as before):

       walkIn = DifficultyScale[d] x LevelScalar[L] x Ramp(phase) x KindFactor x Peak

   - `DifficultyScale` (1.5 / 1.85 / 1.23 / 2.38): the multiplier of a level's middle wave per difficulty.
   - `LevelScalar`: one number per level in `Tools/PlayTest/wave_tuning.txt` (`C1_01 0.7`), set by the closed loop,
     held to 0.7 .. 1.4 (`LevelScalarLeast`, `LevelScalarMost`).
   - `Ramp`: geometric in the wave's phase `(n-1)/(N-1)` from `RampStart` 0.75 to `RampEnd` 1.34. Fitted on the
     plain ground waves round 86 stored (`Tools/PlayTest/r87_fit.py` on `r87_dump_r86.txt`, 244 waves of 24 cells):
     geometric in phase leaves an rms residual of x1.373, geometric in wave number x1.382; a slope per difficulty
     x1.343 (not kept: three more numbers for 3 %). The residual is large because round 86's waves jumped from
     wave to wave (band, caps, purse); the fit only sets where the first measurement starts.
   - `KindFactor`: rush 1.1, heavy 1.15, raid 0.9 (`RushFactor`, `HeavyFactor`, `AirFactor`), times
     `AfterBossFactor` 0.8 on the wave after a boss, times `BossCrowdFactor` 0.8 on the crowd of a boss wave.
   - `Peak`: `PeakFactor` 1.3 on two peak waves per level and difficulty, three on a level of `LongLevelWaves` (16)
     waves or more. `PeakPhases` 0.4 and 0.7 (`LongLevelPeakPhase` 0.55). Every peak first lands on the wave
     nearest its phase that is a ground wave without a boss and not the breather after one, no two side by side;
     then a peak moves to a neighbour that carries a surprise or is a heavy wave when that keeps it clear of the
     other peaks (`WaveFormula.Peaks`, `WaveSolver.PeaksOf`). In two steps so that a surprise can draw a peak onto
     its own wave and never pushes one onto another wave. Remco: waves before the boss must be able to hurt.
   - A raid keeps `RaidHealthFloor`; the flyer rules (the Easy purse rule, `FlyersAfterBounties`) are unchanged and
     still read the ledger's purse to decide WHEN flyers come. Nothing else reads the ledger for sizing.
   - A surprise wave gets the formula like any wave and makes no other wave tougher (it can draw a peak onto
     itself, which leaves the neighbour that would have carried it an ordinary wave;
     `WaveGrammarTests.ASurprise_MakesNoOtherWaveTougher`).
2. **A level's boss.** Its bar as spawned (body plus armour):

       bossBar = BossBase x BossDifficulty[d] x (1 + BossLevelGrowth x L) x (BossEarlyShare + (1 - BossEarlyShare) x phase) x (flies ? FlyingBossShare : 1)

   `BossBase` 2300, `BossDifficulty` 0.7 / 1 / 1.15 / 2.0 (never falling), `BossLevelGrowth` 0.12 per level of the
   continent, `BossEarlyShare` 0.3, `FlyingBossShare` 0.7. It does not move with the level scalar or
   `DifficultyScale`. **Floor only:** never under `BossOverRegular` (3) times the bar (body plus armour) of the
   toughest ordinary walker met before it (`BossYardstick`; flyers are not met, a surprise wave creatures count as
   they walk in). On the round 87 data the floor binds on 3 of 42 bosses, all earlier Psycho bosses, and on no last
   boss. `BossArmourShare` is 1: armour pool equal to the body (`ApplyRound85.cs`).
   Gone: the light-fire row of the yardstick (`LightFireOnArmour`, `LightFireInTheAir`), the book floor
   (`BossBookShare`), the calm reading of a surprise wave, the separate rule for a flying boss, the boss held
   against its own wave.
3. **Play's correction** is the level's scalar and nothing else. `closed_loop.py` sets it from the geometric mean
   of the level's four cells; what the difficulties still disagree on goes to `DifficultyScale` (`--centre`).

Gone with the solve: the target curve (`TargetPressure`, `OpeningShare`, `OpeningWaves`, `EarlyTargetShare`,
`LateTargetShare`, `BossFactor`), the band and the median caps (`MaxRise`, `MaxDrop`, `MostOverMedian`,
`LeastOfMedian`, `MinHealth`, `MaxHealth`), the hero cap on a wave, the bisection, the calm reference solve
(`CampaignModel.CountsAs`, `CalmOf`), `ArmourAim` and the raid's air growth in the target, the paper band checks
(`BalanceChecks.Check`, `CheckHumanTarget`, `MaxPressure`, `DifficultyPressure`, `BandMargin`, `BossAllowance`,
`MaxCliff`, `MinLateEasyPressure`, `HumanTargetTolerance`), the builder stages that moved a scale per cell
(`BalanceStages.Build`, `BuildFromVerdicts`). The ledger stays as the paper model of the plan search, the finale
model and the reports (`balance_ledger.txt`).

Reports: `stage.sh run Round87Lab.cs Round87Lab.Run` writes `Tools/PlayTest/r87_waves.txt` (every wave's walk-in
multiplier with kind, peak, boss, raid, surprise) and `r87_bosses.txt` (every level boss: bar, body, armour, the
formula bar, times the toughest walker before it under both rows, whether the floor binds).

Tests: `WaveFormulaTests` (the formula, the ramp, the peaks, the boss formula and its floor, every stored wave on
the formula, two or three peaks per cell, every stored boss on formula or floor, bosses growing with difficulty and
with the level, the scalar moving no boss on its formula), `Round83Tests.NoStoredGroundWave_IsPaper` (against the
median of the whole level again), `ALevelsScalar_LiesNearOne`, `TheTuningFile_HoldsALevelsScalar_AndNothingElse`.

## Edits to "How to balance a new level"

- Step 1: "the level starts at scalar one" (not "the cell"); `wave_tuning.txt` takes the level's scalar.
- Step 4/6: measure with `bash Tools/PlayTest/r87_measure.sh <name> "<node>"` (rungs skilled, sensible, laser,
  cannon; Easy on x1.25 / 1.4 / 1.57 / 1.76 / 1.97 / 2.21, Medium on x0.9 .. x1.57, Hard and Psycho standard).
- Step 5: `python -I Tools/PlayTest/closed_loop.py <the three records files> --levels <node> [--centre] [--write]`:
  new level scalar = old x geometric mean over the level's four cells of measured / target. Targets and gates are
  frozen in `Tools/PlayTest/closed_loop_thresholds.txt` (2026-11-20): Easy weaker rung x1.66, Medium sensible
  x1.32, Hard sensible x0.95, Psycho skilled x1.06; accepted within one ladder step on both sides (Easy too, no
  "over the ladder"); Medium and Hard: the best of skilled / laser / cannon itself reads x1.06; laser and cannon
  within one step of each other; at most 40 % of a level's lost runs in a boss wave (aim 35 %).
- Step 7: `ClosedLoopAcceptanceTests` reads the same thresholds file; `python -I Tools/PlayTest/boss_share.py
  <records>` shows where the lost runs fall (boss waves, last wave, peak waves).
- Step 8: `--centre` prints `DifficultyScale` times the difficulty's mean residual; the level scalars need no division.
