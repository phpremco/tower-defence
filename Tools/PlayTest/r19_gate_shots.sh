#!/usr/bin/env bash
# Round 19: loads the Gravemaw arena, steps until the boss starts its own gate crossing on the first gate of its route
# (r19_step_until.cs) and stills Gate_Mount (spades over the crowns and down to the far ground) every 16 frames, close on
# the boss at 1/30 s per stepped frame. Keeps the save exactly as it was.
# Usage: r19_gate_shots.sh <output dir> <prefix>
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command eval_file --file Tools/PlayTest/r16_compiled.cs --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-300; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; run render_still.cs; }
echo Gravemaw > Tools/PlayTest/r14_boss.txt; step 60; sleep 2
for i in 1 2 3; do r=$(run r14_arena.cs); echo "$r"; case "$r" in *loading*) break;; esac; sleep 5; done; sleep 8
run r17_fixed_step.cs
echo "3.2" > Tools/PlayTest/r16_focus.txt
for i in $(seq 1 60); do step 10; run skip_wave.cs >/dev/null; b=$(run r14_boss.cs); case "$b" in *" at ("*) echo "$b"; break;; esac; done
run r16_dialogue_skip.cs >/dev/null
echo "ClimbingGate 0" > Tools/PlayTest/r19_until.txt
for i in $(seq 1 50); do r=$(run r19_step_until.cs); case "$r" in *reached*) echo "$r"; break;; esac; [ $((i % 5)) = 0 ] && run r16_dialogue_skip.cs >/dev/null; done
for n in $(seq 1 11); do run r16_focus_boss.cs; still "mount_$n"; echo "ClimbingGate 16" > Tools/PlayTest/r19_until.txt; run r19_step_until.cs; done
run r14_boss.cs
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 2
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
