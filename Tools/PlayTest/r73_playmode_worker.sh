#!/usr/bin/env bash
# Round 73: a PlayMode test (class or full name) on a headless, silent worker clone wN. Syncs the clone from the project like
# workers.sh (own product name, audio off), runs Unity -batchmode -runTests PlayMode with the filter and waits; the results
# stay in the clone (Logs/results.xml, Logs/unity.log, Tools/PlayTest/...). Remco's save MD5 is checked before and after.
# Usage: bash Tools/PlayTest/r73_playmode_worker.sh <N> <TestFilter>
set -euo pipefail
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
n="$1"; test="$2"
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
PROJECT="$(pwd -W 2>/dev/null || pwd)"
SAVE="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
dir="$(cygpath -u "$ROOT\w$n")"
before=$(md5sum "$SAVE" 2>/dev/null | cut -d' ' -f1 || echo none)
mkdir -p "$dir/Logs"
for d in Assets Packages ProjectSettings Tools; do
  robocopy "$PROJECT/$d" "$ROOT/w$n/$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
done
if [ ! -d "$dir/Library" ]; then
  robocopy "$PROJECT/Library" "$ROOT/w$n/Library" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
fi
sed -i 's/^  productName: Tower defense$/  productName: Tower defense worker w'"$n"'/' "$dir/ProjectSettings/ProjectSettings.asset"
sed -i 's/^  m_DisableAudio: 0$/  m_DisableAudio: 1/' "$dir/ProjectSettings/AudioManager.asset"
rm -f "$dir"/Assets/InitTestScene*.unity* "$dir/Tools/PlayTest/probe_arg.txt" "$dir/Logs/results.xml"
started=$(date +%s)
echo "== w$n: PlayMode $test ($(date +%H:%M:%S))"
"$UNITY" -batchmode -job-worker-count 2 -timestamps -projectPath "$ROOT\w$n" -runTests -testPlatform PlayMode -testFilter "$test" \
  -testResults "$ROOT\w$n\Logs\results.xml" -logFile "$ROOT\w$n\Logs\unity.log" > /dev/null 2>&1 || true
ended=$(date +%s)
echo "== w$n done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s: $(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo 'no results.xml')"
grep -o 'name="[^"]*" fullname="[^"]*" methodname[^>]*result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | sed 's/ methodname.*result=/ result=/' | head -20 || true
after=$(md5sum "$SAVE" 2>/dev/null | cut -d' ' -f1 || echo none)
echo "save md5 before $before after $after"
