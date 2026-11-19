#!/usr/bin/env bash
# Round 85: every Unity process of the worker clones and the stage runs at BelowNormal priority, so Remco can use the
# PC (a film, the open editor) while a batch runs. Matches Unity.exe whose command line names TowerDefenseWorkers and
# their child processes (asset import workers inherit the class from a BelowNormal parent; the ones started before it
# are caught by the repeat). The open editor ("Tower defense" project) is never touched.
#   bash Tools/PlayTest/lower_priority.sh [seconds to keep re-applying, default 90]   # call it right after a launch
set -uo pipefail
for_seconds="${1:-90}"
here="$(cd "$(dirname "$0")" && pwd -W 2>/dev/null || pwd)"
apply() { powershell -NoProfile -ExecutionPolicy Bypass -File "$here/lower_priority.ps1" > /dev/null 2>&1; }
end=$(( $(date +%s) + for_seconds ))
apply
while [ "$(date +%s)" -lt "$end" ]; do sleep 10; apply; done
