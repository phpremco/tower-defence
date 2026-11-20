#!/usr/bin/env bash
# Round 87: one measurement of the closed loop, a ladder per difficulty and the four rungs the gates read (the
# reference rungs skilled and sensible, the archetypes laser and cannon; PR_POLICIES overrides, PR_SEEDS and
# PR_WORKERS pass through to pressure.sh). After r85_measure.sh; what changed: the rungs, and Easy on a ladder that
# brackets its target (x1.66) on both sides, so Easy gets a real reading and an upper bound instead of "over x1.86".
# Three pressure batches, one after the other, into three records files of one data state (freeze the folder it runs
# from meanwhile: every batch syncs the clones again):
#   Tools/PlayTest/pressure_<name>_easy_records.txt     Easy on x1.25 / 1.4 / 1.57 / 1.76 / 1.97 / 2.21
#   Tools/PlayTest/pressure_<name>_medium_records.txt   Medium on x0.9 / 1 / 1.12 / 1.25 / 1.4 / 1.57
#   Tools/PlayTest/pressure_<name>_records.txt          Hard and Psycho on the standard ladder (x0.8 / 0.9 / 1 / 1.12 / 1.25)
# and the three names in Tools/PlayTest/closed_loop_records.txt when all 24 cells were played (the acceptance test
# reads them). Run it in the background; it writes DONE to Tools/PlayTest/<name>.log.
#   bash Tools/PlayTest/r87_measure.sh <name> ["0,1,2,3,4,5" levels] ["0,1,2,3" difficulties]
# Then: python -I Tools/PlayTest/closed_loop.py Tools/PlayTest/pressure_<name>_easy_records.txt \
#         Tools/PlayTest/pressure_<name>_medium_records.txt Tools/PlayTest/pressure_<name>_records.txt [--centre] [--write]
#       python -I Tools/PlayTest/boss_share.py <the same three files>
set -uo pipefail
cd "$(dirname "$0")/../.."
name="$1"; levels="${2:-0,1,2,3,4,5}"; diffs="${3:-0,1,2,3}"
export PR_POLICIES="${PR_POLICIES:-skilled,sensible,laser,cannon}"
log="Tools/PlayTest/$name.log"
: > "$log"
has() { case ",$diffs," in *",$1,"*) return 0 ;; *) return 1 ;; esac; }
files=()
if has 0; then
  PR_RECORDS="Tools/PlayTest/pressure_${name}_easy_records.txt" PR_MULTIPLES="1.25,1.4,1.57,1.76,1.97,2.21" bash Tools/PlayTest/pressure.sh "$levels" "0" >> "$log" 2>&1
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
