#!/usr/bin/env bash
# Round 86: the final chain on data that stands still: the full measurement (three ladders, 756 runs), then the
# measurement the grace report reads. The finale probe ran before it (a boss health it moves is data).
cd "$(dirname "$0")/../.."
name="${1:-r86_final}"
log=Tools/PlayTest/r86_chain.log
echo "== chain start $(date +%H:%M)" > "$log"
PR_WORKERS=6 bash Tools/PlayTest/r85_measure.sh "$name" >> "$log" 2>&1
echo "== grace measure $(date +%H:%M)" >> "$log"
bash Tools/PlayTest/workers.sh run measure 6 "difficulties=0,1,2,3" "seeds=1,2,3" >> "$log" 2>&1
echo "CHAIN DONE $(date +%H:%M)" >> "$log"
