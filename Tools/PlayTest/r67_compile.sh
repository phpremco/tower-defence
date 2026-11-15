#!/usr/bin/env bash
# Round 67 (as round 66): recompiles the editor's scripts and reports errors and warnings; like r64_compile.sh, but it also takes
# "up_to_date" for done (r64's loop waited its full three minutes when nothing had changed). Refuses while Play Mode runs.
set -u
cd "$(dirname "$0")/../.." || exit 1
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command recompile --result-only >/dev/null 2>&1
for i in $(seq 1 50); do
  sleep 2
  state=$(unity command recompile_status --result-only 2>&1)
  if echo "$state" | grep -q '"status": *"\(completed\|up_to_date\)"'; then
    ready=$(unity command editor_status --result-only 2>&1)
    if echo "$ready" | grep -q '"compiling": *false' && echo "$ready" | grep -q '"domainReloadInProgress": *false'; then break; fi
  fi
done
echo "$state" | tr -d '\n' | cut -c1-3000; echo
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v '"Log"' | cut -c1-400 | sort | uniq -c | sort -rn | head -30
