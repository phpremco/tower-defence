# QA round 32 helpers: source from the project root. SHOTS is the folder for stills. Play Mode must run UNPAUSED for
# the virtual gamepad (pad) and simulated keys (key): EditorApplication.Step runs no input update.
SAVE="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
LIVE="${LIVE:-C:/Users/remco/AppData/Local/Temp/claude/b18d9d7fa25a4d30/2aa46f30-11b3-418c-a974-fe154b605af3/scratchpad/qa/progress_live.json}"
SHOTS="${SHOTS:-C:/Users/remco/Tower defense/ArtSource/Previews/QA}"
ev() { unity command eval_file --file "Tools/PlayTest/$1" --result-only 2>&1 | sed -n 's/.*"result": "\(.*\)".*/\1/p; s/.*"message": "\(.*\)".*/ERR \1/p' | sed 's/\\\\n/\n/g; s/\\n/\n/g' | cut -c1-500; }
pad() { echo "$*" > Tools/PlayTest/qa/pad_arg.txt; ev qa/pad.cs >/dev/null; sleep 0.4; echo "[pad $*] $(ev qa/focus.cs)"; }
key() { echo "$*" > Tools/PlayTest/qa/kbd_arg.txt; ev qa/kbd.cs >/dev/null; sleep 0.4; echo "[key $*] $(ev qa/focus.cs)"; }
probe() { ev qa/ui_probe.cs; }
still() { echo "$SHOTS/$1.png|${2:-1920}|${3:-1080}" > Tools/PlayTest/qa/still_arg.txt; ev qa/still.cs; }
stills() { still "$1_1080" >/dev/null; still "$1_720" 1280 720 >/dev/null; echo "stills $1"; }
steps() { echo "$1" > Tools/PlayTest/frames_arg.txt; ev step_frames.cs >/dev/null; }
click() { echo "$1" > Tools/PlayTest/button_arg.txt; ev click_button.cs | cut -c1-120; }
stopped() { unity command editor_status --result-only 2>&1 | grep -q '"playMode": *"stopped"'; }
saveok() { md5sum < "$SAVE" | cut -c1-32; }
restore() { cp "$LIVE" "$SAVE"; echo "restored: $(saveok)"; }
errs() { unity command console_status --result-only 2>&1 | tr -d '\n ' | grep -o '"counts":{[^}]*}'; echo; unity command console --level error --result-only 2>&1 | grep -o '"message": "[^"]*' | sort | uniq -c | sort -rn | head -6; }
# The Game view (1920x1080) as the player sees it, overlay UI and post included, scaled to 1280x720 by the CLI.
shot() { unity command capture_game_view --save_path "Temp/qa_$1.png" --result-only >/dev/null 2>&1; mv -f "Assets/Temp/qa_$1.png" "$SHOTS/$1.png" 2>/dev/null && rm -f "Assets/Temp/qa_$1.png.meta" && echo "shot $1" || echo "shot $1 FAILED"; }
