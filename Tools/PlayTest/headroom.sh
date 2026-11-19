#!/usr/bin/env bash
# Headroom (Docs/BALANCE_HEADROOM.md; stage 1 of Docs/BALANCE_METHOD_REVIEW.md): per level, difficulty, reference player and
# seed the largest uniform multiple of the creatures' health that player still wins at, bisected in the real game by
# HeadroomProbeTests.Measure_Headroom on the headless worker clones of workers.sh (silent, their own save folder). The open
# editor is never touched and no data is written; the records go to Tools/PlayTest/headroom_records.txt (appended, so a
# rerun of a unit sits beside the first and the report shows whether it repeated) and the table to Tools/PlayTest/headroom.txt.
#
#   bash Tools/PlayTest/headroom.sh "0,2" "0,1,2,3"       # campaign nodes, difficulties: the ladder and the mono rows
#   bash Tools/PlayTest/headroom.sh report                 # only rebuild the table from the records
#
# Env (all optional):
#   HR_POLICIES  careless,sensible,skilled   the ladder rungs played on every seed of HR_SEEDS (also: fresh)
#   HR_SEEDS     1,2,3
#   HR_EXTRA     mono,fresh                  policies played on HR_EXTRA_SEEDS only (mono = one row per owned tower type)
#   HR_EXTRA_SEEDS 1
#   HR_UNITS     node:difficulty:policy:seed,...   exactly these units instead of the product above
#   HR_PRECISION 0.03                        the bisection stops when first loss / last win is within it
#   HR_SCAN      (empty)                     multiples of the measured edge played after the bisection (monotonicity check),
#                                            e.g. 0.6,0.75,0.87,0.94,1.06,1.15,1.33,1.6
#   HR_BASE      (empty)                     node:difficulty:factor,... the cell's health times factor before the multiple
#   HR_RECORDS   Tools/PlayTest/headroom_records.txt
#   HR_WORKERS   the N of workers.sh count;  HR_FIRST 1 (the first clone to use: clones wFIRST..wFIRST+N-1)
set -euo pipefail
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
ROOT="${TD_WORKERS_ROOT:-C:\\Users\\remco\\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.5f1\\Editor\\Unity.exe}"
JOB_WORKERS="${TD_JOB_WORKERS:-2}"
SAVE_DIR="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves"
SAVE="$SAVE_DIR/progress.json"
TEST=Game.Tests.HeadroomProbeTests.Measure_Headroom
RECORDS="${HR_RECORDS:-Tools/PlayTest/headroom_records.txt}"
REPORT="${RECORDS%_records.txt}.txt"

report() { python Tools/PlayTest/headroom_report.py "$RECORDS" "$REPORT"; }
if [ "${1:-}" = "report" ]; then report; exit 0; fi

LEVELS="${1:-0,2}"; DIFFS="${2:-0,1,2,3}"
POLICIES="${HR_POLICIES-careless,sensible,skilled}"; SEEDS="${HR_SEEDS:-1,2,3}"
EXTRA="${HR_EXTRA-mono,fresh}"; EXTRA_SEEDS="${HR_EXTRA_SEEDS:-1}"
UNITS=()
if [ -n "${HR_UNITS:-}" ]; then
  IFS=, read -r -a UNITS <<< "$HR_UNITS"
