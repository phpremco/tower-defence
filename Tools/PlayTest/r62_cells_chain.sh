#!/usr/bin/env bash
# Round 62: measure, search and replay a handful of cells after their waves moved (a builder step in wave_tuning.txt and a
# solve), all on the worker clones; the open editor is not used. Usage, cells as node:difficulty (node 0 = C1_01):
#
#   bash Tools/PlayTest/r62_cells_chain.sh 2:3 3:2 4:3 > Logs/r62/chain.log 2>&1 &
#
# 1. the yardstick of those cells on the workers (workers.sh run measure, merged into calibration_records.txt);
# 2. the strategy search, one clone per cell side by side (r34b_editmode_worker.sh N PlanSearchTests "cells=n:d"), the cell's
#    plan files copied back over the project's (the old files of the cell deleted first: the search deletes stale ones only
#    in its own folder) and its summary row appended to Tools/PlayTest/r62_plans_summary.txt;
# 3. the replay of those cells on the workers (merged into plan_records.txt).
# Afterwards: fit, apply (or apply-keep) and acceptance. The clones must be idle.
set -euo pipefail
cd "$(dirname "$0")/../.."
[ "$#" -gt 0 ] || { sed -n '2,12p' "$0"; exit 1; }
ROOT="$(cygpath -u "${TD_WORKERS_ROOT:-C:\\Users\\remco\\TowerDefenseWorkers}")"
cells="$(IFS=,; echo "$*")"
mkdir -p Logs/r62

echo "== chain: measure cells=$cells ($(date +%H:%M:%S))"
bash Tools/PlayTest/workers.sh run measure "cells=$cells"

echo "== chain: search ($(date +%H:%M:%S))"
n=0
for cell in "$@"; do
  n=$((n + 1))
  bash Tools/PlayTest/r34b_editmode_worker.sh "$n" PlanSearchTests "cells=$cell" > "Logs/r62/chain_search_w$n.log" 2>&1 &
  # At most five clones at once, as the PlayMode stages use.
  if [ $((n % 5)) = 0 ]; then wait; fi
done
wait
n=0
for cell in "$@"; do
  n=$((n + 1))
  node="${cell%%:*}"; difficulty="${cell##*:}"
  clone="$ROOT/w$n/Tools/PlayTest"
  row="$(grep -E "^Level_[A-Za-z0-9_]+ +(Easy|Medium|Hard|Psycho)" "$clone/plans_summary.txt" | head -1 || true)"
  level="${row%% *}"
  if [ -z "$level" ]; then echo "w$n: no summary row for cell $cell (see Logs/r62/chain_search_w$n.log)" >&2; exit 1; fi
  rm -f "Tools/PlayTest/Plans/${level}_${difficulty}.txt" "Tools/PlayTest/Plans/${level}_${difficulty}"_*.json
  cp "$clone/Plans/${level}_${difficulty}.txt" "$clone/Plans/${level}_${difficulty}"_*.json Tools/PlayTest/Plans/
  echo "$row" >> Tools/PlayTest/r62_plans_summary.txt
  echo "  w$n cell $cell (node $node): $(echo "$row" | cut -c1-110)"
done

echo "== chain: replay cells=$cells ($(date +%H:%M:%S))"
bash Tools/PlayTest/workers.sh run replay "cells=$cells"
echo "== chain done ($(date +%H:%M:%S))"
