#!/usr/bin/env bash
# Round menu 2026-11-04: plays Boot, opens the upgrade screen and renders it at 1920x1080 with a subject selected for
# each name given ($OUT/<prefix>_<name>.png; base_select-style names matched by preview_audit.cs), then audits every
# subject's preview (preview_audit.cs, pictures in $OUT/previews). Stops Play Mode. Usage: upgrade_shots.sh <prefix> [name ...]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-upgrade}; shift || true
out=${OUT:-Logs}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-3000; }
still() { echo "$out/${prefix}_$1.png" > Tools/PlayTest/look_arg.txt; unity command eval_file --file Tools/PlayTest/render_still.cs --result-only 2>&1 | grep -o "wrote[^\"]*"; }
step 60; run intro_skip.cs
echo "0" > Tools/PlayTest/points_arg.txt
run goto_base.cs; sleep 4; step 40; step 40
for name in "$@"; do
  echo "$name" > Tools/PlayTest/zoom_arg.txt; echo "$out/previews" > Tools/PlayTest/look_arg.txt; run preview_audit.cs; step 30
  still "$(echo "$name" | tr ' ' '_')"
done
echo "" > Tools/PlayTest/zoom_arg.txt; echo "$out/previews" > Tools/PlayTest/look_arg.txt
echo "=== AUDIT"; run preview_audit.cs
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
