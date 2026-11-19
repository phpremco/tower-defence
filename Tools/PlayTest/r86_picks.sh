#!/usr/bin/env bash
# Round 86: the skilled rung's picks, searched anew for every cell and played on today's data. A BOT batch: run it on
# data that stands still, never together with a data change (Docs/COMMANDS.md, "Round 86").
#   1. every cell's present pick is kept aside (Tools/PlayTest/Plans_old_<name>/<level>_<difficulty>.json);
#   2. PlanSearchTests on six clones side by side (one level each), the project's Plans replaced by the clones';
#   3. the old pick joins each cell's candidates as its next strategy number (a fresh search can be worse);
#   4. every candidate is played as the skilled rung plays one, three seeds, on four multiples per difficulty
#      (Easy x1 / 1.4 / 1.57 / 1.76, Medium x1 / 1.12 / 1.25 / 1.4, Hard and Psycho x0.9 / 1 / 1.12 / 1.25).
# Then: python -I Tools/PlayTest/skilled_picks.py Tools/PlayTest/skilled_picks_<name>_easy_records.txt \
#         Tools/PlayTest/skilled_picks_<name>_medium_records.txt Tools/PlayTest/skilled_picks_<name>_records.txt [--write]
#   bash Tools/PlayTest/r86_picks.sh <name>        (in the background; DONE in Tools/PlayTest/<name>_picks.log)
set -uo pipefail
cd "$(dirname "$0")/../.."
name="$1"
log="Tools/PlayTest/${name}_picks.log"
: > "$log"
plans=Tools/PlayTest/Plans
old="Tools/PlayTest/Plans_old_$name"
keys=(C1_01 C1_02 C1_03 C1_04 C1_05 C1_06)

mkdir -p "$old"
grep '^pick|' Tools/PlayTest/skilled_picks.txt | while IFS='|' read -r _ level d strategy _; do
  [ -f "$plans/$strategy.json" ] && cp "$plans/$strategy.json" "$old/${level}_$d.json"
done
echo "== old picks kept: $(ls "$old" | wc -l) ($(date +%H:%M))" >> "$log"

for n in 0 1 2 3 4 5; do bash Tools/PlayTest/r34b_editmode_worker.sh $((n + 1)) PlanSearchTests "levels=$n" >> "$log" 2>&1 & done
wait
for n in 0 1 2 3 4 5; do
  key="${keys[$n]}"
  clone="../w$((n + 1))/Tools/PlayTest/Plans"
  if ls "$clone"/Level_"$key"_*.json > /dev/null 2>&1; then
    rm -f "$plans"/Level_"$key"_*
    cp "$clone"/Level_"$key"_* "$plans"/
  else
    echo "!! no searched plans for $key in w$((n + 1)): the old ones stay" >> "$log"
  fi
done

for f in "$old"/*.json; do
  cell="$(basename "$f" .json)"
  count=$(ls "$plans"/"${cell}"_strategy*.json 2> /dev/null | wc -l)
  next="${cell}_strategy$((count + 1))"
  sed 's/"Name": *"[^"]*"/"Name": "'"$next"'"/' "$f" > "$plans/$next.json"
  echo "old pick of $cell plays as $next" >> "$log"
done
echo "== candidates: $(ls "$plans"/*_strategy*.json | wc -l) ($(date +%H:%M))" >> "$log"

policies="strategy1,strategy2,strategy3,strategy4,strategy5,strategy6"
PR_WORKERS=6 PR_RECORDS="Tools/PlayTest/skilled_picks_${name}_easy_records.txt" PR_POLICIES="$policies" PR_MULTIPLES="1,1.4,1.57,1.76" bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "0" >> "$log" 2>&1
PR_WORKERS=6 PR_RECORDS="Tools/PlayTest/skilled_picks_${name}_medium_records.txt" PR_POLICIES="$policies" PR_MULTIPLES="1,1.12,1.25,1.4" bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "1" >> "$log" 2>&1
PR_WORKERS=6 PR_RECORDS="Tools/PlayTest/skilled_picks_${name}_records.txt" PR_POLICIES="$policies" PR_MULTIPLES="0.9,1,1.12,1.25" bash Tools/PlayTest/pressure.sh "0,1,2,3,4,5" "2,3" >> "$log" 2>&1
echo "DONE $(date +%H:%M)" >> "$log"
