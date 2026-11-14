#!/usr/bin/env bash
# Round 62: a what-if on a headless worker clone, nothing written in the project. Syncs clone wN from the project, applies
# source edits inside the clone only (a rule or a spec number), solves the waves there (BalanceStages.SolveWaves) and plays the
# air-defence report (AirDefenceProbeTests: no sky gun / no sky gun but guns by the besieged base / two sky guns) on the clone's
# waves. The report is copied to Tools/PlayTest/r62_air_<label>.txt. The clone keeps its own product name, so Remco's save is
# never read or written; the next workers.sh run syncs the project over the clone again (the edits are gone).
#
#   bash Tools/PlayTest/r62_variant_worker.sh <N> <label> "<file under the project>::<sed expression>" [more edits...] -- [filter lines]
#   bash Tools/PlayTest/r62_variant_worker.sh 4 floor045 \
#     "Assets/_Project/Scripts/Balance/BalanceRules.cs::s/RaidHealthFloor = { 0.6f,/RaidHealthFloor = { 0.45f,/" -- levels=2 difficulties=0
#
# An edit that changes nothing stops the run (a what-if that silently plays the stored waves is worse than none).
set -euo pipefail
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
n="$1"; label="$2"; shift 2
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
PROJECT="$(pwd -W 2>/dev/null || pwd)"
dir="$(cygpath -u "$ROOT\w$n")"
for d in Assets Packages ProjectSettings Tools; do
  robocopy "$PROJECT\\$d" "$ROOT\\w$n\\$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
done
sed -i 's/^  productName: Tower defense$/  productName: Tower defense worker w'"$n"'/' "$dir/ProjectSettings/ProjectSettings.asset"
rm -f "$dir"/Assets/InitTestScene*.unity*
while [ "$#" -gt 0 ] && [ "$1" != "--" ]; do
  file="${1%%::*}"; expr="${1#*::}"
  before="$(md5sum < "$dir/$file")"
  sed -i "$expr" "$dir/$file"
  if [ "$(md5sum < "$dir/$file")" = "$before" ]; then echo "w$n $label: the edit changed nothing in $file: $expr" >&2; exit 1; fi
  shift
done
[ "${1:-}" = "--" ] && shift
if [ "$#" -gt 0 ]; then printf '%s\n' "$@" > "$dir/Tools/PlayTest/probe_arg.txt"; else printf 'levels=2,3\ndifficulties=0\nseeds=1,2,3\n' > "$dir/Tools/PlayTest/probe_arg.txt"; fi
rm -f "$dir/Logs/results.xml" "$dir/Tools/PlayTest/air_defence.txt"
started=$(date +%s)
echo "== w$n $label: solve ($(date +%H:%M:%S))"
"$UNITY" -batchmode -nographics -quit -job-worker-count 2 -projectPath "$ROOT\w$n" -executeMethod Game.Editor.BalanceStages.SolveWaves -logFile "$ROOT\w$n\Logs\unity_solve.log" > /dev/null 2>&1 || true
# The solve returns its line to the caller, not to the log: judge it by a clean batch exit and by the wave assets it rewrote.
if grep -q "error CS" "$dir/Logs/unity_solve.log" || ! grep -q "Exiting batchmode successfully" "$dir/Logs/unity_solve.log"; then echo "w$n $label: the solve failed (see $dir/Logs/unity_solve.log)" >&2; exit 1; fi
echo "   wave assets changed against the project: $(diff -rq "Assets/_Project/Data/Waves" "$dir/Assets/_Project/Data/Waves" | grep -o 'Waves_[A-Za-z0-9_]*' | sort -u | tr '\n' ' ')"
echo "== w$n $label: air probe ($(date +%H:%M:%S))"
"$UNITY" -batchmode -job-worker-count 2 -timestamps -projectPath "$ROOT\w$n" -runTests -testPlatform PlayMode \
  -testFilter Game.Tests.AirDefenceProbeTests.Report_SkyGunsAgainstNone_OnTheFirstAirLevels \
  -testResults "$ROOT\w$n\Logs\results.xml" -logFile "$ROOT\w$n\Logs\unity.log" > /dev/null 2>&1 || true
ended=$(date +%s)
rm -f "$dir"/Assets/InitTestScene*.unity*
echo "== w$n $label done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s: $(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo 'no results.xml'), $(grep -c "\[Probe\] error" "$dir/Logs/unity.log" 2>/dev/null || true) probe errors"
if [ -f "$dir/Tools/PlayTest/air_defence.txt" ]; then
  cp "$dir/Tools/PlayTest/air_defence.txt" "Tools/PlayTest/r62_air_$label.txt"
  echo "report: Tools/PlayTest/r62_air_$label.txt"
else
  echo "w$n $label: no report written" >&2; exit 1
fi
