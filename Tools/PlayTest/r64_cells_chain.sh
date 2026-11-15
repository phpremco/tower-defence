#!/usr/bin/env bash
# Round 64: search and replay a list of cells whose waves moved, never on more than the six existing clones (round 62's chain
# numbered a clone per cell, so a twelfth cell asked for a twelfth clone, which Unity then imported from scratch).
#   copy <cell:clone> ...    copies the plan files of cells already searched on the named clones (cell as node:difficulty)
#   search <cell> ...        searches the cells five at a time on w1..w5 and copies their plan files back
#   replay <cell> ...        replays the cells on the five workers (merged into plan_records.txt)
# Usage: bash Tools/PlayTest/r64_cells_chain.sh search 0:3 1:3 2:3 > Logs/r64/chain3.log 2>&1
set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="$(cygpath -u "${TD_WORKERS_ROOT:-C:\\Users\\remco\\TowerDefenseWorkers}")"
keys=(C1_01 C1_02 C1_03 C1_04 C1_05 C1_06)
mkdir -p Logs/r64
take() {
  # $1 cell, $2 clone number: the cell's plan files from the clone, over the project's.
  local node="${1%%:*}" difficulty="${1##*:}" level="Level_${keys[${1%%:*}]}" clone="$ROOT/w$2/Tools/PlayTest"
  ls "$clone/Plans/${level}_${difficulty}".txt > /dev/null
  rm -f "Tools/PlayTest/Plans/${level}_${difficulty}.txt" "Tools/PlayTest/Plans/${level}_${difficulty}"_*.json
  cp "$clone/Plans/${level}_${difficulty}.txt" "$clone/Plans/${level}_${difficulty}"_*.json Tools/PlayTest/Plans/
  echo "  cell $1 from w$2: $(ls Tools/PlayTest/Plans/${level}_${difficulty}_*.json | wc -l) plans"
}
stage="$1"; shift
echo "== r64 cells: $stage $* ($(date +%H:%M:%S))"
case "$stage" in
  copy)
    for pair in "$@"; do take "${pair%:*}" "${pair##*:}"; done
    ;;
  search)
    cells=("$@")
    for ((start = 0; start < ${#cells[@]}; start += 5)); do
      for ((i = start; i < start + 5 && i < ${#cells[@]}; i++)); do
        n=$((i - start + 1))
        bash Tools/PlayTest/r34b_editmode_worker.sh "$n" PlanSearchTests "cells=${cells[$i]}" > "Logs/r64/search_cell_${cells[$i]/:/_}.log" 2>&1 &
      done
      wait
      for ((i = start; i < start + 5 && i < ${#cells[@]}; i++)); do take "${cells[$i]}" $((i - start + 1)); done
    done
    ;;
  replay)
    bash Tools/PlayTest/workers.sh run replay 5 "cells=$(IFS=,; echo "$*")" | tail -8
    ;;
  *) echo "unknown stage $stage" >&2; exit 1 ;;
esac
echo "== r64 cells done ($(date +%H:%M:%S))"
