#!/usr/bin/env bash
# Round 64: stills of the loading screen. Plays from Boot, waits for the menu, pauses the editor, starts campaign node <node>
# on difficulty <d> through Game.UI.LevelLoader (r64_hitch.cs "new", its recorder left running) and steps the paused editor
# frame by frame; after the step counts given it renders the screen off-screen at 1920x1080 and 1280x720 (r64_shot.cs).
# Save copied aside first, restored and checked against r64_save_md5.txt at the end.
# Usage: OUT=<dir> bash Tools/PlayTest/r64_loading.sh <node> <difficulty> <prefix> <frames> [<frames> ...]
set -u
cd "$(dirname "$0")/../.." || exit 1
node=$1; diff=$2; prefix=$3; shift 3
out=${OUT:-Logs/r64}; mkdir -p "$out"
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before_$$.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 10
step() { echo "$1 hold" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-600; }
shot() {
  echo "1920 1080 $(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/r64_shot_arg.txt; echo "  $1: $(run r64_shot.cs)"
  echo "1280 720 $(cygpath -w "$out/${prefix}_$1_720.png")" > Tools/PlayTest/r64_shot_arg.txt; run r64_shot.cs > /dev/null
}
step 2
shot 00_before
echo "new $node $diff" > Tools/PlayTest/r64_hitch_arg.txt
echo "start: $(run r64_hitch.cs)"
total=0
for frames in "$@"; do
  step "$frames"; total=$((total + frames))
  shot "$(printf '%03d' "$total")"
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep '"message"' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; got=$(md5sum "$save" | cut -c1-32); want=$(cat Tools/PlayTest/r64_save_md5.txt | tr -d '[:space:]')
echo "save after restore $got"; [ "$got" = "$want" ] && echo "save md5 verified" || echo "SAVE MD5 MISMATCH want $want"
