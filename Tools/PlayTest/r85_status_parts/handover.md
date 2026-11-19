
## Round 85 – handover (2026-11-19, 09:58)

State: HEAD 00c4ed1c, nothing committed. **All of round 85's code and data lives in the stage**
(`C:\Users\remco\TowerDefenseWorkers\stage`); the project holds only the round's Docs (COMMANDS "Round 85",
COMBAT_RULES, WAVE_GRAMMAR, this entry) and the three save-rule scripts. Save MD5 d5b04a5d2834425dc4bb6c8c33b4d036,
never written. Bot: one pick changed (Landing Site Easy, by hand in the stage's `skilled_picks.txt`, with a comment).

Done in the stage (see the draft entry in `Tools/PlayTest/r85_status_parts/`: s1, status_r85_body, s2, be_table, s3,
boss_table, s4, to be concatenated in that order into the Round 85 entry after the tables are refreshed):
- Easy/Medium targets x1.55 / x1.32 (`closed_loop.py`, `ClosedLoopAcceptanceTests`), ladders in `r85_measure.sh`.
- Boss armour 0.25 of the body (`BalanceRules.BossArmourShare`, generator, `ApplyRound85.cs`, finale stage moves the
  armour with the body); `BossOverRegular` 3 (solver floor, both boss floors off the wave's lift; a boss wave over
  its cap: crowd to the lower edge of its band, then the boss towards its floor).
- Laser: `IHitBudget`, `Tower.ReleaseBeam` (the killing pulse costs its needed share).
- Continent bosses: Gravemaw 46400, Cinder 12100, Warden 5600, Rotbloom 14000, Shattered 15200, Sealed One 29200,
  armour 0.25 of each.
- `DifficultyScale` 0.982 / 1.369 / 0.913 / 0.918 (Hard folded); scalars in `Tools/PlayTest/wave_tuning.txt`.
- Tests changed (reasons in the draft, s4): ClosedLoopAcceptance targets, BalanceLedger boss-on-its-multiple waves
  reported not failed, WaveGrammar surprise bounds (swarm 1.35, boss wave 1.4), Continent1Content boss armour.
- Throttle tools: `lower_priority.sh/.ps1`, `stage.sh` fixes, `closed_loop.py` one-seed reading.

**Why not finished:** the last solver change (round 85's crowd goes to the lower edge of its band instead of its
floor, because with the crowd on its floor a Gun Turret row won Landing Site Easy and the grace gate broke) moved the
boss waves. Its measurement `pressure_r85_final4_*` (data 66f9d292's successor, named in `closed_loop_records.txt`)
reads Glass Meadows Psycho under x0.75 (three steps tough), Landing Site Psycho x0.85 and Glass Meadows Hard x0.85
(two tough); everything else within a step (`closed_loop_r85_final4.txt`). A chain (`r85_chain.sh`, log
`r85_chain.log`) was still running the finale probe and the grace measurement on that data at 09:58.

To do, in this order (all from inside the stage; five clones allowed; check `unity command editor_status` of the
open editor only before step 5):
1. Wait for `CHAIN DONE` in `Tools/PlayTest/r85_chain.log` (or kill the chain's bash first, then its Unity workers).
2. `python -I Tools/PlayTest/closed_loop.py Tools/PlayTest/pressure_r85_final4_easy_records.txt Tools/PlayTest/pressure_r85_final4_medium_records.txt Tools/PlayTest/pressure_r85_final4_records.txt --step 1.12 --write`;
   then by hand: never toughen Easy (put back any Easy scalar the loop raised), Quartz Flats Hard stays 0.7, bisect
   where two passes straddle the target (wave_tuning_r85_*.txt hold the earlier scalars). Solve:
   `bash Tools/PlayTest/r85_cycle.sh r85_cycle8.log` (stage editor: compile, ApplyRound68, stop; then the key test
   classes on w6).
3. `bash Tools/PlayTest/r85_chain.sh` again after changing its name `r85_final4` to `r85_final5` (full measurement 62
   min, finale probe 6 min, grace measurement 22 min). Then the GraceReport in the stage editor
   (`stage.sh start`, `stage.sh run GraceReport.cs GraceReport.Run`, `stage.sh stop`): the gate must hold (no BREAK;
   round 85 measured 51 in time, 37 late inside allowances before the last change).
4. Gates on w6: `r34b_editmode_worker.sh 6 ClosedLoopAcceptanceTests` (24 of 24 wanted, within a step), then the
   whole suite `r34b_editmode_worker.sh 6 "Game\.Tests\..*"` (`editmode_failures.py`: 0 failed wanted).
5. `bash Tools/PlayTest/stage.sh back` (refuses while Remco plays), then `unity command refresh` / `recompile` in the
   open editor and `console_status` (0 compile errors). The stage's ProjectSettings are not copied (product name).
6. Refresh the tables: `be_table.md` (from closed_loop of the final records), `boss_table.md`
   (`boss_vs_regular.py` before = the project's old data from git, after = the stage), the continent bosses and the
   finale line in s4 (`finale_probe_r85_final.txt`), replace GATES_REST in s4 with the grace result, compile and run
   times; then append s1 + status_r85_body + s2 + be_table + s3 + boss_table + s4 to Docs/STATUS.md as the "Round 85"
   entry and report.
