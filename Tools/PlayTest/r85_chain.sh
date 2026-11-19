#!/usr/bin/env bash
# Round 85: the final chain after the last data change: full measurement, finale probe, grace measurement.
cd "$(dirname "$0")/../.."
log=Tools/PlayTest/r85_chain.log
echo "== chain start $(date +%H:%M)" > "$log"
PR_WORKERS=5 bash Tools/PlayTest/r85_measure.sh r85_final5 >> "$log" 2>&1
echo "== finale $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/finale_workers.sh >> "$log" 2>&1
cp Tools/PlayTest/finale_probe.txt Tools/PlayTest/finale_probe_r85_final.txt
echo "== grace measure $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers.sh run measure 5 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
echo "CHAIN DONE $(date +%H:%M)" >> "$log"
