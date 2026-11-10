#!/usr/bin/env bash
# Round 34b: the solve stage headless on clone wN (BalanceStages.SolveWaves by -executeMethod), when the open editor is busy or hung.
# Syncs the clone from the project first; copy the changed Assets/_Project/Data/Waves/*.asset back afterwards (the solve is deterministic).
# Usage: bash Tools/PlayTest/r34b_solve_worker.sh <N>
set -euo pipefail
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
n="$1"
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
PROJECT="$(pwd -W 2>/dev/null || pwd)"
dir="$(cygpath -u "$ROOT\w$n")"
for d in Assets Packages ProjectSettings Tools; do
  robocopy "$PROJECT/$d" "$ROOT/w$n/$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
done
sed -i 's/^  productName: Tower defense$/  productName: Tower defense worker w'"$n"'/' "$dir/ProjectSettings/ProjectSettings.asset"
started=$(date +%s)
echo "== w$n: SolveWaves ($(date +%H:%M:%S))"
"$UNITY" -batchmode -nographics -quit -job-worker-count 2 -projectPath "$ROOT\w$n" -executeMethod Game.Editor.BalanceStages.SolveWaves -logFile "$ROOT\w$n\Logs\unity_solve.log" > /dev/null 2>&1 || true
ended=$(date +%s)
echo "== w$n done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s"
grep -o "Waves solved[^\"]\{0,120\}\|\[Balance\][^\"]\{0,160\}" "$dir/Logs/unity_solve.log" | head -3 || true
