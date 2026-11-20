#!/usr/bin/env bash
# Round 87: the archetype rungs (the sensible player with a favourite ground gun: laser, cannon) on the ladders of
# r85_measure.sh, as a measurement of its own. Three pressure batches, one after the other, into three records files of
# one data state (freeze the folder it runs from meanwhile: every batch syncs the clones again):
#   Tools/PlayTest/pressure_<name>_easy_records.txt     Easy on x1 / 1.25 / 1.4 / 1.57 / 1.76
#   Tools/PlayTest/pressure_<name>_medium_records.txt   Medium on x0.9 / 1 / 1.12 / 1.25 / 1.4 / 1.57
#   Tools/PlayTest/pressure_<name>_records.txt          Hard and Psycho on the standard ladder
# Unlike r85_measure.sh it never writes closed_loop_records.txt: these rungs are no target of the closed loop.
# Run it in the background; it writes DONE to Tools/PlayTest/<name>.log.
#   PR_POLICIES="laser,cannon" bash Tools/PlayTest/r87_archetypes.sh <name> ["0,1,2,3,4,5" levels] ["0,1,2,3" difficulties]
# Then: python -I Tools/PlayTest/archetype_table.py Tools/PlayTest/pressure_<name>_easy_records.txt \
#         Tools/PlayTest/pressure_<name>_medium_records.txt Tools/PlayTest/pressure_<name>_records.txt \
#         <the records of the sensible and skilled rungs> --rungs laser,cannon,sensible,skilled
set -uo pipefail
cd "$(dirname "$0")/../.."
name="$1"; levels="${2:-0,1,2,3,4,5}"; diffs="${3:-0,1,2,3}"
export PR_POLICIES="${PR_POLICIES:-laser,cannon}"
log="Tools/PlayTest/$name.log"
: > "$log"
has() { case ",$diffs," in *",$1,"*) return 0 ;; *) return 1 ;; esac; }
if has 0; then
  PR_RECORDS="Tools/PlayTest/pressure_${name}_easy_records.txt" PR_MULTIPLES="1,1.25,1.4,1.57,1.76" bash Tools/PlayTest/pressure.sh "$levels" "0" >> "$log" 2>&1
fi
if has 1; then
  PR_RECORDS="Tools/PlayTest/pressure_${name}_medium_records.txt" PR_MULTIPLES="0.9,1,1.12,1.25,1.4,1.57" bash Tools/PlayTest/pressure.sh "$levels" "1" >> "$log" 2>&1
fi
rest=""; has 2 && rest="2"; if has 3; then rest="${rest:+$rest,}3"; fi
if [ -n "$rest" ]; then
  PR_RECORDS="Tools/PlayTest/pressure_${name}_records.txt" bash Tools/PlayTest/pressure.sh "$levels" "$rest" >> "$log" 2>&1
fi
echo "DONE" >> "$log"
