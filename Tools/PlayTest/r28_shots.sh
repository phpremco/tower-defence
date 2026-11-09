#!/usr/bin/env bash
# Round 28: Chakra Petch in the main menu, the HUD and the codex (stills plus the r28_texts.cs fit sweep), Spencer's
# pistol at game zoom and his jet flames in flight, and the Skitter/Shardback walk (r28_gait.cs foot slide) on campaign
# node $2 (default 1), Medium, waves off. Stills go to $1 (16:9 via render_still.cs; the Game view is left alone).
# Keeps the save exactly as it was. Usage: r28_shots.sh <output dir> [node] [ui|hero|gait|all]
set -u
cd "$(dirname "$0")/../.." || exit 1
hold=
out=$1; node=${2:-1}; part=${3:-all}; prefix=r28
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="$out/${prefix}_progress_before.json"
had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; md5sum "$save"; else echo "no progress.json before"; fi
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
rm -f Tools/PlayTest/r28_gait.txt Tools/PlayTest/r28_texts.txt
unity command clear_console --result-only >/dev/null 2>&1
unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
unity command editor_play --result-only 2>&1 | grep -o "Entered[^\"]*"
sleep 8
step() { echo "$1${hold:+ hold}" > Tools/PlayTest/frames_arg.txt; unity command eval_file --file Tools/PlayTest/step_frames.cs --result-only >/dev/null 2>&1; }
run() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | grep -o '"result": "[^"]*\|"message": "[^"]*' | cut -c1-900; }
hero() { echo "$*" > Tools/PlayTest/r23d_hero.txt; run r23d_hero.cs; }
view() { echo "$*" > Tools/PlayTest/r28_view.txt; run r28_view.cs; }
still() { echo "$(cygpath -w "$out/${prefix}_$1.png")" > Tools/PlayTest/look_arg.txt; run render_still.cs >/dev/null; }
pistol() { echo "$1" > Tools/PlayTest/r28_pistol.txt; run r28_pistol.cs; }
for i in 1 2 3; do step 60; sleep 1; done
if [ "$part" != hero ]; then echo "=== main menu"; still main_menu; echo "texts: $(run r28_texts.cs)"; fi
echo "node $node" > Tools/PlayTest/r15b_load.txt; run r15b_load.cs; sleep 8
step 30
echo "waves: $(run no_waves.cs)"
echo "deploy: $(run deploy.cs)"
for i in $(seq 1 12); do step 60; sleep 1; done
echo "dialogue: $(run r16_dialogue_skip.cs)"
echo "revive: $(run hero_revive.cs)"
step 150
if [ "$part" != hero ]; then
echo "=== HUD at game zoom: $(view report)"
still hud
echo "texts: $(run r28_texts.cs)"
echo "=== codex"
echo enemies > Tools/PlayTest/menu_arg.txt; echo 0 > Tools/PlayTest/card_arg.txt
run codex_open.cs; step 20; still codex_enemies
echo "texts: $(run r28_texts.cs)"
echo recipes > Tools/PlayTest/menu_arg.txt; run codex_open.cs; step 20; still codex_recipes
echo "texts: $(run r28_texts.cs)"
run codex_close.cs; step 10
fi
if [ "$part" != ui ]; then
if [ "$part" != gait ]; then
echo "=== pistol"
hero fps 30
hold=1
hero draw ranged; step 30
hero view 7; step 10; hero face 90; step 1; still pistol_game; echo "pistol: $(pistol report)"
pistol off; hero face 90; step 1; still pistol_game_before; pistol on
hero view 3.5; step 5; hero face 90; step 1; still pistol_mid; echo "pistol: $(pistol report)"
pistol off; hero face 90; step 1; still pistol_mid_before; pistol on
hero view 1.6 1.1; step 5; hero face 90; step 1; still pistol_close
hero draw none; step 30
echo "=== jet flight"
hero view 7 0
hero move 2
for i in $(seq 1 40); do
  step 3
  s=$(hero state)
  case "$s" in *"view flight Cruise"*) if [ ! -f "$out/${prefix}_jet_game.png" ]; then still jet_game; hero view 3 1; step 1; still jet_mid; hero view 1.6 1; step 1; still jet_close; hero view 7 0; fi;; esac
  case "$s" in *"view flight None"*"walking False"*) break;; esac
done
fi
echo "=== gait"
hold=1
hero fps 60
for e in Skitter Shardback; do
  echo "$e" > Tools/PlayTest/enemy_arg.txt
  echo "spawn: $(run spawn_enemy.cs)"
  step 10
  view "$e" 2.2; step 5
  for k in 1 2 3; do echo "gait: $(run r28_gait.cs)"; done
  still "walk_$e"
done
hold=
hero fps 0
step 1
fi
echo "=== CONSOLE"
unity command console_status --result-only 2>&1 | grep -A3 '"counts"' | tr -d '\n '; echo
unity command console --level error --result-only 2>&1 | grep -o '"message": "[^"]*' | sort | uniq -c | sort -rn | head -8
unity command console --level warning --result-only 2>&1 | grep -o '"message": "[^"]*' | grep -v "allocation" | sort | uniq -c | sort -rn | head -8
unity command editor_stop --result-only >/dev/null 2>&1
sleep 2
if [ "$had" = 1 ]; then cp "$backup" "$save"; md5sum "$save"; else rm -f "$save"; echo "progress.json removed: $([ -f "$save" ] && echo no || echo yes)"; fi
git checkout -q -- Tools/PlayTest/frames_arg.txt Tools/PlayTest/look_arg.txt Tools/PlayTest/r23d_hero.txt Tools/PlayTest/r15b_load.txt Tools/PlayTest/menu_arg.txt Tools/PlayTest/card_arg.txt Tools/PlayTest/enemy_arg.txt
