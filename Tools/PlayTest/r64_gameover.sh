#!/usr/bin/env bash
# Round 64: the field freezes behind the result panel. Plays from Boot, loads campaign node <node> on difficulty <d>, deploys,
# runs the level at speed until creatures are on the road, ends it ("lose": the base falls; "win": the level is won), waits
# for the panel and probes the clock and everything that moves twice, a few real seconds apart (r64_gameover.cs); stills at
# 1920x1080 and 1280x720 with the interface (r64_shot.cs). Save copied aside first, restored and checked at the end.
# Usage: OUT=<dir> bash Tools/PlayTest/r64_gameover.sh <node> <difficulty> <lose|win> <prefix>
set -u
cd "$(dirname "$0")/../.." || exit 1
node=$1; diff=$2; how=$3; prefix=$4
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
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-900; }
over() { echo "$*" > Tools/PlayTest/r64_gameover_arg.txt; run r64_gameover.cs; }
shot() {
  echo "1920 1080 $(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/r64_shot_arg.txt; echo "  $1: $(run r64_shot.cs)"
  echo "1280 720 $(cygpath -w "$out/${prefix}_$1_720.png")" > Tools/PlayTest/r64_shot_arg.txt; run r64_shot.cs > /dev/null
}
echo "$node $diff" > Tools/PlayTest/r37_load_arg.txt; run r37_load.cs; sleep 6
echo "deploy: $(run deploy.cs)"; sleep 4
for i in 1 2 3 4; do run r16_dialogue_skip.cs > /dev/null; sleep 2; done
echo "$(over fast 4)"; sleep "${RUN:-25}"
for i in 1 2; do run r16_dialogue_skip.cs > /dev/null; sleep 1; done
echo "$(over fast 1)"; sleep 1
echo "running: $(over probe)"
shot 0_running
echo "end: $(over "$how")"
sleep 1.5; echo "1.5 s after: $(over probe)"
sleep 4; echo "5.5 s after: $(over probe)"
shot 1_panel
sleep 3; echo "8.5 s after: $(over probe)"
shot 2_later
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep '"message"' | grep -v "allocation" | cut -c1-300 | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 3
cp "$backup" "$save"; got=$(md5sum "$save" | cut -c1-32); want=$(cat Tools/PlayTest/r64_save_md5.txt | tr -d '[:space:]')
echo "save after restore $got"; [ "$got" = "$want" ] && echo "save md5 verified" || echo "SAVE MD5 MISMATCH want $want"
