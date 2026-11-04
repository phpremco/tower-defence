#!/usr/bin/env bash
# Runs the PlayMode test classes one at a time (async) and prints each summary. Usage: fb2_playmode.sh Class1 Class2 ...
set -u
cd "$(dirname "$0")/../.." || exit 1
for cls in "$@"; do
  status=$(unity command editor_status --result-only 2>&1)
  if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
  unity command open_scene --path Assets/_Project/Scenes/Boot.unity --result-only >/dev/null 2>&1
  unity command run_tests --mode PlayMode --filter "$cls" --async_tests true --result-only >/dev/null 2>&1
  sleep 10
  for i in $(seq 1 200); do
    s=$(unity command test_status --result-only 2>&1)
    echo "$s" | grep -q '"status": *"running"' || break
    sleep 6
  done
  echo "$cls: $(echo "$s" | tr -d '\n ' | grep -o '"summary":{[^}]*}')"
  echo "$s" | grep -o '"Message": *"[^"]*' | grep -v ': null' | head -3
done
