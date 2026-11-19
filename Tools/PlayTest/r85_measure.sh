#!/usr/bin/env bash
# Round 85: one measurement of the closed loop with a ladder per difficulty, since the Easy and Medium targets
# (x1.55 and x1.32) lie above the standard ladder. Three pressure batches, one after the other, into three records
# files of one data state:
#   Tools/PlayTest/pressure_<name>_easy_records.txt     Easy on x1 / 1.25 / 1.4 / 1.57 / 1.76
#   Tools/PlayTest/pressure_<name>_medium_records.txt   Medium on x0.9 / 1 / 1.12 / 1.25 / 1.4 / 1.57
#   Tools/PlayTest/pressure_<name>_records.txt          Hard and Psycho on the standard ladder
# and the three names in Tools/PlayTest/closed_loop_records.txt when it ends (the acceptance test reads them).
# 756 runs, about 70 minutes on five clones. Run it in the background; it writes DONE to Tools/PlayTest/<name>.log.
#   bash Tools/PlayTest/r85_measure.sh <name> ["0,1,2,3,4,5" levels] ["0,1,2,3" difficulties]
# Then: python Tools/PlayTest/closed_loop.py Tools/PlayTest/pressure_<name>_easy_records.txt \
#         Tools/PlayTest/pressure_<name>_medium_records.txt Tools/PlayTest/pressure_<name>_records.txt [--centre] [--write]
set -uo pipefail
cd "$(dirname "$0")/../.."
name="$1"; levels="${2:-0,1,2,3,4,5}"; diffs="${3:-0,1,2,3}"
log="Tools/PlayTest/$name.log"
: > "$log"
has() { case ",$diffs," in *",$1,"*) return 0 ;; *) return 1 ;; esac; }
files=()
if has 0; then
  PR_RECORDS="Tools/PlayTest/pressure_${name}_easy_records.txt" PR_MULTIPLES="1,1.25,1.4,1.57,1.76" bash Tools/PlayTest/pressure.sh "$levels" "0" >> "$log" 2>&1
  files+=("Tools/PlayTest/pressure_${name}_easy_records.txt")
fi
if has 1; then
  PR_RECORDS="Tools/PlayTest/pressure_${name}_medium_records.txt" PR_MULTIPLES="0.9,1,1.12,1.25,1.4,1.57" bash Tools/PlayTest/pressure.sh "$levels" "1" >> "$log" 2>&1
  files+=("Tools/PlayTest/pressure_${name}_medium_records.txt")
fi
rest=""; has 2 && rest="2"; if has 3; then rest="${rest:+$rest,}3"; fi
if [ -n "$rest" ]; then
  PR_RECORDS="Tools/PlayTest/pressure_${name}_records.txt" bash Tools/PlayTest/pressure.sh "$levels" "$rest" >> "$log" 2>&1
  files+=("Tools/PlayTest/pressure_${name}_records.txt")
fi
if [ "$levels" = "0,1,2,3,4,5" ] && [ "$diffs" = "0,1,2,3" ]; then
  { echo "# The measurement the closed-loop acceptance reads (ClosedLoopAcceptanceTests); one records file per line."; printf '%s\n' "${files[@]}"; } > Tools/PlayTest/closed_loop_records.txt
fi
echo "DONE" >> "$log"
