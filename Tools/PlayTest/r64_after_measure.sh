#!/usr/bin/env bash
# Round 64: waits for the running yardstick (Logs/r64/measure1.log) and then runs the rest of the chain, detached from the
# shell that started it. Usage: (bash Tools/PlayTest/r64_after_measure.sh > /dev/null 2>&1 &)
cd "$(dirname "$0")/../.." || exit 1
until grep -q "== workers done" Logs/r64/measure1.log; do sleep 15; done
bash Tools/PlayTest/r64_chain.sh fit apply search replay accept > Logs/r64/chain.log 2>&1
echo "== chain exit $? ($(date +%H:%M:%S))" >> Logs/r64/chain.log
