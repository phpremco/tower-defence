#!/usr/bin/env bash
# Round 23c: the result screen's text. Loads campaign node 1 on Medium with the waves off, shows the result screen for
# the mode in $2 ("earned", "unlocks" or "lost"; r23c_result.cs), measures every text on the overlays (r23c_texts.cs)
# and grabs the Game view as Remco sees it plus a 1920x1080 still. Keeps the save exactly as it was.
# With "sweep" as $4 it first measures the pause menu's quit question and the codex the same way.
# Usage: r23c_result.sh <output dir> <mode> [prefix] [sweep]
set -u
cd "$(dirname "$0")/../.." || exit 1
out=$1; mode=$2; prefix=${3:-r23c_result}
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-3000; }
step 60
echo "node 1" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 12); do step 60; sleep 1; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
echo "waves: $(run no_waves.cs)"
shot() { unity command capture_game_view --save_path "Temp/${prefix}_$1.png" --result-only >/dev/null 2>&1; cp "Assets/Temp/${prefix}_$1.png" "$out/"; }
if [ "${4:-}" = sweep ]; then
  echo confirm > Tools/PlayTest/r23c_pause.txt; echo "pause: $(run r23c_pause.cs)"; step 10
  echo "pause texts: $(run r23c_texts.cs)"; shot pause_confirm
  echo close > Tools/PlayTest/r23c_pause.txt; run r23c_pause.cs; step 10
  echo enemies > Tools/PlayTest/menu_arg.txt; echo 0 > Tools/PlayTest/card_arg.txt; echo "codex: $(run codex_open.cs)"; step 10
  echo "codex texts: $(run r23c_texts.cs)"; shot codex
  run codex_close.cs; step 10
fi
step 10
echo "$mode" > Tools/PlayTest/r23c_result.txt; run r23c_result.cs
for i in 1 2 3 4 5; do step 30; sleep 1; done
echo "texts: $(run r23c_texts.cs)"
unity command capture_game_view --save_path "Temp/${prefix}_${mode}_gameview.png" --result-only >/dev/null 2>&1; cp "Assets/Temp/${prefix}_${mode}_gameview.png" "$out/"
echo "$(cygpath -w "$out/${prefix}_${mode}.png")" > Tools/PlayTest/look_arg.txt; run render_still.cs
echo "=== CONSOLE"
unity command console --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 2
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
