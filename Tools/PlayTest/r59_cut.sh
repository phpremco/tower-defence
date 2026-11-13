#!/usr/bin/env bash
# Round 59 (n2_cut.sh with the save kept and a capsule zoom): plays the opening cutscene from Boot and, per argument, renders
# a sought moment ("<shot> <progress>", n2_still.cs, into $CUT_OUT/<prefix>_<shot>_<progress>.png), a magnified capsule
# ("z59:<name>|<shot> <progress>|<capsule> <nozzle|body|head|tail|mid> <fov>", r59_zoom.cs, into $CUT_OUT/<prefix>_<name>.png),
# or runs a probe ("probe:<file.cs>"). Copies progress.json aside first and restores it after (MD5 printed at both ends).
# Usage: CUT_OUT=<dir> bash Tools/PlayTest/r59_cut.sh <prefix> "<shot> <progress>" probe:r59_salvo_probe.cs ...
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=${1:-cut}
out=${CUT_OUT:-Logs/r59}
shift
mkdir -p "$out" Logs/r59
save="/c/Users/remco/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="Logs/r59/progress_backup_$$.json"
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; echo "save before $(md5sum "$save" | cut -c1-32)"; else echo "no progress.json before"; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-4000; }
step 20
run cutscene_start.cs
sleep 4
step 10
for moment in "$@"; do
  case "$moment" in
    probe:*) echo "$(run "${moment#probe:}")" ;;
    z59:*) spec="${moment#z59:}"; name="${spec%%|*}"; rest="${spec#*|}"; echo "${rest%%|*}" > Tools/PlayTest/cut_arg.txt; echo "${rest#*|}" > Tools/PlayTest/r59_zoom_arg.txt; echo "$(cygpath -w "$out/${prefix}_${name}.png")" > Tools/PlayTest/look_arg.txt; echo "  $(run r59_zoom.cs)" ;;
    *)
      echo "$moment" > Tools/PlayTest/cut_arg.txt
      name=$(echo "$moment" | tr ' .' '_p')
      echo "$(cygpath -w "$out/${prefix}_${name}.png")" > Tools/PlayTest/look_arg.txt
      echo "  $(run n2_still.cs)" ;;
  esac
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; echo "save after restore $(md5sum "$save" | cut -c1-32)"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
