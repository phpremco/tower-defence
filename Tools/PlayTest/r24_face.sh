#!/usr/bin/env bash
# Round 24 step 0: one face close-up of Spencer after a Mercer re-sync. Loads campaign node $2 (default 1), waves off,
# renders <out>/<prefix>_face_visor.png and <prefix>_idle_face.png. Keeps the save exactly as it was (checked by the caller).
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; node=${2:-1}; prefix=${3:-r24}
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
hold=
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
hero fps 30
hold=1
hero view 1.0 1.45; step 20; still idle_face
hero face cam; hero view 0.45 1.6; step 1; still face_visor
unity command editor_stop --result-only 2>&1 | grep -o '"success":[a-z]*'
echo "errors: $(unity command get_console_logs --result-only 2>&1 | grep -c '"type": *"Error"')"
