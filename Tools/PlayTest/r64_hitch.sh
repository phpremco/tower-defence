#!/usr/bin/env bash
# Round 64: measures the frame hitch of starting a level (r64_hitch.cs). Plays from Boot, waits for the menu, starts campaign
# node <node> on difficulty <d> the old way ("old") or through the loading screen ("new"), lets it run free for WAIT seconds
# and writes Logs/r64/hitch_<label>.txt. Save copied aside first, restored and checked against r64_save_md5.txt at the end.
# Usage: bash Tools/PlayTest/r64_hitch.sh old|new <node> <difficulty> <label>
set -u
cd "$(dirname "$0")/../.." || exit 1
mode=$1; node=$2; diff=$3; label=$4
out=Logs/r64; mkdir -p "$out"
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${label}_progress_before_$$.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 10
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-2400; }
echo "$mode $node $diff" > Tools/PlayTest/r64_hitch_arg.txt
echo "start: $(run r64_hitch.cs)"
sleep "${WAIT:-12}"
echo "stop $label" > Tools/PlayTest/r64_hitch_arg.txt
echo "report: $(run r64_hitch.cs)"
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; got=$(md5sum "$save" | cut -c1-32); want=$(cat Tools/PlayTest/r64_save_md5.txt | tr -d '[:space:]')
echo "save after restore $got"; [ "$got" = "$want" ] && echo "save md5 verified" || echo "SAVE MD5 MISMATCH want $want"
