#!/usr/bin/env bash
# Round 67: recompile (refuses while Play Mode runs), write the round's data (ApplyRound67.cs: combat rules, tower steps
# and roles, tower art) and the role matrix (Tools/PlayTest/r67_role_matrix.txt). Safe to run again.
set -u
cd "$(dirname "$0")/../.." || exit 1
bash Tools/PlayTest/r67_compile.sh || exit 1
unity command console_status --result-only 2>&1 | grep -q '"compilationFailed": *false' || { echo "compile failed"; exit 1; }
unity command run_script --file Tools/PlayTest/Scripts/ApplyRound67.cs --entry ApplyRound67.Run --timeout_ms 300000 --timeout 360 2>&1 | grep -o '"success":[a-z]*' | head -1
unity command run_script --file Tools/PlayTest/Scripts/RoleMatrixReport.cs --entry RoleMatrixReport.Run --timeout_ms 120000 --timeout 180 2>&1 | grep -o '"success":[a-z]*' | head -1
