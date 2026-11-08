#!/usr/bin/env bash
# Round 23a: the same level stilled at play zoom, halfway and fully zoomed out (no codex, no extra lights), to check that
# zooming out alone does not wash the look out. Usage: r23a_zoom.sh <output dir> [node]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; node=${2:-1}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
step 60
echo "node $node" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "waves: $(run no_waves.cs)"
: > Tools/PlayTest/r23a_lum.txt
for z in 2 3.2 4.5; do
  echo "$z" > Tools/PlayTest/zoom_size.txt; echo "$(run r23a_zoomset.cs)"; step 10
  echo "$out/r23a_zoom_$z.png" > Tools/PlayTest/look_arg.txt; run render_still.cs >/dev/null
  echo "$out/r23a_zoom_$z.png" >> Tools/PlayTest/r23a_lum.txt
done
echo "lum: $(run r23a_lum.cs)"
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
