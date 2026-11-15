#!/usr/bin/env bash
# Round 64: the pipeline after the yardstick of all 24 cells was measured on the round's mechanics (armour rule, boss scales).
#   fit      the calibration from the records, on clone w6 (the replayed plans of the old waves are set aside first);
#   apply    the new fit onto the calibration asset (apply-force: the bar is the curator's standing exception), then solve;
#   search   the strategy search, one level per clone side by side (w1..w5, then the sixth level), plans copied back;
#   replay   the searched plans on the five workers;
#   accept   acceptance on w6, the report copied back, then the plans' README in the editor.
# Usage: bash Tools/PlayTest/r64_chain.sh fit apply search replay accept > Logs/r64/chain.log 2>&1 &
set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="$(cygpath -u "${TD_WORKERS_ROOT:-C:\\Users\\remco\\TowerDefenseWorkers}")"
mkdir -p Logs/r64
keys=(C1_01 C1_02 C1_03 C1_04 C1_05 C1_06)
for stage in "$@"; do
  echo "== r64 chain: $stage ($(date +%H:%M:%S))"
  case "$stage" in
    fit)
      if [ ! -f Logs/r64/plan_records_old_waves.txt ]; then cp Tools/PlayTest/plan_records.txt Logs/r64/plan_records_old_waves.txt; : > Tools/PlayTest/plan_records.txt; fi
      bash Tools/PlayTest/r34b_editmode_worker.sh 6 CalibrationFitTests | tail -3
      cp "$ROOT/w6/Tools/PlayTest/calibration.txt" "$ROOT/w6/Tools/PlayTest/calibration_waves.txt" Tools/PlayTest/
      sed -n '1,30p' Tools/PlayTest/calibration.txt
      ;;
    apply)
      bash Tools/PlayTest/balance_pipeline.sh apply-force | tail -1 | cut -c1-1500
      bash Tools/PlayTest/balance_pipeline.sh solve | tail -1 | cut -c1-400
      ;;
    search)
      rm -f Tools/PlayTest/Plans/Level_C1_0*; : > Tools/PlayTest/r64_plans_summary.txt
      for round in "0 1 2 3 4" "5"; do
        n=0
        for level in $round; do
          n=$((n + 1))
          bash Tools/PlayTest/r34b_editmode_worker.sh "$n" PlanSearchTests "levels=$level" > "Logs/r64/search_level$level.log" 2>&1 &
        done
        wait
        n=0
        for level in $round; do
          n=$((n + 1))
          clone="$ROOT/w$n/Tools/PlayTest"
          cp "$clone/Plans/Level_${keys[$level]}"_* Tools/PlayTest/Plans/
          grep -E "^Level_${keys[$level]} " "$clone/plans_summary.txt" >> Tools/PlayTest/r64_plans_summary.txt || true
          echo "  level $level (${keys[$level]}): $(ls Tools/PlayTest/Plans/Level_${keys[$level]}_* | wc -l) plan files, $(tail -1 "Logs/r64/search_level$level.log" | cut -c1-120)"
        done
      done
      ;;
    replay)
      bash Tools/PlayTest/workers.sh run replay 5 | tail -3
      ;;
    accept)
      bash Tools/PlayTest/r34b_editmode_worker.sh 6 BalanceAcceptanceTests | tail -2
      cp "$ROOT/w6/Tools/PlayTest/acceptance.txt" Tools/PlayTest/
      bash Tools/PlayTest/balance_pipeline.sh readme | tail -1 | cut -c1-300
      ;;
    *) echo "unknown stage $stage" >&2; exit 1 ;;
  esac
done
echo "== r64 chain done ($(date +%H:%M:%S))"
