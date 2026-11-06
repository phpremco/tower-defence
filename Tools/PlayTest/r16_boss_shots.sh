#!/usr/bin/env bash
# Round 16: loads a boss arena, starts its wave, lets the boss walk the road for a while, stills its ground trail at the
# play zoom and close, three close frames of its body a few frames apart (moving parts), and probes every steady
# spinner in the scene (r16_spin_probe.cs, with one drone of each kind out). Keeps the save exactly as it was.
# Usage: r16_boss_shots.sh <output dir> <boss> <prefix> [road steps of 60 frames]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; boss=$2; prefix=$3; walk=${4:-12}
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
# Warm the eval compiler up in edit mode: the first eval after a domain reload can miss the 5 s main-thread limit.
unity command eval_file --file Tools/PlayTest/r16_compiled.cs --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-400; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; run render_still.cs; }
focus() { echo "$1" > Tools/PlayTest/r16_focus.txt; run r16_focus_boss.cs; }
echo "$boss" > Tools/PlayTest/r14_boss.txt; step 60; sleep 2; for i in 1 2 3; do r=$(run r14_arena.cs); echo "$r"; case "$r" in *loading*) break;; esac; sleep 5; done; sleep 8
for i in $(seq 1 30); do step 30; sleep 1; r=$(run skip_wave.cs); b=$(run r14_boss.cs); case "$b" in *" at ("*) echo "$b"; break;; esac; done
run buy_drones.cs
for i in $(seq 1 60); do step 60; b=$(run r14_boss.cs); case "$b" in *"boss Road"*) echo "$b"; break;; esac; done
for i in $(seq 1 "$walk"); do step 60; done
run r16_dialogue_skip.cs; step 30
run r14_boss.cs
focus "play 2"; step 1; still trail_play
focus "1.7 2.2"; step 1; echo old > Tools/PlayTest/r16_furrow_arg.txt; run r16_furrow_mode.cs; run r16_trail_mode.cs; step 1; still trail_near_before
echo new > Tools/PlayTest/r16_furrow_arg.txt; run r16_furrow_mode.cs; run r16_trail_mode.cs; step 1; still trail_near_after
focus "3.2 2.5"; step 1; still trail_close
focus "2.6 -0.5"; step 1; still body_a
step 8; still body_b
step 8; still body_c
run r16_trail_probe.cs
echo hide > Tools/PlayTest/r16_furrow_arg.txt; run r16_hide_boss.cs; focus "2 1"; still ground_under
echo show > Tools/PlayTest/r16_furrow_arg.txt; run r16_hide_boss.cs
run r16_spin_probe.cs
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 2
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
