#!/usr/bin/env bash
# Runs PlayMode test classes one at a time (only with Play Mode stopped), waits for each, prints its summary and any
# failure, and restores the save from $SAVE_BACKUP after each class when set. Usage: imp2_tests.sh <Class> [Class ...]
set -u
cd "$(dirname "$0")/../.." || exit 1
save="$USERPROFILE/AppData/LocalLow/DefaultCompany/Tower defense/Saves/progress.json"
for cls in "$@"; do
  status=$(unity command editor_status --result-only 2>&1)
  if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort before $cls"; exit 1; fi
  unity command run_tests --mode PlayMode --filter "$cls" --async_tests true --result-only >/dev/null 2>&1
  sleep 15
  for i in $(seq 1 720); do
    s=$(unity command test_status --result-only 2>&1)
    echo "$s" | grep -q '"status": "completed"' && break
    sleep 10
  done
  echo "== $cls: $(echo "$s" | tr -d '\n ' | grep -o '"summary":{[^}]*}')"
  echo "$s" | grep -B2 -A3 '"Status": "Failed"' | grep '"FullName"\|"Message"' | cut -c1-400
  echo "$s" | grep -o '\[Perf\][^"]*' | sed 's/\\n/\n/g'
  if [ -n "${SAVE_BACKUP:-}" ]; then cp "$SAVE_BACKUP" "$save"; fi
  sleep 5
done
echo "=== TESTS END"
