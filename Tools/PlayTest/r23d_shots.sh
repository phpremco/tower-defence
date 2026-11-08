#!/usr/bin/env bash
# Round 23d: Spencer's jetpack switch, his weapons and face, and the NTI flag at the base. Loads campaign node $2
# (default 1) on Medium with the waves off; logs the hero's flight frame by frame (r23d_hero.cs state) and renders 16:9
# stills (render_still.cs, the Game view is left alone) into $1 with prefix $3. Keeps the save exactly as it was.
# Usage: r23d_shots.sh <output dir> [node] [prefix] [far post]
set -u
cd "$(dirname "$0")/../.." || exit 1
hold=
out=$1; node=${2:-1}; prefix=${3:-r23d}; far=${4:-2}
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1${hold:+ hold}" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-900; }
hero() { echo "$*" > Tools/PlayTest/r23d_hero.txt; run r23d_hero.cs; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; run render_still.cs >/dev/null; }
step 60
echo "node $node" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 12); do step 60; sleep 1; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
echo "waves: $(run no_waves.cs)"
step 30
echo "=== flag at the base"
hero base 7; step 30; still flag_game
hero flag 1.8; step 30; still flag_close
echo "=== idle close-up"
hero fps 30
hold=1
hero view 1.0 1.45; step 20; still idle_face
hero face cam; hero view 0.45 1.6; step 1; still face_visor
hero view 1.8; step 10
echo "=== blade draw"
hero draw melee
for i in 1 2 3 4 5 6; do step 4; hero state; [ $i = 3 ] && still draw_blade_a; [ $i = 5 ] && still draw_blade_b; done
step 40; hero state
hero draw none; for i in 1 2 3 4 5 6 7; do step 4; hero state; done
echo "=== pistol draw"
hero draw ranged; for i in 1 2 3 4 5; do step 4; hero state; [ $i = 3 ] && still draw_pistol; done
step 20; hero draw none; for i in 1 2 3 4 5 6; do step 4; hero state; done
step 20
echo "=== flight to post $far"
hero view 7 0
hero move "$far"
for i in $(seq 1 40); do
  step 3
  s=$(hero state); echo "$i $s"
  case "$s" in *"view flight Takeoff"*) [ ! -f "$out/${prefix}_takeoff.png" ] && still takeoff;; esac
  case "$s" in *"view flight Cruise"*) [ ! -f "$out/${prefix}_flight.png" ] && { hero view 3 1; step 1; still flight; hero view 7 0; };; esac
  case "$s" in *"view flight Landing"*) [ ! -f "$out/${prefix}_landing.png" ] && { hero view 2.5 0.8; step 1; still landing; hero view 7 0; };; esac
  case "$s" in *"view flight None"*"walking False"*) break;; esac
done
hero view 7 0; step 5; still arrived
hold=
hero fps 0
step 1
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -o '"consoleErrors": [0-9]*\|"consoleWarnings": [0-9]*'
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 2
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
