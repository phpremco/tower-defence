#!/usr/bin/env bash
# Round 34b: an EditMode test class on a headless worker clone (the open editor's Pipeline stopped answering after every EditMode
# run this round). Syncs clone wN from the project (workers.sh sync_worker), writes its probe_arg.txt from the filter lines,
# runs Unity -batchmode -runTests EditMode with the class filter and waits. Outputs stay in the clone (Tools/PlayTest/...);
# copy back what the stage wrote. Usage: bash Tools/PlayTest/r34b_editmode_worker.sh <N> <ClassName> [filter lines...]
set -euo pipefail
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
n="$1"; test="$2"; shift 2
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
PROJECT="$(pwd -W 2>/dev/null || pwd)"
dir="$(cygpath -u "$ROOT\w$n")"
for d in Assets Packages ProjectSettings Tools; do
  robocopy "$PROJECT\\$d" "$ROOT\\w$n\\$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
done
# The clone keeps its own product name (persistentDataPath), as workers.sh does.
sed -i 's/^  productName: Tower defense$/  productName: Tower defense worker w'"$n"'/' "$dir/ProjectSettings/ProjectSettings.asset"
if [ "$#" -gt 0 ]; then printf '%s\n' "$@" > "$dir/Tools/PlayTest/probe_arg.txt"; else rm -f "$dir/Tools/PlayTest/probe_arg.txt"; fi
rm -f "$dir/Logs/results.xml"
started=$(date +%s)
echo "== w$n: EditMode $test, filter: $* ($(date +%H:%M:%S))"
"$UNITY" -batchmode -nographics -job-worker-count 2 -timestamps -projectPath "$ROOT\w$n" -runTests -testPlatform EditMode -testFilter "$test" \
  -testResults "$ROOT\w$n\Logs\results.xml" -logFile "$ROOT\w$n\Logs\unity_editmode.log" > /dev/null 2>&1 || true
ended=$(date +%s)
echo "== w$n done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s: $(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo 'no results.xml')"
grep -o 'name="[^"]*" fullname="[^"]*" methodname[^>]*result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | sed 's/ methodname.*result=/ result=/' | head -20 || true
