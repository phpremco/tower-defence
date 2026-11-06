#!/usr/bin/env bash
# Round 16: runs PlayMode test classes one at a time (async, polling test_status), keeping the save exactly as it was
# before each run. Usage: r16_playmode.sh <Class> [<Class> ...]
set -u
cd "$(dirname "$0")/../.." || exit 1
save="$LOCALAPPDATA/../LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
backup="Temp/r16_progress_before.json"
for cls in "$@"; do
  status=$(unity command editor_status --result-only 2>&1)
  if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort before $cls"; exit 1; fi
  had=0; if [ -f "$save" ]; then had=1; cp "$save" "$backup"; fi
  unity command clear_console --result-only >/dev/null 2>&1
  unity command run_tests --mode PlayMode --async_tests true --filter "$cls" --result-only >/dev/null 2>&1
  for i in $(seq 1 180); do
    sleep 10
    s=$(unity command test_status --result-only 2>&1)
    if echo "$s" | grep -q '"status": "completed"' && echo "$s" | grep -q "$cls"; then break; fi
  done
  echo "== $cls: $(echo "$s" | tr -d '\n ' | grep -o '"summary":{[^}]*}')"
  echo "$s" | grep -B1 -A2 '"Status": "Failed"' | grep -o '"FullName": "[^"]*\|"Message": "[^"]\{0,300\}'
  unity command console --result-only 2>&1 | grep -o '"type": "\(error\|warning\)"' | sort | uniq -c
  sleep 3
  if [ "$had" = 1 ]; then cp "$backup" "$save"; else rm -f "$save"; fi
  echo "save restored (existed before: $had)"
done
