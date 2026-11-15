#!/usr/bin/env bash
# Round 64: recompiles the editor's scripts and reports errors and warnings. Refuses while Play Mode runs (Remco may be
# playing: a domain reload mid-play breaks his session). Usage: bash Tools/PlayTest/r64_compile.sh
set -u
cd "$(dirname "$0")/../.." || exit 1
status=$(unity command editor_status --result-only 2>&1)
if ! echo "$status" | grep -q '"playMode": *"stopped"'; then echo "NOT STOPPED - abort"; exit 1; fi
unity command clear_console --result-only >/dev/null 2>&1
unity command recompile --result-only >/dev/null 2>&1
for i in $(seq 1 90); do
  sleep 2
  state=$(unity command recompile_status --result-only 2>&1)
  if echo "$state" | grep -q '"status": *"completed"'; then
    # A recompile right after an edit can report the previous run: wait until the editor is ready again.
    ready=$(unity command editor_status --result-only 2>&1)
    if echo "$ready" | grep -q '"compiling": *false' && echo "$ready" | grep -q '"domainReloadInProgress": *false'; then break; fi
  fi
done
echo "$state" | tr -d '\n' | cut -c1-3000; echo
unity command console_status --result-only 2>&1 | grep -E "consoleErrors|consoleWarnings"
unity command console --result-only 2>&1 | grep -o '"logType": "[A-Za-z]*", *"message": "[^"]*' | grep -v '"Log"' | cut -c1-400 | sort | uniq -c | sort -rn | head -30
