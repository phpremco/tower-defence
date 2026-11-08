#!/usr/bin/env bash
# Round 23c: the gate repair flicker. Loads campaign node $2 (default 1) on Medium with the waves off, frames the first
# gate and logs its view state frame by frame through: damage to 30 % and a full repair; a wreck and a rebuild; a repair
# drone's trickle of heal against hits around the damage line; the hero's repair. Stills go to $1 with prefix $3.
# Keeps the save exactly as it was. Usage: r23c_gate.sh <output dir> [node] [prefix]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; node=${2:-1}; prefix=${3:-r23c}
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1500; }
gate() { echo "$*" > Tools/PlayTest/r23c_gate.txt; run r23c_gate.cs; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; run render_still.cs >/dev/null; }
step 60
echo "node $node" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 12); do step 60; sleep 1; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
echo "waves: $(run no_waves.cs)"
step 30
gate focus 3
step 5
echo "=== A: damage to 30 %, full repair"
gate damage 0.7
step 10; still A_damaged
gate repairfully
for i in $(seq 1 12); do step 3; gate state; [ $i -le 6 ] && still "A_$i"; done
echo "=== B: wreck and rebuild"
gate wreck
for i in 1 2 3 4; do step 30; gate state; done
still B_wreck
gate repairfully
for i in $(seq 1 14); do step 4; gate state; [ $((i % 2)) = 0 ] && still "B_$i"; done
echo "=== C: drone trickle against hits around the damage line"
gate damage 0.52
step 10
for i in $(seq 1 16); do gate repair 0.01; step 1; [ $((i % 4)) = 0 ] && gate damage 0.03; gate state; [ $((i % 2)) = 0 ] && still "C_$i"; done
echo "=== D: hero repair from below the line"
gate damage 0.3
step 10; gate state
gate restore 0.35
for i in $(seq 1 8); do step 4; gate state; done
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 2
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
