#!/usr/bin/env bash
# Round 47: plays the level at campaign index LEVEL (default 0, Level_C1_01) with probe towers, records every animated rig per frame
# (r47_rec_start.cs) through the waves in WAVES at game speed SPEED for SECS real seconds each, and writes the report.
# Usage: TAG=before_x1 SPEED=1 WAVES="3 6" SECS=40 bash Tools/PlayTest/r47_play.sh. Restores the save afterwards.
set -u
cd "$(dirname "$0")/../.." || exit 1
tag=${TAG:-run}; speed=${SPEED:-1}; waves=${WAVES:-"3 6"}; secs=${SECS:-40}; level=${LEVEL:-0}
mkdir -p Logs/r47
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="Logs/r47/progress_backup_$tag.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*' | cut -c12-3000; }
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
echo "$level" > Tools/PlayTest/level_arg.txt
echo "start: $(run start_level.cs)"
sleep 8
echo "$tag" > Tools/PlayTest/r47_arg.txt
echo "$speed" > Tools/PlayTest/r47_speed.txt
echo "rec: $(run r47_rec_start.cs)"
for w in $waves; do
  echo "$w" > Tools/PlayTest/r47_wave.txt
  echo "level: $(run r47_level.cs)"
  sleep "$secs"
done
echo "stop: $(run r47_rec_stop.cs)"
echo "report: $(run r47_rec_report.cs)"
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; echo "save after restore $(md5sum "$save" | cut -c1-32)"
