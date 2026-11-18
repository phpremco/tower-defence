#!/usr/bin/env bash
# Round 82: the full pressure measurement and the gates after it, one after the other on the worker clones:
# 720 pressure runs, the grace measurement of all 24 cells, the replay of all 24 cells, the acceptance report on w6.
# The grace report (GraceReport.cs, open editor) and the whole EditMode suite follow by hand (Docs/COMMANDS.md, Round 81).
# Do not edit anything under Assets, Packages, ProjectSettings or Tools while it runs. Log: Tools/PlayTest/r82_chain.log.
cd "$(dirname "$0")/../.." || exit 1
log=Tools/PlayTest/r82_chain.log
: > "$log"
echo "== pressure start $(date +%H:%M)" >> "$log"
PR_RECORDS=Tools/PlayTest/pressure_r82_final_records.txt bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0,1,2,3" > Tools/PlayTest/r82_final.log 2>&1
echo "== measure start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers.sh run measure 5 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
echo "== replay start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers.sh run replay 5 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
cp Tools/PlayTest/plan_records.txt Tools/PlayTest/plan_records_r82_replay.txt
echo "== acceptance start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/r34b_editmode_worker.sh 6 BalanceAcceptanceTests >> "$log" 2>&1
cp /c/Users/remco/TowerDefenseWorkers/w6/Tools/PlayTest/acceptance.txt Tools/PlayTest/acceptance_r82.txt
# The replayed plans are kept aside; the two files go back to HEAD's (plan_records.txt is part of the bot stamp).
git show HEAD:Tools/PlayTest/plan_records.txt > Tools/PlayTest/plan_records.txt
git show HEAD:Tools/PlayTest/plan_replay_partial.txt > Tools/PlayTest/plan_replay_partial.txt
echo "== chain done $(date +%H:%M)" >> "$log"
