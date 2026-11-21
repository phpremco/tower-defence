# Round 88b, package "bossescort": doc text for the curator to merge

## Docs/WAVE_GRAMMAR.md, section "The solver (round 87: a formula)", new point after "2. A level's boss"

2b. **A level boss's brood (round 88b: the escort rule).** Remco's playtest of round 87: "the boss wave feels pathetic
   ... once the boss passed the first defence it felt like 5 minutes until it reached the end; it sends little ants
   that die before the last gate". Every level boss (a boss without boss data; `EscortRule.Binds`) sends a brood out
   of its body along its walk, by one rule:

       brood health over an unhindered walk, spawn to base = EscortWorth[d] x the boss's bar as spawned

   - `EscortWorth` is a difficulty factor (`DifficultyProfile._escortWorth`, from `BalanceSheet`): 0.4 / 1 / 1.1 / 1.2.
     No number per level or per boss: a longer road, a bigger boss or another creature all get the same share.
   - **It grows along the road.** Every call (the boss's one Spawn power: its interval and first delay) grants the
     budget of the seconds since the last call, weighted by how far down its road the boss is: from
     `EscortStartWeight` 0.4 times the mean rate at the spawn to `EscortEndWeight` 1.6 at the base, a straight line
     (`EscortBrood.Weight`). With these, 35 % of the brood comes in the first half of the walk and 65 % in the second;
     a boss killed at a first gate at 13 % of its road has sent 6 %. The budget buys whole creatures; the rest
     carries to the next call. A call that buys nothing makes no sound.
   - **As tough as its own wave.** A brood creature walks in with the multiplier of the ordinary creatures of the
     boss's own wave (difficulty health x level scale x the wave's walk-in multiplier), remembered at the boss's
     spawn: a later wave starting while the boss lives does not change it. (Before: the multiplier of whichever
     wave was under way.)
   - **A plated share.** `EscortHeavyShare` 0.25 of the brood walks in as the toughest plated ordinary walker the
     level has sent before the boss (`EscortRule.Tougher`; on Crystal Plains the Cragback), so the brood is not only
     food for a blast. Only when that share of the whole walk pays for at least one, never for a boss that flies,
     and never a creature the level has not shown.
   - **A cap.** A boss held at a gate or slowed keeps calling at the rate of where it stands, up to
     `EscortMostOfWorth` 1.5 times the whole walk's worth.
   - **No bounty.** `EscortBountyShare` 0: a brood creature pays nothing (before: full bounty, 37 to 110 coins per
     brood mother walk). It counts for the wave's end like any creature and bites gates and the base as its kind does.
   - The creature per boss is data (its Spawn power's minion): Brood Mother and Hive Queen Skitters, Storm Matriarch
     Mossbats, Slime Monarch Oozers (they had these), and new in round 88b Pack Alpha Prowlers (`PackCall`), Core Wisp
     Wisps (`WispWake`), Acid Empress Stingers (`StingerGuard`), Crystal Warlord Shard Runners (`ShardGuard`), Sky
     Sovereign Skyrays (`RayWing`), Roller King Rollers (`RollerPack`) (`ProjectGenerator.Escorts.cs`,
     `ApplyRound88bEscort.cs`). The power's own count is not read under the rule.
   - **Continent bosses are outside the rule**: their calls keep count, interval, creature, bounty and
     `DifficultyProfile._bossEscortShare` (1 / 1 / 1 / 0.5) to the number (`EscortRuleTests.AContinentBoss_KeepsItsOwnCall`).
     Side effect removed: until round 88a Psycho's 0.5 also halved the brood of a LEVEL boss (the field was meant
     for the finale); level bosses no longer read it.

   Before the rule (lab `Tools/PlayTest/r88b_escort.txt`): the brood mother's plain call (5 Skitters of 11 book
   health every 10 s) came to 43 to 56 % of her bar on Easy to Hard and 24 to 33 % on Psycho, spread evenly over
   the walk; the Storm Matriarch's bats 28 to 43 %; Core Wisp and Pack Alpha sent nothing on a timer. The first gate
   stands at 10 to 14 % of the boss's road on all six levels, so 85 to 90 % of the old brood already came behind
   it: the complaint is not how much health came but that it was paper, evenly thin, and the same at the end as at
   the start. After, as a walk realises it (the last call falls before the base, and a plated creature not yet
   paid for is not sent): Easy 35 to 40 % of the bar, Medium 85 to 99 %, Hard 95 to 110 %, Psycho 102 to 121 %;
   59 to 76 % of it in the second half of the walk (means 67 to 73 % per difficulty).

   What to turn (curator, after the bots):
   - `BalanceSheet.*EscortWorth` (then `ApplyRound88bEscort.Run`): how much brood a difficulty gets. The one knob
     for "the boss wave is a wall" (down) or "still dull" (up).
   - `BalanceRules.EscortStartWeight` / `EscortEndWeight`: how late it comes. 1 / 1 is even; 0.4 / 1.6 is the
     stored slope; 0.2 / 1.8 puts 70 % in the second half. They change no total.
   - `BalanceRules.EscortHeavyShare`: how much of it survives a blast (plated). 0 is ants only.

