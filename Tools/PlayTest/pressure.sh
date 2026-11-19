#!/usr/bin/env bash
# Pressure (Docs/BALANCE_HEADROOM.md, "Stage 1b"): the continuous balance ruler. Per level, difficulty, reference player and
# seed the level is played at a few fixed multiples of the creatures' health by PressureProbeTests.Measure_Pressure on the
# headless worker clones of workers.sh (silent, their own save folder); nothing is bisected. The open editor is never touched
# and no data is written; the records go to Tools/PlayTest/pressure_records.txt (appended) and the report, with the stop
# rules, to Tools/PlayTest/pressure.txt. The headroom pipeline (headroom.sh) is untouched and still works beside it.
# Every record carries the stamp of the bot, data and game code it was measured with (round 76, pressure_stamp.py): keep one
# records file (PR_RECORDS) per bot and data state; the report refuses a file that mixes bots. Never change the bot and the
# data in one batch.
#
#   bash Tools/PlayTest/pressure.sh "0,2" "0,1,2,3"       # campaign nodes, difficulties
#   bash Tools/PlayTest/pressure.sh report                 # only rebuild the report from the records
#
# Env (all optional):
#   PR_POLICIES  skilled,sensible            the rungs played on every seed (also: script, typical, careless, fresh, mono-TowerId)
#   PR_SEEDS     1,2,3
#   PR_MULTIPLES 0.8,0.9,1,1.12,1.25         the health multiples every unit is played at (round 76: health jitter instead of more seeds)
#   PR_EXTRA     (none)                      node:policy pairs played as well, on every difficulty and seed, e.g. 0:mono-Cannon,0:careless
#   PR_UNITS     node:difficulty:policy:seed,...   exactly these units instead of the product above
#   PR_RECORDS   Tools/PlayTest/pressure_records.txt
#   PR_WORKERS   the N of workers.sh count;  PR_FIRST 1 (the first clone to use: clones wFIRST..wFIRST+N-1)
#   PR_KEEPTRACE 1 keeps the probe's per-wave trace of every run in the clone (Logs/probe_trace.txt) for a diagnosis
#   PR_OVERLAY   a folder whose files are copied over each clone after the sync (same relative paths), to measure another
#                data state with the same bot without touching the project; the records carry that state's data stamp
set -euo pipefail
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
ROOT="${TD_WORKERS_ROOT:-C:\\Users\\remco\\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.5f1\\Editor\\Unity.exe}"
JOB_WORKERS="${TD_JOB_WORKERS:-2}"
SAVE_DIR="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves"
SAVE="$SAVE_DIR/progress.json"
TEST=Game.Tests.PressureProbeTests.Measure_Pressure
RECORDS="${PR_RECORDS:-Tools/PlayTest/pressure_records.txt}"
REPORT="${RECORDS%_records.txt}.txt"

report() { python Tools/PlayTest/pressure_report.py "$RECORDS" "$REPORT" > /dev/null && echo "report: $REPORT" && sed -n '1,40p' "$REPORT"; }
if [ "${1:-}" = "report" ]; then report; exit 0; fi

LEVELS="${1:-0,2}"; DIFFS="${2:-0,1,2,3}"
POLICIES="${PR_POLICIES-skilled,sensible}"; SEEDS="${PR_SEEDS:-1,2,3}"
MULTIPLES="${PR_MULTIPLES:-0.8,0.9,1,1.12,1.25}"
EXTRA="${PR_EXTRA-}"
UNITS=()
if [ -n "${PR_UNITS:-}" ]; then
  IFS=, read -r -a UNITS <<< "$PR_UNITS"
