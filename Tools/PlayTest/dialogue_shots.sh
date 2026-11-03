#!/usr/bin/env bash
# Plays level 1 from Boot and captures the dialogue box for each sample speaker into Assets/Temp/dlg_<sample>.png
# (typing half-way and complete for the first), then prints the console summary and stops play mode.
# Usage: dialogue_shots.sh [samples...]   (default: mercer ora voss nti pioneer fx). Restore the save afterwards.
set -u
cd "$(dirname "$0")/../.." || exit 1
samples=${*:-mercer ora voss nti pioneer fx}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-120; }
shot() { unity command capture_game_view --save_path "Temp/dlg_$1.png" --result-only >/dev/null 2>&1; }
step 60
run intro_skip.cs
echo 0 > Tools/PlayTest/level_arg.txt
run start_level.cs
sleep 6
step 100; step 100
first=1
for s in $samples; do
  echo "$s" > Tools/PlayTest/dialogue_arg.txt
  run dialogue_show.cs
  step 2
  if [ $first = 1 ]; then step 40; shot "${s}_typing"; first=0; fi
  run dialogue_finish.cs
  step 30
  shot "$s"
  if [ -n "${PORTRAITS:-}" ]; then echo "$PORTRAITS/portrait_$s.png" > Tools/PlayTest/portrait_arg.txt; run portrait_dump.cs; fi
  [ "${PROBE:-0}" = 1 ] && unity command eval_file --file Tools/PlayTest/dialogue_probe.cs --result-only 2>&1 | grep -o '"result": "[^"]*'
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
ls Assets/Temp | grep dlg_ | grep -v meta
unity command editor_stop --result-only >/dev/null 2>&1
