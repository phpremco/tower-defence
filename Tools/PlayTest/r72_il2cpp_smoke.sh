#!/usr/bin/env bash
# Round 72: the IL2CPP smoke build from worker clone wN (the open editor is never touched) and the scripted smoke run.
# 1. Mirrors the project into wN (own product name "Tower defense worker wN", audio off), builds
#    Game.Editor.BuildScript.BuildWindowsIl2CppSmoke (IL2CPP, the TD_SMOKE_TEST define, Builds/WindowsIl2CppSmoke).
# 2. Empties the player's own save folder (LocalLow/DefaultCompany/Tower defense worker wN), so the run starts fresh.
# 3. Runs the player windowed 1280x720 with -autotest (Game.UI.SmokeTestDriver: intro skip, menu, options, credits, map,
#    level 1 on High, a tower, 30 s frame times, pause, quit; the same on Low; Quit) and waits for it to quit by itself.
# 4. Runs it again with -autotest-crashprobe (one deliberate exception, crash.log must hold it).
# 5. Prints the [AutoTest] lines, the exception count of both player logs and the crash.log contents.
# Usage: bash Tools/PlayTest/r72_il2cpp_smoke.sh <N> [skip-build]
set -u
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
n="$1"; skip_build="${2:-}"
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
dir="$(cygpath -u "$ROOT\w$n")"
PROJECT="$(pwd -W 2>/dev/null || pwd)"
data="$(cygpath -u "$USERPROFILE")/AppData/LocalLow/DefaultCompany/Tower defense worker w$n"
case "$data" in *"worker w"*) ;; *) echo "refusing: $data is not a worker folder"; exit 1;; esac
if [ -z "$skip_build" ]; then
  # A running player locks its files and the build fails on copying them (seen in round 72).
  tasklist /FI "IMAGENAME eq TowerDefense.exe" | grep -q TowerDefense && { echo "== a player is alive: killing it before the build"; taskkill /F /T /IM TowerDefense.exe; sleep 2; }
  for d in Assets Packages ProjectSettings Tools; do
    robocopy "$PROJECT\\$d" "$ROOT\\w$n\\$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
  done
  sed -i "s/^  productName: Tower defense\$/  productName: Tower defense worker w$n/" "$dir/ProjectSettings/ProjectSettings.asset"
  sed -i "s/^  m_DisableAudio: 0\$/  m_DisableAudio: 1/" "$dir/ProjectSettings/AudioManager.asset"
  rm -f "$dir"/Assets/InitTestScene*.unity*
  started=$(date +%s)
  echo "== w$n: IL2CPP smoke build ($(date +%H:%M:%S))"
  "$UNITY" -batchmode -quit -nographics -projectPath "$ROOT\w$n" -executeMethod Game.Editor.BuildScript.BuildWindowsIl2CppSmoke \
    -logFile "$ROOT\w$n\Logs\build_il2cpp_smoke.log" > /dev/null 2>&1
  code=$?
  ended=$(date +%s)
  echo "== build exit $code in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s"
  grep -E "\[Build\]|error CS|Error building|IL2CPP error|il2cpp.*error" "$dir/Logs/build_il2cpp_smoke.log" | head -20
fi
exe="$dir/Builds/WindowsIl2CppSmoke/TowerDefense.exe"
[ -f "$exe" ] || { echo "no exe"; exit 1; }
echo "== size: $(du -sh "$dir/Builds/WindowsIl2CppSmoke" | cut -f1)"

run_player() { # $1 = log name, $2 = argument, $3 = timeout seconds
  local log="$ROOT\w$n\Logs\\$1"
  rm -f "$(cygpath -u "$log")"
  "$exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 "$2" -logFile "$log" &
  local pid=$! waited=0
  while kill -0 "$pid" 2>/dev/null && [ "$waited" -lt "$3" ]; do sleep 5; waited=$((waited + 5)); done
  # MSYS_NO_PATHCONV=1 is set, so taskkill takes single-slash switches (the round-71 "//F" form silently failed and
  # left a stuck player on the desktop).
  if kill -0 "$pid" 2>/dev/null; then echo "== $1: still running after $3 s, killed"; taskkill /F /T /IM TowerDefense.exe > /dev/null 2>&1; else echo "== $1: quit by itself after ~$waited s"; fi
  sleep 2
  tasklist /FI "IMAGENAME eq TowerDefense.exe" | grep -q TowerDefense && { echo "== a player is still alive: killing"; taskkill /F /T /IM TowerDefense.exe; }
}

rm -rf "$data"
echo "== fresh save folder: $data"
tasklist /FI "IMAGENAME eq TowerDefense.exe" | grep -q TowerDefense && { echo "== a player from an earlier run is alive: killing it first"; taskkill /F /T /IM TowerDefense.exe; sleep 2; }
run_player player_smoke.log -autotest 420
plog="$dir/Logs/player_smoke.log"
echo "== [AutoTest] lines"
grep -a "\[AutoTest\]" "$plog" | sed 's/^.*\[AutoTest\] //'
echo "== exceptions in the player log: $(grep -a -c -E "Exception" "$plog")"
grep -a -n -E "Exception|^Error|error" "$plog" | grep -v -i "d3d11\|shader warm" | head -20
echo "== crash.log after the clean run: $([ -f "$data/crash.log" ] && echo present || echo absent)"
cp "$plog" "$dir/Logs/player_smoke_clean.log"
[ -f "$data/autotest.txt" ] && cp "$data/autotest.txt" "$dir/Logs/autotest_clean.txt"

run_player player_crashprobe.log -autotest-crashprobe 180
echo "== crash probe [AutoTest] lines"
grep -a "\[AutoTest\]" "$dir/Logs/player_crashprobe.log" | sed 's/^.*\[AutoTest\] //'
echo "== crash.log"
cat "$data/crash.log" 2>/dev/null | head -20
echo "== save folder"
ls -la "$data" "$data/Saves" 2>/dev/null
