#!/usr/bin/env bash
# Round 63: the base and its shuttle in a campaign level. Loads level <index> on Medium from Boot, skips the briefing,
# points the play camera at the base ("Structure", zoom ZOOM) and, per argument, "shuttle:<seconds>" puts the shuttle's
# round there (r63_shuttle.cs, seconds after its rest ends) and renders the play view, "base" renders the base as it stands,
# "probe:<file.cs>" runs a probe. Save copied aside first, restored and checked against r63_save_md5.txt at the end.
# Usage: OUT=<dir> ZOOM=<z> bash Tools/PlayTest/r63_level.sh <index> <prefix> shuttle:0.5 ...
set -u
cd "$(dirname "$0")/../.." || exit 1
index=$1; prefix=$2; shift 2
out=${OUT:-Logs/r63}; zoom=${ZOOM:-2.4}
mkdir -p "$out"
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before_$$.json"
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
echo "1" > Tools/PlayTest/r49_light.txt
echo "$index" > Tools/PlayTest/level_arg.txt
echo "level $index: $(run start_level.cs)"; sleep 8; step 90 hold; run r16_dialogue_skip.cs >/dev/null; step 30 hold
echo "Structure ${zoom} 0" > Tools/PlayTest/r60_view.txt
echo "  $(run r60_view.cs)" | cut -c1-200
step 3 hold
for a in "$@"; do
  case "$a" in
    shuttle:*) t="${a#shuttle:}"; echo "$t" > Tools/PlayTest/r63_shuttle_arg.txt; echo "  $(run r63_shuttle.cs)"; still "shuttle_$(echo "$t" | tr '.-' 'pm')" ;;
    base) still base ;;
    probe:*) echo "  $(run "${a#probe:}")" ;;
  esac
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; got=$(md5sum "$save" | cut -c1-32); want=$(cat Tools/PlayTest/r63_save_md5.txt | tr -d '[:space:]')
echo "save after restore $got"; [ "$got" = "$want" ] && echo "save md5 verified" || echo "SAVE MD5 MISMATCH want $want"
