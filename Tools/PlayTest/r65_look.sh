#!/usr/bin/env bash
# Round 65: recompiles, paints Landing Site again (ApplyRound65 with only=C1_01) and takes the plateau stills, to judge a
# change to the painter or the landscape. Usage: OUT=<dir> bash Tools/PlayTest/r65_look.sh <prefix>
cd "$(dirname "$0")/../.." || exit 1
bash Tools/PlayTest/r64_compile.sh 2>&1 | tail -3
echo "only=C1_01" > Tools/PlayTest/r65_apply_arg.txt
unity command run_script --file Tools/PlayTest/Scripts/ApplyRound65.cs --entry ApplyRound65.Run --timeout_ms 900000 --timeout 960 --result-only 2>&1 | grep -c '"success": true'
rm -f Tools/PlayTest/r65_apply_arg.txt
bash Tools/PlayTest/r65_shots.sh "$1" 0 1 "act:tower Laser 13 2 2" "act:tower Laser 12 1 0" "act:tower Cannon 24 2" "act:tower PulseRifle 25 2" "step:30" "shot:plateau_a|cell:12.5:2 2.2 clean" "shot:plateau_b|cell:24.5:1.5 2.2 clean" "view:plateau_b" "shot:play|cell:18:4 1 clean" "view:play" 2>&1 | tail -3
