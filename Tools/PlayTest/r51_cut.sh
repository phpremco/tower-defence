#!/usr/bin/env bash
# Round 51: the opening cutscene's glows and lines before and after, on the same frames: plays the cutscene from Boot, seeks
# each "<shot> <progress>" moment and renders it (r50_still.cs, plain light, 1920x1080) three times: the round-49 material,
# the round-50 look and this round's (r51_space_variant.cs). Usage: r51_cut.sh <output dir> <prefix> "<shot> <progress>" ...
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; prefix=$2; shift 2
mkdir -p "$out"
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-400; }
still() { echo "1" > Tools/PlayTest/r49_light.txt; echo "0" > Tools/PlayTest/r50_orbit.txt; echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; echo "  $1: $(run r50_still.cs)"; }
look() { echo "$1" > Tools/PlayTest/r51_variant.txt; echo "  $(run r51_space_variant.cs)"; }
step 20
echo "  $(run cutscene_start.cs)"
sleep 4
step 10
for moment in "$@"; do
  echo "$moment" > Tools/PlayTest/cut_arg.txt
  echo "== $moment: $(run cutscene_seek.cs)"
  step "3 hold"
  name=$(echo "$moment" | tr ' .' '_p')
  look r51; still "${name}_r51"
  look r50; still "${name}_r50"
  look r49; still "${name}_r49"
  look r51
done
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E '"consoleErrors"|"consoleWarnings"'
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -10
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
