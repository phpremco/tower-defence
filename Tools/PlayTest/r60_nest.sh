#!/usr/bin/env bash
# Round 60: stills of the spawn props of campaign levels (Remco: "looks like a spawn inside a spawn"). For each
# "<campaign index>:<object prefix>:<zoom>[:<nth>]" in R60_SHOTS it loads the level on Medium, waits, points the play
# camera at that object (r60_view.cs, which also lists what stands within 4 m) and renders it as in play and with the
# inspection light (r49_still.cs). Copies the save aside first, restores and verifies it at the end.
# Usage: R60_SHOTS="1:Origin_0:3.2 2:SpawnGate_0:3.2" bash Tools/PlayTest/r60_nest.sh <output dir> <prefix>
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2
mkdir -p "$out"
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$*" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-2400; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r49_still.cs)"; }
echo "-" > Tools/PlayTest/r45_face.txt
loaded=-1
for shot in ${R60_SHOTS}; do
  IFS=: read -r index name zoom nth <<< "$shot"
  if [ "$index" != "$loaded" ]; then
    echo "$index" > Tools/PlayTest/level_arg.txt
    echo "level $index: $(run start_level.cs)"; sleep 8; step 90 hold; run r16_dialogue_skip.cs >/dev/null; step 30 hold
    loaded=$index
  fi
  echo "${name} ${zoom} ${nth:-0}" > Tools/PlayTest/r60_view.txt
  echo "  $(run r60_view.cs)"
  step 3 hold
  echo "1" > Tools/PlayTest/r49_light.txt; still "L${index}_${name}_${nth:-0}_game"
  echo "1.8" > Tools/PlayTest/r49_light.txt; still "L${index}_${name}_${nth:-0}_lit"
done
echo "1" > Tools/PlayTest/r49_light.txt
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; echo "save after restore $(md5sum "$save" | cut -c1-32)"
