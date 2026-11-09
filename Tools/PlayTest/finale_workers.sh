#!/usr/bin/env bash
# The finale probe on headless workers (round 27): one clone per difficulty plays every continent boss at its arena
# (FinaleProbeTests.Measure_ContinentFinales_AsPlayed, about seven runs per boss and difficulty), and the four reports are
# merged into Tools/PlayTest/finale_probe.txt (the header of the first, every 'cell' line). Then run ContinentFinaleTests
# and `balance_pipeline.sh finale`. Uses the clones and settings of workers.sh; the clones must be idle.
#
#   bash Tools/PlayTest/finale_workers.sh              # difficulties 0..3 on w1..w4
#   bash Tools/PlayTest/finale_workers.sh 1 2          # only these difficulties (one clone each)
set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="${TD_WORKERS_ROOT:-C:\\Users\\remco\\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.5f1\\Editor\\Unity.exe}"
JOB_WORKERS="${TD_JOB_WORKERS:-2}"
SAVE="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
TEST=Game.Tests.FinaleProbeTests.Measure_ContinentFinales_AsPlayed
REPORT=Tools/PlayTest/finale_probe.txt
DIFFS=("$@"); [ "${#DIFFS[@]}" -gt 0 ] || DIFFS=(0 1 2 3)

save_md5=""; [ -f "$SAVE" ] && save_md5=$(md5sum "$SAVE" | cut -d' ' -f1)
started=$(date +%s)
echo "== finale probe: difficulties ${DIFFS[*]} on ${#DIFFS[@]} workers ($(date +%H:%M:%S))"
bash Tools/PlayTest/workers.sh sync "${#DIFFS[@]}" > /dev/null
i=0
for d in "${DIFFS[@]}"; do
  i=$((i + 1))
  dir="$(cygpath -u "$ROOT\\w$i")"
  printf 'difficulties=%s\n' "$d" > "$dir/Tools/PlayTest/probe_arg.txt"
  rm -f "$dir/$REPORT" "$dir/Logs/results.xml"
  echo "  w$i: difficulty $d"
  "$UNITY" -batchmode -job-worker-count "$JOB_WORKERS" -timestamps -projectPath "$ROOT\\w$i" -runTests -testPlatform PlayMode -testFilter "$TEST" \
    -testResults "$ROOT\\w$i\\Logs\\results.xml" -logFile "$ROOT\\w$i\\Logs\\unity.log" > /dev/null 2>&1 &
done
wait
ended=$(date +%s)
echo "== finale workers done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s ($(date +%H:%M:%S))"

merged=""
cells=""
for ((i = 1; i <= ${#DIFFS[@]}; i++)); do
  dir="$(cygpath -u "$ROOT\\w$i")"
  result=$(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo 'result="missing"')
  echo "  w$i: $result, $(grep -c '^cell|' "$dir/$REPORT" 2>/dev/null || echo 0) cells"
  if [ -f "$dir/$REPORT" ]; then
    [ -n "$merged" ] || merged=$(grep -v '^cell|' "$dir/$REPORT")
    cells="$cells$(grep '^cell|' "$dir/$REPORT")"$'\n'
  fi
  rm -f "$dir"/Assets/InitTestScene*.unity*
done
if [ -z "$cells" ]; then echo "no worker wrote a finale report: see $ROOT\\wN\\Logs\\unity.log" >&2; exit 1; fi
{ printf '%s\n' "$merged"; printf '%s' "$cells" | sed '/^$/d' | sort -t'|' -k2,2 -k3,3n; } > "$REPORT"
echo "merged $(grep -c '^cell|' "$REPORT") cells into $REPORT"
now_md5=""; [ -f "$SAVE" ] && now_md5=$(md5sum "$SAVE" | cut -d' ' -f1)
[ "$now_md5" = "$save_md5" ] && echo "  save untouched (MD5 ${save_md5:-none})" || echo "WARNING: the save changed ($save_md5 -> $now_md5)" >&2
