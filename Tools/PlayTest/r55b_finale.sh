#!/usr/bin/env bash
# Round 55b: the finale probe for the Shattered alone (FinalePlan index 4), one clone per difficulty (w1..w4), the four
# reports merged into the file named by $1 (Tools/PlayTest/finale_probe.txt is not touched). Clones must be idle.
# Usage: bash Tools/PlayTest/r55b_finale.sh <merged report path>
set -euo pipefail
cd "$(dirname "$0")/../.."
out="$1"
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
TEST=Game.Tests.FinaleProbeTests.Measure_ContinentFinales_AsPlayed
REPORT=Tools/PlayTest/finale_probe.txt
started=$(date +%s)
bash Tools/PlayTest/workers.sh sync 4 > /dev/null
for d in 0 1 2 3; do
  i=$((d + 1))
  dir="$(cygpath -u "$ROOT\w$i")"
  printf 'bosses=4\ndifficulties=%s\n' "$d" > "$dir/Tools/PlayTest/probe_arg.txt"
  rm -f "$dir/$REPORT" "$dir/Logs/results.xml"
  "$UNITY" -batchmode -job-worker-count 2 -timestamps -projectPath "$ROOT\w$i" -runTests -testPlatform PlayMode -testFilter "$TEST" \
    -testResults "$ROOT\w$i\Logs\results.xml" -logFile "$ROOT\w$i\Logs\unity.log" > /dev/null 2>&1 &
done
wait
: > "$out"
for i in 1 2 3 4; do
  dir="$(cygpath -u "$ROOT\w$i")"
  echo "w$i: $(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo missing)" >> "$out"
  grep -E '^  Shattered|^cell\|Shattered' "$dir/$REPORT" >> "$out" 2>/dev/null || echo "  no report on w$i" >> "$out"
  rm -f "$dir"/Assets/InitTestScene*.unity*
done
echo "done in $(( ($(date +%s) - started) / 60 )) min" >> "$out"
cat "$out"
