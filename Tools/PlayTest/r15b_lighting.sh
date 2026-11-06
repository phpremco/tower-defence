#!/usr/bin/env bash
# Round 15b: paired stills of the lighting trial. For each target ("node <i>" or "arena <boss>") loads it with the trial
# off and then on (r15b_flag.cs), logs the lights (r15b_lights.cs) and stills the start view; first opens and closes the codex
# (writing its Frost Warden preview), since its display stages stay in the scene from then on. Usage: r15b_lighting.sh <output dir> "<target>|<label>" ...
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; shift
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1500; }
step 60
for spec in "$@"; do
  target=${spec%%|*}; label=${spec##*|}
  for mode in off on; do
    echo "$mode" > Tools/PlayTest/r15b_flag.txt; run r15b_flag.cs
    echo "$target" > Tools/PlayTest/r15b_load.txt; sleep 1; run r15b_load.cs; sleep 8
    for i in $(seq 1 20); do step 30; sleep 2; r=$(run no_waves.cs); case "$r" in *"waves off"*) break;; esac; done
    step 60
    # The display stages are built when the codex first opens and stay for the level: open and close it first, so the
    # still shows the field as it is lit for the rest of the level.
    echo FrostWarden > Tools/PlayTest/r15_codex_name.txt
    echo "$out/72_codex_${label}_${mode}.png" > Tools/PlayTest/look_arg.txt; run r15_codex.cs
    run codex_close.cs; step 30
    echo "$label $mode: $(run r15b_lights.cs)"
    echo "$out/72_${label}_${mode}.png" > Tools/PlayTest/look_arg.txt; run render_still.cs
  done
done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