else
  # Seeds outermost, so the long units of one cell are dealt over the workers and seed 1 of everything finishes first.
  for s in ${SEEDS//,/ }; do for p in ${POLICIES//,/ }; do for n in ${LEVELS//,/ }; do for d in ${DIFFS//,/ }; do UNITS+=("$n:$d:$p:$s"); done; done; done; done
  for s in ${EXTRA_SEEDS//,/ }; do for p in ${EXTRA//,/ }; do for n in ${LEVELS//,/ }; do for d in ${DIFFS//,/ }; do UNITS+=("$n:$d:$p:$s"); done; done; done; done
fi
[ "${#UNITS[@]}" -gt 0 ] || { echo "no units" >&2; exit 1; }
N="${HR_WORKERS:-$(bash Tools/PlayTest/workers.sh count)}"; FIRST="${HR_FIRST:-1}"
[ "$N" -gt "${#UNITS[@]}" ] && N=${#UNITS[@]}

save_md5=""; [ -f "$SAVE" ] && save_md5=$(md5sum "$SAVE" | cut -d' ' -f1)
had_settings=0; [ -f "$SAVE_DIR/settings.json" ] && had_settings=1
started=$(date +%s)
echo "== headroom: ${#UNITS[@]} units over $N workers (w$FIRST..w$((FIRST + N - 1))), precision ${HR_PRECISION:-0.03} ($(date +%H:%M:%S))"
# The clones are refreshed as workers.sh does it (a product name of their own, so their own save folder; audio off), side by side.
PROJECT="$(pwd -W 2>/dev/null || pwd)"
sync_worker() {
  local i=$1 dir="$ROOT\\w$1" d
  mkdir -p "$(cygpath -u "$dir")/Logs"
  for d in Assets Packages ProjectSettings Tools; do
    robocopy "$PROJECT\\$d" "$dir\\$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
  done
  if [ ! -d "$(cygpath -u "$dir")/Library" ]; then
    robocopy "$PROJECT\\Library" "$dir\\Library" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
  fi
  sed -i "s/^  productName: Tower defense.*\$/  productName: Tower defense worker w$i/" "$(cygpath -u "$dir")/ProjectSettings/ProjectSettings.asset"
  sed -i 's/^  m_DisableAudio: 0$/  m_DisableAudio: 1/' "$(cygpath -u "$dir")/ProjectSettings/AudioManager.asset"
  rm -f "$(cygpath -u "$dir")"/Assets/InitTestScene*.unity*
}
for ((i = FIRST; i < FIRST + N; i++)); do sync_worker "$i" & done
wait
echo "  synced in $(( $(date +%s) - started )) s"
pids=()
for ((k = 0; k < N; k++)); do
  i=$((FIRST + k))
  dir="$(cygpath -u "$ROOT\\w$i")"
  mine=()
  for ((u = k; u < ${#UNITS[@]}; u += N)); do mine+=("${UNITS[u]}"); done
  {
    echo "units=$(IFS=,; echo "${mine[*]}")"
    echo "precision=${HR_PRECISION:-0.03}"
    [ -z "${HR_SCAN:-}" ] || echo "scan=$HR_SCAN"
    [ -z "${HR_BASE:-}" ] || echo "base=$HR_BASE"
  } > "$dir/Tools/PlayTest/headroom_arg.txt"
  # A stale probe filter in the clone would set the probe's step; the headroom runs use the probe's defaults.
  rm -f "$dir/Tools/PlayTest/probe_arg.txt" "$dir/Tools/PlayTest/headroom_records.txt" "$dir/Logs/results.xml" "$dir/Logs/probe_trace.txt"
  echo "  w$i: ${#mine[@]} units: ${mine[*]}"
  "$UNITY" -batchmode -job-worker-count "$JOB_WORKERS" -timestamps -projectPath "$ROOT\\w$i" -runTests -testPlatform PlayMode -testFilter "$TEST" \
    -testResults "$ROOT\\w$i\\Logs\\results.xml" -logFile "$ROOT\\w$i\\Logs\\unity.log" > /dev/null 2>&1 &
  pids+=($!)
done
wait "${pids[@]}" || true
ended=$(date +%s)
echo "== headroom workers done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s ($(date +%H:%M:%S))"
failed=0
for ((k = 0; k < N; k++)); do
  i=$((FIRST + k))
  dir="$(cygpath -u "$ROOT\\w$i")"
  result=$(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo 'result="missing"')
  errors=$(grep -c "\[Probe\] error" "$dir/Logs/unity.log" 2>/dev/null || true)
  mine="$dir/Tools/PlayTest/headroom_records.txt"
  echo "  w$i: $result, $errors probe errors, $(grep -c '^h|' "$mine" 2>/dev/null || echo 0) edges, $(grep -c '^run|' "$mine" 2>/dev/null || echo 0) runs"
  if [ -f "$mine" ]; then
    # Each record carries the worker and the batch it came from, for the cost and the rerun comparison.
    sed "s/\$/|w$i|$started/" "$mine" >> "$RECORDS"
  else
    failed=1
  fi
  rm -f "$dir"/Assets/InitTestScene*.unity*
done
now_md5=""; [ -f "$SAVE" ] && now_md5=$(md5sum "$SAVE" | cut -d' ' -f1)
[ "$now_md5" = "$save_md5" ] && echo "  save untouched (MD5 ${save_md5:-none})" || echo "WARNING: the save changed ($save_md5 -> $now_md5)" >&2
if [ "$had_settings" = 0 ] && [ -f "$SAVE_DIR/settings.json" ]; then echo "WARNING: a settings.json appeared beside the save" >&2; fi
echo "batch|$started|$(( ended - started ))|$N|${#UNITS[@]}" >> "$RECORDS"
[ "$failed" = 0 ] || echo "a worker wrote no records: see $ROOT\\wN\\Logs\\unity.log" >&2
report
