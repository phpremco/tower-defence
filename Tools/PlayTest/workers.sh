#!/usr/bin/env bash
# Parallel PlayMode workers for the balance pipeline (round 25; Docs/COMMANDS.md "Round 25: impact and parallel runs").
# Every worker is a clone of the project (Assets, Packages, ProjectSettings, Library, Tools) under TD_WORKERS_ROOT that runs
# the measure or replay test headless (Unity -batchmode -runTests, no editor window) on its own (cell, seed) units, written
# into its own probe_arg.txt; the records of all workers are merged into the project's records as measure_cells.sh and
# replay_cells.sh do. The open editor is never touched. Each clone carries its own product name ("Tower defense worker wN"),
# so its persistentDataPath is its own and Remco's save (DefaultCompany/Tower defense/Saves) is never read or written; the
# script checks the save's MD5 before and after anyway and restores it if it changed.
#
#   bash Tools/PlayTest/workers.sh count                                   # the N this machine can carry
#   bash Tools/PlayTest/workers.sh sync [N] [--library]                    # make or refresh N clones (Library only when missing, or --library)
#   bash Tools/PlayTest/workers.sh run replay [N] "cells=2:0,3:2" "seeds=1,2,3"
#   bash Tools/PlayTest/workers.sh run measure [N] "levels=2" "difficulties=0"
#
# Filter lines are the probe_arg.txt lines: levels=, difficulties=, seeds=, cells=node:difficulty[:seed]. Without levels the
# campaign's nodes TD_LEVELS (default 0..5, Crystal Plains) are taken; without seeds 1,2,3. Units are dealt round robin.
# Env: TD_WORKERS_ROOT (default C:\Users\remco\TowerDefenseWorkers), TD_WORKERS (N), TD_LEVELS, TD_UNITY (the editor exe).
set -euo pipefail
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
PROJECT="$(pwd -W 2>/dev/null || pwd)"
ROOT="${TD_WORKERS_ROOT:-C:\\Users\\remco\\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.5f1\\Editor\\Unity.exe}"
LEVELS="${TD_LEVELS:-0,1,2,3,4,5}"
# The probe turns the cameras off in batch mode, so the GPU idles; URP still wants a device (without one it logs render errors), so
# -nographics is opt-in (TD_GRAPHICS=0) for machines without a GPU.
GRAPHICS="${TD_GRAPHICS:-1}"
# Job threads per worker: six editors each spinning sixteen job threads thrash the cores (round 25 measured 20x slower runs).
JOB_WORKERS="${TD_JOB_WORKERS:-2}"
SAVE="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"

count() {
  local cores ram
  cores=$(nproc)
  ram=$(powershell -NoProfile -Command "[int]((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB)" | tr -d '\r')
  # A headless worker grows to about 4 GB over a long run and keeps one or two cores busy; six workers on 32 GB paged and ran
  # six times slower per run (round 25), so RAM sets the number: 4 GB each with 10 GB kept for the editor and the system.
  local by_cores=$(( cores / 2 - 1 )) by_ram=$(( (ram - 10) / 4 ))
  local n=$(( by_cores < by_ram ? by_cores : by_ram ))
  [ "$n" -lt 1 ] && n=1
  echo "$n"
}

winpath() { cygpath -w "$1"; }

sync_worker() {
  local i=$1 force_library=$2 dir="$ROOT\\w$i"
  mkdir -p "$(cygpath -u "$dir")/Logs"
  for d in Assets Packages ProjectSettings Tools; do
    robocopy "$PROJECT\\$d" "$dir\\$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
  done
  if [ "$force_library" = 1 ] || [ ! -d "$(cygpath -u "$dir")/Library" ]; then
    robocopy "$PROJECT\\Library" "$dir\\Library" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
  fi
  # The clone's own persistentDataPath: its product name differs, so it never sees Remco's save folder.
  sed -i "s/^  productName: Tower defense\$/  productName: Tower defense worker w$i/" "$(cygpath -u "$dir")/ProjectSettings/ProjectSettings.asset"
  rm -f "$(cygpath -u "$dir")"/Assets/InitTestScene*.unity*
}

