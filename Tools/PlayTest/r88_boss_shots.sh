#!/usr/bin/env bash
# Round 88a: every level boss on its own level at the play zoom, as the player sees it (stage only, no save handling).
# Usage (from the stage): OUT=<dir> bash Tools/PlayTest/r88_boss_shots.sh <prefix> [steps before the still, default 300]
set -u
cd "$(dirname "$0")/../.." || exit 1
prefix=$1; steps=${2:-300}
shoot() {
  local node=$1; shift
  local actions=("eval:no_waves.cs" "arg:zoom_size.txt=1")
  for boss in "$@"; do
    actions+=("arg:enemy_arg.txt=$boss" "eval:spawn_enemy.cs" "step:$steps" "eval:r88_focus.cs" "step:2" "shot:${boss}|keep 1 clean" "eval:kill_all.cs" "step:20")
  done
  bash Tools/PlayTest/r88_shots.sh "${prefix}_n${node}" "$node" 1 "${actions[@]}" 2>&1 | grep "wrote\|spawned\|console\|Error\|Exception"
}
shoot 0 BroodMother
shoot 1 BroodMother CoreWisp
shoot 2 BroodMother CoreWisp
shoot 3 BroodMother CoreWisp StormMatriarch
shoot 4 BroodMother CoreWisp StormMatriarch
shoot 5 CoreWisp PackAlpha StormMatriarch