else
  # Seeds outermost, so the long units of one cell are dealt over the workers and seed 1 of everything finishes first.
  for s in ${SEEDS//,/ }; do
    for p in ${POLICIES//,/ }; do for n in ${LEVELS//,/ }; do for d in ${DIFFS//,/ }; do UNITS+=("$n:$d:$p:$s"); done; done; done
    for e in ${EXTRA//,/ }; do for d in ${DIFFS//,/ }; do UNITS+=("${e%%:*}:$d:${e#*:}:$s"); done; done
  done
fi
[ "${#UNITS[@]}" -gt 0 ] || { echo "no units" >&2; exit 1; }
N="${PR_WORKERS:-$(bash Tools/PlayTest/workers.sh count)}"; FIRST="${PR_FIRST:-1}"
[ "$N" -gt "${#UNITS[@]}" ] && N=${#UNITS[@]}

save_md5=""; [ -f "$SAVE" ] && save_md5=$(md5sum "$SAVE" | cut -d' ' -f1)
had_settings=0; [ -f "$SAVE_DIR/settings.json" ] && had_settings=1
started=$(date +%s)
echo "== pressure: ${#UNITS[@]} units at multiples $MULTIPLES over $N workers (w$FIRST..w$((FIRST + N - 1))) ($(date +%H:%M:%S))"
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
  # A clone never makes a sound: audio is off in the clone's own settings (the probe also mutes the listener in batch mode).
  sed -i 's/^  m_DisableAudio: 0$/  m_DisableAudio: 1/' "$(cygpath -u "$dir")/ProjectSettings/AudioManager.asset"
  grep -q '^  m_DisableAudio: 1$' "$(cygpath -u "$dir")/ProjectSettings/AudioManager.asset" || { echo "w$i: audio is not off in the clone" >&2; exit 1; }
  rm -f "$(cygpath -u "$dir")"/Assets/InitTestScene*.unity*
  if [ -n "${PR_OVERLAY:-}" ]; then cp -r "$PR_OVERLAY"/. "$(cygpath -u "$dir")"/; fi
}
sync_pids=()
for ((i = FIRST; i < FIRST + N; i++)); do sync_worker "$i" & sync_pids+=($!); done
for pid in "${sync_pids[@]}"; do wait "$pid"; done
echo "  synced in $(( $(date +%s) - started )) s"
pids=()
for ((k = 0; k < N; k++)); do
  i=$((FIRST + k))
  dir="$(cygpath -u "$ROOT\\w$i")"
  mine=()
  for ((u = k; u < ${#UNITS[@]}; u += N)); do mine+=("${UNITS[u]}"); done
  {
    echo "units=$(IFS=,; echo "${mine[*]}")"
    echo "multiples=$MULTIPLES"
    if [ -n "${PR_KEEPTRACE:-}" ]; then echo "keeptrace=1"; fi
  } > "$dir/Tools/PlayTest/pressure_arg.txt"
  # A stale probe filter in the clone would set the probe's step; the pressure runs use the probe's defaults.
  rm -f "$dir/Tools/PlayTest/probe_arg.txt" "$dir/Tools/PlayTest/pressure_records.txt" "$dir/Logs/results.xml" "$dir/Logs/probe_trace.txt"
  echo "  w$i: ${#mine[@]} units"
  "$UNITY" -batchmode -job-worker-count "$JOB_WORKERS" -timestamps -projectPath "$ROOT\\w$i" -runTests -testPlatform PlayMode -testFilter "$TEST" \
    -testResults "$ROOT\\w$i\\Logs\\results.xml" -logFile "$ROOT\\w$i\\Logs\\unity.log" > /dev/null 2>&1 &
  pids+=($!)
done
# Round 85: the workers run at BelowNormal priority (Remco uses the PC meanwhile).
bash Tools/PlayTest/lower_priority.sh 120 &
wait "${pids[@]}" || true
ended=$(date +%s)
echo "== pressure workers done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s ($(date +%H:%M:%S))"
failed=0
for ((k = 0; k < N; k++)); do
  i=$((FIRST + k))
  dir="$(cygpath -u "$ROOT\\w$i")"
  result=$(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo 'result="missing"')
  errors=$(grep -c "\[Probe\] error" "$dir/Logs/unity.log" 2>/dev/null || true)
  mine="$dir/Tools/PlayTest/pressure_records.txt"
  echo "  w$i: $result, $errors probe errors, $(grep -c '^prun|' "$mine" 2>/dev/null || echo 0) runs"
  if [ -f "$mine" ]; then
    # Each record carries the worker and the batch it came from, for the cost.
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