sync_all() {
  local n=$1 force=$2
  for i in $(seq 1 "$n"); do sync_worker "$i" "$force"; done
  echo "synced $n workers under $ROOT"
}

# Reads the filter lines into LEVELS_F, DIFFS_F, SEEDS_F and CELLS_F.
parse_filter() {
  LEVELS_F="$LEVELS"; DIFFS_F="0,1,2,3"; SEEDS_F="1,2,3"; CELLS_F=""
  for line in "$@"; do
    case "$line" in
      levels=*) LEVELS_F="${line#levels=}" ;;
      difficulties=*) DIFFS_F="${line#difficulties=}" ;;
      seeds=*) SEEDS_F="${line#seeds=}" ;;
      cells=*) CELLS_F="${line#cells=}" ;;
      step=*|seed=*) EXTRA_F="${EXTRA_F:-}$line"$'\n' ;;
      *) echo "unknown filter line: $line" >&2; exit 1 ;;
    esac
  done
}

# The (node:difficulty:seed) units of a filter, one per line.
units() {
  if [ -n "$CELLS_F" ]; then
    for cell in ${CELLS_F//,/ }; do
      IFS=: read -r node diff seed <<< "$cell"
      if [ -n "${seed:-}" ]; then echo "$node:$diff:$seed"; else for s in ${SEEDS_F//,/ }; do echo "$node:$diff:$s"; done; fi
    done
  else
    for node in ${LEVELS_F//,/ }; do for diff in ${DIFFS_F//,/ }; do for s in ${SEEDS_F//,/ }; do echo "$node:$diff:$s"; done; done; done
  fi
}

merge_records() {
  # $1 the project's records, $2 the new records (whole cells): keep the old records of every cell outside the new file, add the new ones.
  local records=$1 new=$2
  awk -F'|' '$1 == "run" { print $2 "|" $3 "|" $13 }' "$new" | sort -u > "$records.cells"
  [ -f "$records" ] || : > "$records"
  awk -F'|' -v cells="$records.cells" '
    BEGIN { while ((getline line < cells) > 0) { drop[line] = 1 } }
    $1 == "run" { keep = !((($2 "|" $3 "|" $13) in drop)) }
    keep { print }
  ' "$records" > "$records.merged"
  cat "$new" >> "$records.merged"
  mv "$records.merged" "$records"
  rm -f "$records.cells"
}

run() {
  local stage=$1; shift
  local n=${1:-}; if [[ "$n" =~ ^[0-9]+$ ]]; then shift; else n="${TD_WORKERS:-$(count)}"; fi
  local test records table
  case "$stage" in
    measure) test=Game.Tests.BalancePipelineTests.Measure_Yardstick_ObviousFamily; records=calibration_records.txt; table=yardstick.txt ;;
    replay)  test=Game.Tests.PlanReplayTests.Replay_SearchedPlans_EveryCell; records=plan_records.txt; table=plan_replay.txt ;;
    *) echo "run <measure|replay> [N] [filter lines]" >&2; exit 1 ;;
  esac
  EXTRA_F=""
  parse_filter "$@"
  mapfile -t UNITS < <(units)
  [ "${#UNITS[@]}" -gt 0 ] || { echo "no units match the filter" >&2; exit 1; }
  [ "$n" -gt "${#UNITS[@]}" ] && n=${#UNITS[@]}
  local save_md5="" save_copy=""
  if [ -f "$SAVE" ]; then save_md5=$(md5sum "$SAVE" | cut -d' ' -f1); save_copy=$(mktemp); cp "$SAVE" "$save_copy"; fi
  local started=$(date +%s)
  echo "== workers: $stage, ${#UNITS[@]} units over $n workers ($(date +%H:%M:%S))"
  # Every clone refreshed first, side by side (a first Library copy takes a while), then every worker launched at once.
  for i in $(seq 1 "$n"); do sync_worker "$i" 0 & done
  wait
  echo "  synced $n workers in $(( $(date +%s) - started )) s"
  for i in $(seq 1 "$n"); do
    local dir; dir="$(cygpath -u "$ROOT\\w$i")"
    local mine=() seeds=()
    for ((u = i - 1; u < ${#UNITS[@]}; u += n)); do mine+=("${UNITS[u]}"); seeds+=("${UNITS[u]##*:}"); done
    {
      echo "cells=$(IFS=,; echo "${mine[*]}")"
      echo "seeds=$(printf '%s\n' "${seeds[@]}" | sort -un | paste -sd,)"
      printf '%s' "${EXTRA_F}"
    } > "$dir/Tools/PlayTest/probe_arg.txt"
    rm -f "$dir/Tools/PlayTest/$records" "$dir/Tools/PlayTest/$table" "$dir/Logs/results.xml"
    echo "  w$i: ${#mine[@]} units: ${mine[*]}"
    "$UNITY" -batchmode $([ "$GRAPHICS" = 1 ] || echo -nographics) -job-worker-count "$JOB_WORKERS" -timestamps -projectPath "$ROOT\\w$i" -runTests -testPlatform PlayMode -testFilter "$test" \
      -testResults "$ROOT\\w$i\\Logs\\results.xml" -logFile "$ROOT\\w$i\\Logs\\unity.log" > /dev/null 2>&1 &
  done
  wait
  local ended=$(date +%s)
  echo "== workers done in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s ($(date +%H:%M:%S))"
  local new="Tools/PlayTest/$records.workers" partial="Tools/PlayTest/${table%.txt}_partial.txt" failed=0
  : > "$new"
  echo "Parallel $stage over $n workers, $(date '+%Y-%m-%d %H:%M'); units: ${UNITS[*]}" > "$partial"
  for i in $(seq 1 "$n"); do
    local dir; dir="$(cygpath -u "$ROOT\\w$i")"
    local result; result=$(grep -o 'result="[A-Za-z]*"' "$dir/Logs/results.xml" 2>/dev/null | head -1 || echo 'result="missing"')
    local errors; errors=$(grep -c "\[Probe\] error" "$dir/Logs/unity.log" 2>/dev/null || true)
    echo "  w$i: $result, $errors probe errors, $(grep -c '^run|' "$dir/Tools/PlayTest/$records" 2>/dev/null || echo 0) runs"
    if [ -f "$dir/Tools/PlayTest/$records" ]; then cat "$dir/Tools/PlayTest/$records" >> "$new"; else failed=1; fi
    if [ -f "$dir/Tools/PlayTest/$table" ]; then { echo; echo "---- worker w$i"; cat "$dir/Tools/PlayTest/$table"; } >> "$partial"; fi
    rm -f "$dir"/Assets/InitTestScene*.unity*
  done
  if [ -f "$SAVE" ] || [ -n "$save_md5" ]; then
    local now_md5=""; [ -f "$SAVE" ] && now_md5=$(md5sum "$SAVE" | cut -d' ' -f1)
    if [ "$now_md5" != "$save_md5" ]; then
      echo "WARNING: the save changed during the run ($save_md5 -> $now_md5); restoring it." >&2
      if [ -n "$save_copy" ]; then cp "$save_copy" "$SAVE"; else rm -f "$SAVE"; fi
    else
      echo "  save untouched (MD5 ${save_md5:-none})"
    fi
  fi
  [ -n "$save_copy" ] && rm -f "$save_copy"
  if [ "$failed" = 1 ]; then echo "a worker wrote no records: see $ROOT\\wN\\Logs\\unity.log" >&2; exit 1; fi
  merge_records "Tools/PlayTest/$records" "$new"
  rm -f "$new"
  echo "records merged: $(grep -c '^run|' "Tools/PlayTest/$records") runs in Tools/PlayTest/$records; tables in $partial"
}

case "${1:-}" in
  count) count ;;
  sync) shift; n="${1:-}"; if [[ "$n" =~ ^[0-9]+$ ]]; then shift; else n="${TD_WORKERS:-$(count)}"; fi; force=0; [ "${1:-}" = "--library" ] && force=1; sync_all "$n" "$force" ;;
  run) shift; run "$@" ;;
  *) sed -n '2,19p' "$0"; exit 1 ;;
esac
