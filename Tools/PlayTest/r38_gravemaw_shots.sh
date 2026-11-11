#!/usr/bin/env bash
# Round 38: Gravemaw's worm maw in play. Loads the Gravemaw arena from Boot, waits for the boss to surface, then renders
# 1920x1080 stills (r38_still.cs, no screen-space interface): the play camera, and a Roar (maw open) at the play zoom and
# close, near and far LOD, plus a renderer/material probe (r38_maw.cs). Keeps the save exactly as it was.
# It waits for the roar Gravemaw plays after it surfaces at the rig. FACE=1 turns it towards the camera for that roar
# (a diagnostic of the throat). Usage: [FACE=1] r38_gravemaw_shots.sh <output dir> <prefix> [close zoom]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; close=${3:-3.5}
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1 hold" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-1500; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "$1: $(run r38_still.cs)"; }
maw() { echo "$1" > Tools/PlayTest/r38_arg.txt; run r38_maw.cs; }
echo Gravemaw > Tools/PlayTest/r14_boss.txt; step 60; sleep 2
for i in 1 2 3; do r=$(run r14_arena.cs); echo "$r"; case "$r" in *loading*) break;; esac; sleep 5; done; sleep 8
run r17_fixed_step.cs
for i in $(seq 1 60); do step 10; run skip_wave.cs >/dev/null; b=$(run r14_boss.cs); case "$b" in *" at ("*) echo "$b"; break;; esac; done
run r16_dialogue_skip.cs >/dev/null
# Wait for its own roar after it surfaces at the rig (BurrowSignature.Breakout -> ContinentBossBrain.Roar).
run r16_dialogue_skip.cs >/dev/null
echo "probe: $(maw probe)"
for i in $(seq 1 80); do step 4; b=$(run r14_boss.cs); case "$b" in *"boss Roar"*) echo "$b"; break;; esac; done
if [ -n "${FACE:-}" ]; then echo "face: $(maw face)"; fi
t=0; for f in 4 4 4 4 4 4 4 4; do step "$f"; t=$((t + f)); echo "play" > Tools/PlayTest/r16_focus.txt; run r16_focus_boss.cs >/dev/null; still "roar_play_f$t"; echo "$close" > Tools/PlayTest/r16_focus.txt; run r16_focus_boss.cs >/dev/null; still "roar_close_f$t"; done
run r14_boss.cs
# The far LOD with a scripted roar.
maw roar; step 12
maw "lod 1"; step 1
echo "probe far: $(maw probe)"
echo "$close" > Tools/PlayTest/r16_focus.txt; run r16_focus_boss.cs >/dev/null
t=12; for f in 6 6; do step "$f"; t=$((t + f)); still "roar_far_f$t"; done
maw "lod -1"
# On the road at the play camera.
for i in $(seq 1 30); do step 10; run r16_dialogue_skip.cs >/dev/null; done
run r14_boss.cs
echo "play" > Tools/PlayTest/r16_focus.txt; run r16_focus_boss.cs >/dev/null; still road_play
run r14_boss.cs
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
