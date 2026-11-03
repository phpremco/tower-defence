# Balance model

How the numbers of the game are made, and how to change them. Written 2026-11-02 with the balance overhaul. The aim:
balance by construction, so a new level (hand-made or generated) gets fair waves from rules instead of hand tuning.

## In one picture

```
BalanceSheet (Editor)            WaveScripts (Editor)            the level's map (LevelDefinition)
 difficulty profiles              roster, rhythm, air raids,      roads, gates, nests, boss entrance
 research/power per level         bosses per difficulty,               |
 tower prices                     crowd budget curve                   v
        \                               |                        LevelShape (fronts, trunk, gates, joins)
         \                              v                              |
          +-----> Progression -----> WaveSolver <---- Ledger <---------+
                  (what the player     (per wave: how tough the
                   owns and bought)     creatures are, on Medium)
                                         |
                                         v
                              WaveComposer -> waves of every difficulty -> checks (BalanceChecks)
```

All of this lives in `Assets/_Project/Scripts/Balance` (`Game.Balance`, no engine references), so it runs in the
generator, in the EditMode tests and in the BalanceLab console tool with the same results.

## The pieces

- **Threat** (`Threat.cs`): one threat point is 20 effective health (health + armour; armour soaks exactly its own
  points, whatever its share per hit). Flyers count 1.2x, damage to the base and structures adds a little.
  **Kill money follows threat**: `reward = 1.3 * threat^0.9`, bosses x3. No creature has a hand-set reward any more.
- **Ledger** (`Ledger.cs`, `WaveLoad.cs`): per wave, the money the player can have had (start, earlier kills less what
  leaked, the mines less one harvester), the best defence that buys from the towers the player **owns** (built and
  upgraded tower by tower; an upgrade needs the tower below it), and the pressure = wave health / what that defence
  can deal with in its window. Window = spawn span + each tower's own time in range (2 x range / speed) + a share of
  the gates' hold time. Splash counts by stream density, overkill wastes big hits on small creatures, a boss is a
  single target, a second front or a late-joining nest only meets part of the defence. Pressure 1 = the towers alone
  just hold (calibrated to the PlayMode greedy bot, `BalanceRules.Utilisation`).
- **LevelShape** (`LevelShape.cs`): built from the map. Road length, the *trunk* (the stretch every lane shares to the
  base), the number of ground fronts (roads that meet late count as separate fronts), gates on the trunk or a lane,
  and how much road is left where nest and pool creatures or the boss entrance join.
- **Progression** (`Progression.cs`): towers owned at a level = starting set + rewards of the levels before. Research
  is spent cheapest-first on upgrades of owned guns, hero and gates. The path per difficulty: Easy player = earlier
  levels on Easy; Medium = on Medium; Hard = on Medium and Hard; **Psycho = end game**: the whole campaign on Easy,
  Medium and Hard, every weapon owned (GAME_DESIGN: Psycho is for a fully upgraded commander).
- **WaveSolver** (`WaveSolver.cs`): the script fixes the *crowd* (budget curve `Budget -> FinalBudget`, which sets kill
  money); the solver only chooses each wave's **health multiplier** so the Medium pressure meets the target curve.
  Tougher creatures pay no more, so the purse cannot snowball into ever bigger waves. Waves added or changed by
  another difficulty (extra air raids, extra bosses, the air sprinkle, ambush splits) are solved on that difficulty
  against `Medium target x DifficultyPressure`. Everything else keeps the Medium multiplier with the profile on top.
- **WaveComposer** (`WaveComposer.cs`): the old generator composition (rush, heavy, air, ambush, packs, clusters over
  lanes), now pure and seeded with a stable FNV-1a hash of the level key.
- **BalanceChecks** (`BalanceChecks.cs`): what the solver does not optimise. Every wave inside its band (difficulty
  share x the highest Medium target x 1.4; bosses x1.6), no wave more than 2.6x the few before it, late Easy not empty.

## The target curve (Medium)

`TargetPressure` 0.6, rising from 0.85x early to 1.2x at the end of a level; the first four waves ramp up from 45%;
rush x1.1, heavy x1.15, air x0.9, boss x1.4, the wave after a boss x0.8. Easy lands near 0.7x Medium, Hard 1.5x,
Psycho 2.1x (`DifficultyPressure`).

## The numbers chosen (and why)

