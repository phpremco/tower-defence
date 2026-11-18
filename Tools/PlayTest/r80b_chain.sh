#!/usr/bin/env bash
# Round 80b: the gates after the full pressure measurement, one after the other on the worker clones.
# Waits for the pressure batch (its log ends with the save check), then: finale probe, grace measurement of all 24
# cells, replay of all 24 cells, the acceptance report on w6. Log: Tools/PlayTest/r80b_chain.log.
cd "$(dirname "$0")/../.." || exit 1
log=Tools/PlayTest/r80b_chain.log
: > "$log"
until grep -q "save untouched\|the save changed" Tools/PlayTest/r80b_final.log 2>/dev/null; do sleep 20; done
sleep 10
echo "== finale start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/finale_workers.sh >> "$log" 2>&1
cp Tools/PlayTest/finale_probe.txt Tools/PlayTest/finale_probe_r80b.txt
echo "== measure start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers_keepsave.sh run measure 5 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
echo "== replay start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers_keepsave.sh run replay 5 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
cp Tools/PlayTest/plan_records.txt Tools/PlayTest/plan_records_r80b_replay.txt
echo "== acceptance start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/r34b_editmode_worker.sh 6 BalanceAcceptanceTests >> "$log" 2>&1
cp /c/Users/remco/TowerDefenseWorkers/w6/Tools/PlayTest/acceptance.txt Tools/PlayTest/acceptance_r80b.txt
# The replayed plans are kept aside; the two files go back to HEAD's (plan_records.txt is part of the bot stamp).
git show HEAD:Tools/PlayTest/plan_records.txt > Tools/PlayTest/plan_records.txt
git show HEAD:Tools/PlayTest/plan_replay_partial.txt > Tools/PlayTest/plan_replay_partial.txt
echo "== editmode start $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/r34b_editmode_worker.sh 6 Game.Tests >> "$log" 2>&1
python Tools/PlayTest/editmode_failures.py > Tools/PlayTest/r80b_editmode_full.txt 2>&1
echo "== chain done $(date +%H:%M)" >> "$log"
