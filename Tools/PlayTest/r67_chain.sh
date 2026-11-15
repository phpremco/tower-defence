#!/usr/bin/env bash
# Round 67: after an apply and a solve, the yardstick on the new waves (w1..w5) and then the rest of the pipeline for all six levels on the clones: the plan search per
# level on w1..w6 side by side (plan files copied back), the replay on w1..w5 (merged into plan_records.txt), the acceptance
# report on w6 (copied back). The open editor is not used. Usage: bash Tools/PlayTest/r67_chain.sh > Logs/r67/chain.log 2>&1
set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="$(cygpath -u "${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}")"
keys=(C1_01 C1_02 C1_03 C1_04 C1_05 C1_06)
mkdir -p Logs/r67
echo "== r67 chain: measure ($(date +%H:%M:%S))"
bash Tools/PlayTest/workers.sh run measure 5 | tail -8
echo "== r67 chain: search ($(date +%H:%M:%S))"
for n in 0 1 2 3 4 5; do
  bash Tools/PlayTest/r34b_editmode_worker.sh $((n + 1)) PlanSearchTests "levels=$n" > "Logs/r67/search_level_$n.log" 2>&1 &
done
wait
for n in 0 1 2 3 4 5; do
  level="Level_${keys[$n]}"; clone="$ROOT/w$((n + 1))/Tools/PlayTest/Plans"
  tail -2 "Logs/r67/search_level_$n.log" | cut -c1-200
  ls "$clone/${level}_0.txt" > /dev/null
  rm -f Tools/PlayTest/Plans/${level}_*.txt Tools/PlayTest/Plans/${level}_*.json
  cp "$clone"/${level}_*.txt "$clone"/${level}_*.json Tools/PlayTest/Plans/
  echo "  $level from w$((n + 1)): $(ls Tools/PlayTest/Plans/${level}_*.json | wc -l) plans"
done
echo "== r67 chain: replay ($(date +%H:%M:%S))"
bash Tools/PlayTest/workers.sh run replay 5 | tail -8
echo "== r67 chain: acceptance ($(date +%H:%M:%S))"
bash Tools/PlayTest/r34b_editmode_worker.sh 6 BalanceAcceptanceTests | tail -4
cp "$ROOT/w6/Tools/PlayTest/acceptance.txt" Tools/PlayTest/acceptance.txt
echo "== r67 chain done ($(date +%H:%M:%S))"