| What | Value | Why |
| --- | --- | --- |
| Research per difficulty | 1 / 2 / 3 / 5 | Easy everywhere pays 20% of all research upgrades, Psycho everywhere 100% (GAME_DESIGN, Economy) |
| Research per level | 15, 18, 22, 25, 30, 35, 42 | grows 18% a level and sums to 20% of the 940 RP the upgrades cost |
| Power per difficulty | 0 / 1 / 1.5 / 2.2 | none on Easy; Psycho everywhere pays the 186 power of all drone upgrades |
| Power per level | 4 + 3 per Medium boss | bosses pay power (GAME_DESIGN) |
| Difficulty health / count / tempo | Easy 0.68/0.8/1.2, Medium 1/1/1, Hard 1.15/1.2/0.85, Psycho 1.65/1.5/0.7 | tuned so each lands on its pressure share |
| Kill money | 1.3 x threat^0.9, bosses x3 | about a quarter less than before; the economy's main lever (below) |
| Laser / Tesla / Railgun prices | 130-90-170 / 140-115-190 / 220-150-255 | every upgrade buys at least as much damage x range per coin as the level before |
| Extra boss waves | Hard +1, Psycho +2 | GAME_DESIGN: harder difficulties add boss waves; existing waves become boss waves (no new assets) |

Boss routes: first-world bosses always come through the main gate on every difficulty (LEVEL_GUIDE). The Frozen Shelf
and the Black Marsh keep their own entrance; that is now data per boss and per difficulty (`BossSpec.Routes`, written
as `SpawnSource.Entrance` on the boss group), so a level can send a boss through the gate on Easy and up through a pool
on Psycho without code.

## What the model found (before the overhaul)

- The old ledger bought from `AvailableTowers` (all eight towers on most levels), but the bar only shows what is
  unlocked: Landing has just the Pulse Rifle, and until the Nest the only real air gun is the Pulse Rifle. It also
  bought upgrade steps without the tower beneath them.
- With an honest player, Landing was the hardest level and every later level got easier (Swamp Easy around 0.12).
  The meta economy paid Easy 30% and all difficulties 230% of the upgrades.
- **The snowball**: towers stay, so every coin of kill money keeps paying for the rest of the level. Sizing waves by
  *numbers* lets money and crowd chase each other (budgets x500 in one level). Hence the solver sets toughness, and kill
  money per threat is the lever that decides how much tougher late creatures must be: at 1.3 the median late wave
  needs creatures about 3 to 5 times their base health.

## How to retune

1. Change a number in `BalanceRules` (model and targets), `BalanceSheet` (profiles, payouts, prices) or a
   `WaveScripts` entry (roster, rhythm, crowd, bosses).
2. Check it here: `dotnet run --project Tools/BalanceLab -- solve` (prints the ledger and the checks; needs the .NET 8
   SDK). `-- write` writes the solved data into the existing assets; `-- report` judges the assets as they are;
   `-- dump <Level> <difficulty 0-3> <wave>` shows one wave's groups.
3. Or in Unity: `Tools/Generate/Scenes and Starter Data`, then the EditMode tests (`BalanceLedgerTests`,
   `BalanceModelTests`). The generator and the tool run the same code and write the same numbers.
4. After play, the first knobs: *everything too easy/hard* -> `TargetPressure`; *one difficulty off* -> its profile in
   `BalanceSheet` (then check `DifficultyPressure`); *creatures too spongy late* -> lower `MoneyPerThreat` or raise a
   script's `FinalBudget`; *calibration* -> run the PlayMode greedy bot and move `Utilisation` so pressure 1 is where
   it stops holding (the bot still builds from all available towers, not only the unlocked ones).

## For a level generator

A generated level needs: a map (roads, gates, nests, boss entrance -> `LevelShape`), a `WaveScriptSpec` (roster,
rhythm, air waves, bosses with routes per difficulty, crowd curve) and a place in the campaign. Then
`CampaignModel.BuildWaves` gives balanced waves for all four difficulties, and `BalanceChecks` says whether they hold.
Wave counts must match the existing wave assets when writing outside Unity; inside Unity the generator creates them.

## Known limits

The model has no placement, drones, path objects, Cryo slow, Support boost, boss powers (minions, splits, regrowth)
or tower damage. Base turrets and base health upgrades are bought but not counted. Air raids against a roster with
only the Pulse Rifle (Forest to Ruins) are sized by the solver, so they are fair in the model, but the unlock order
(Rocket only after the Ruins) is a design choice worth revisiting.
