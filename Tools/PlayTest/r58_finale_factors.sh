#!/usr/bin/env bash
# Round 58 (copy of r55b_finale.sh): the Shattered finale probe on Easy only, one clone per trial Easy health factor (w1..w4;
# the clone's ShatteredBoss.asset Factor line is rewritten after the sync), the reports merged into the file named by $1.
# Usage: bash Tools/PlayTest/r58_finale_factors.sh <merged report path> <factor w1> <factor w2> <factor w3> <factor w4>
set -euo pipefail
cd "$(dirname "$0")/../.."
out="$1"; shift
factors=("$@")
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
TEST=Game.Tests.FinaleProbeTests.Measure_ContinentFinales_AsPlayed
REPORT=Tools/PlayTest/finale_probe.txt
BOSS=Assets/_Project/Data/Enemies/Bosses/ShatteredBoss.asset
started=$(date +%s)
bash Tools/PlayTest/workers.sh sync 4 > /dev/null
for i in 1 2 3 4; do
  dir="$(cygpath -u "$ROOT\w$i")"
  sed -i "s/^    Factor: .*/    Factor: ${factors[$((i - 1))]}/" "$dir/$BOSS"
  printf 'bosses=4\ndifficulties=0\n' > "$dir/Tools/PlayTest/probe_arg.txt"
  rm -f "$dir/$REPORT" "$dir/Logs/results.xml"
  "$UNITY" -batchmode -job-worker-count 2 -timestamps -projectPath "$ROOT\w$i" -runTests -testPlatform PlayMode -testFilter "$TEST" \
    -testResults "$ROOT\w$i\Logs\results.xml" -logFile "$ROOT\w$i\Logs\unity.log" > /dev/null 2>&1 &
done
wait
: > "$out"
for i in 1 2 3 4; do
  dir="$(cygpath -u "$ROOT\w$i")"
  echo "w$i Easy factor ${factors[$((i - 1))]}: $(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo missing)" >> "$out"
  grep -E '^  Shattered|^cell\|Shattered' "$dir/$REPORT" >> "$out" 2>/dev/null || echo "  no report on w$i" >> "$out"
  rm -f "$dir"/Assets/InitTestScene*.unity*
done
echo "done in $(( ($(date +%s) - started) / 60 )) min" >> "$out"
cat "$out"