## Docs/WAVE_GRAMMAR.md, section "How the balance model stays honest", new paragraph

**The brood of a level boss (round 88b).** `WaveLoad.Of` adds it to the boss wave as a stream of
`floor(EscortWorth[d] x boss bar / creature bar)` creatures of the boss's Spawn creature (`EnemyStats.RuledEscort`,
filled by `BalanceLedger.Stats` for a boss without boss data), joining its road at `BalanceRules.EscortStartShare`
(0.6 with the stored weights; a continent boss's call keeps `SummonStartShare` 0.5 and its rate). The brood of a boss that FLIES (the Storm
Matriarch's bats: 7 of the 42 stored boss walks) is not in the ledger: added to the air health it moved the
ledger's purchase and broke `Round83Tests.AWavesPressure_NeverFalls_AsItsCreaturesGrowTougher` by a hair, and the
air side has no stream that joins mid-road; the bots meet it, the paper does not. Also not modelled: the plated share (the whole worth is counted in the light
creature's body), the cap, and that a boss killed early sends less (the ledger counts the whole walk: conservative).
The brood pays no kill money in play and none in the ledger. The wave formula and `BossYardstick` do not read the
brood: a boss's bar and its wave's multiplier are as round 87 set them. `FinaleModel` is untouched (continent bosses
only). `boss_share.py` needs no change (it counts lost runs per wave). The wave preview lists the wave's groups, as
before; the brood's creature is in the level's roster (`LevelRoster`), the plated one is a creature of the level's
own waves. `InputHash` takes the escort constants, the worth per difficulty and the brood creature per level boss,
so the calibration stamp is stale until the curator re-stamps.

## Docs/COMBAT_RULES.md (bosses), one line

A level boss's brood is sized by the escort rule (WAVE_GRAMMAR, "The solver", 2b): worth per difficulty times the
boss's bar, growing along its road, a quarter plated where the level has shown a plated walker, no bounty.

## Docs/STATUS.md, round 88b, package "bossescort"

Files: new `Enemies/EscortBrood.cs`, `Enemies/EscortSettings.cs`, `Levels/EscortRule.cs`,
`Editor/ProjectGenerator.Escorts.cs`, `Tests/EditMode/EscortRuleTests.cs`, `Tools/PlayTest/Scripts/Round88bEscortLab.cs`,
`Tools/PlayTest/Scripts/ApplyRound88bEscort.cs`; edited `Enemies/BossAbilities.cs` (SetEscortRule, StageEscort,
ReleaseEscort), `Levels/EnemySpawner.cs` (stages a level boss at launch, SpawnEscort, the toughest plated walker
met), `Levels/DifficultyProfile.cs` (`_escortWorth`), `Levels/BalanceLedger.cs` (AddEscort, EscortWorth),
`Balance/BalanceRules.cs` (five Escort constants), `Balance/BalanceStats.cs` (RuledEscort, EscortWorth),
`Balance/WaveLoad.cs`, `Balance/InputHash.cs`, `Editor/BalanceSheet.cs`, `Editor/ProjectGenerator.cs` (one line),
`Editor/ProjectGenerator.Bestiary.cs` (one call), `Resources/Localization/en.data.txt` (six names). Data: six new
ability assets, six boss assets with one more ability, four difficulty assets.
