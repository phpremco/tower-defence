#!/usr/bin/env bash
# Round 81: the gates after the full pressure measurement, one after the other on the worker clones.
# Waits for the pressure batch (its log ends with the save check), then: finale probe, grace measurement of all 24
# cells, replay of all 24 cells, the acceptance report on w6. The grace report and the whole EditMode suite follow by hand (Docs/COMMANDS.md, Round 81). Log: Tools/PlayTest/r81_chain.log.
cd "$(dirname "$0")/../.." || exit 1
log=Tools/PlayTest/r81_chain.log
: > "$log"
until grep -q "save untouched\|the save changed" Tools/PlayTest/r81_final.log 2>/dev/null; do sleep 20; done
sleep 10
echo "== finale start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/finale_workers.sh >> "$log" 2>&1
cp Tools/PlayTest/finale_probe.txt Tools/PlayTest/finale_probe_r81.txt
echo "== measure start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers_keepsave.sh run measure 5 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
echo "== replay start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers_keepsave.sh run replay 5 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
cp Tools/PlayTest/plan_records.txt Tools/PlayTest/plan_records_r81_replay.txt
echo "== acceptance start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/r34b_editmode_worker.sh 6 BalanceAcceptanceTests >> "$log" 2>&1
cp /c/Users/remco/TowerDefenseWorkers/w6/Tools/PlayTest/acceptance.txt Tools/PlayTest/acceptance_r81.txt
# The replayed plans are kept aside; the two files go back to HEAD's (plan_records.txt is part of the bot stamp).
git show HEAD:Tools/PlayTest/plan_records.txt > Tools/PlayTest/plan_records.txt
git show HEAD:Tools/PlayTest/plan_replay_partial.txt > Tools/PlayTest/plan_replay_partial.txt
# The grace report needs the open editor (GraceReport.cs) and the whole EditMode suite reads its rows: both follow by hand.
echo "== chain done $(date +%H:%M)" >> "$log"
