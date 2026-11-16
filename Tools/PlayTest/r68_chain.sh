#!/usr/bin/env bash
# Round 68: after a solve in the open editor (ApplyRound68.cs), the rest of the pipeline for a few cells on the clones only:
# the yardstick on the cells (w1..w5, merged into calibration_records.txt), the plan search per cell on w1..w6 side by side
# (that cell's plan files copied back), the replay of the cells (w1..w5, merged into plan_records.txt) and the acceptance
# report on w6 (copied back). The grace report needs the editor: run it after (balance_pipeline.sh grace).
# Usage: bash Tools/PlayTest/r68_chain.sh "0:0,0:1,1:1" > Logs/r68/chainN.log 2>&1
set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="$(cygpath -u "${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}")"
cells="$1"
keys=(C1_01 C1_02 C1_03 C1_04 C1_05 C1_06)
mkdir -p Logs/r68
echo "== chain: measure cells=$cells ($(date +%H:%M:%S))"
bash Tools/PlayTest/workers.sh run measure 5 "cells=$cells" | tail -12
echo "== chain: search ($(date +%H:%M:%S))"
IFS=',' read -r -a list <<< "$cells"
i=0
while [ $i -lt ${#list[@]} ]; do
  batch=("${list[@]:$i:6}")
  w=1
  for cell in "${batch[@]}"; do
    bash Tools/PlayTest/r34b_editmode_worker.sh $w PlanSearchTests "cells=$cell" > "Logs/r68/search_w$w.log" 2>&1 &
    w=$((w + 1))
  done
  wait
  w=1
  for cell in "${batch[@]}"; do
    node="${cell%%:*}"; diff="${cell##*:}"; level="Level_${keys[$node]}"; clone="$ROOT/w$w/Tools/PlayTest/Plans"
    ls "$clone/${level}_${diff}.txt" > /dev/null
    rm -f "Tools/PlayTest/Plans/${level}_${diff}.txt" Tools/PlayTest/Plans/${level}_${diff}_strategy*.json
    cp "$clone/${level}_${diff}.txt" Tools/PlayTest/Plans/
    cp "$clone"/${level}_${diff}_strategy*.json Tools/PlayTest/Plans/ 2> /dev/null || true
    echo "  w$w cell $cell: $(grep -E "^${level} " "$ROOT/w$w/Tools/PlayTest/plans_summary.txt" | head -1 | cut -c1-150)"
    w=$((w + 1))
  done
  i=$((i + 6))
done
echo "== chain: replay cells=$cells ($(date +%H:%M:%S))"
bash Tools/PlayTest/workers.sh run replay 5 "cells=$cells" | tail -12
# A worker's records also carry the cells it did not replay (copied with the project), so a cell outside the filter can come
# back once per worker: keep one block per run (level, difficulty, strategy, seed, kind). The copies are identical.
awk -F'|' '$1=="run"{k=$2"|"$3"|"$4"|"$5"|"$13; keep=!(k in seen); seen[k]=1} {if(keep)print}' Tools/PlayTest/plan_records.txt > Tools/PlayTest/plan_records.dedup
mv Tools/PlayTest/plan_records.dedup Tools/PlayTest/plan_records.txt
echo "  plan records after dedupe: $(grep -c '^run|' Tools/PlayTest/plan_records.txt) runs"
echo "== chain: acceptance ($(date +%H:%M:%S))"
bash Tools/PlayTest/r34b_editmode_worker.sh 6 BalanceAcceptanceTests | head -2
cp "$ROOT/w6/Tools/PlayTest/acceptance.txt" Tools/PlayTest/acceptance.txt
echo "== chain done ($(date +%H:%M:%S))"
