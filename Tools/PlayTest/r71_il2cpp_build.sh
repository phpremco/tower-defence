#!/usr/bin/env bash
# Round 71: the IL2CPP Windows build from worker clone wN (the open editor is never touched), then a smoke run of the
# player: boot to the main menu, wait, close, and search the player log for AOT and stripping errors.
# The clone keeps its own product name ("Tower defense worker wN"), so the smoke run writes its own save folder and never
# Remco's; its audio is off (clones are silent). Usage: bash Tools/PlayTest/r71_il2cpp_build.sh <N> [smoke-seconds]
set -u
cd "$(dirname "$0")/../.."
export MSYS_NO_PATHCONV=1
n="$1"; wait_s="${2:-40}"
ROOT="${TD_WORKERS_ROOT:-C:\Users\remco\TowerDefenseWorkers}"
UNITY="${TD_UNITY:-C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe}"
dir="$(cygpath -u "$ROOT\w$n")"
PROJECT="$(pwd -W 2>/dev/null || pwd)"
# The same sync as workers.sh sync_worker: sources mirrored, own product name, audio off.
for d in Assets Packages ProjectSettings Tools; do
  robocopy "$PROJECT\\$d" "$ROOT\\w$n\\$d" /MIR /MT:16 /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /NS /NC > /dev/null || [ $? -le 7 ]
done
sed -i "s/^  productName: Tower defense\$/  productName: Tower defense worker w$n/" "$dir/ProjectSettings/ProjectSettings.asset"
sed -i "s/^  m_DisableAudio: 0\$/  m_DisableAudio: 1/" "$dir/ProjectSettings/AudioManager.asset"
rm -f "$dir"/Assets/InitTestScene*.unity*
started=$(date +%s)
echo "== w$n: IL2CPP build ($(date +%H:%M:%S))"
"$UNITY" -batchmode -quit -nographics -projectPath "$ROOT\w$n" -executeMethod Game.Editor.BuildScript.BuildWindowsIl2Cpp \
  -logFile "$ROOT\w$n\Logs\build_il2cpp.log" > /dev/null 2>&1
code=$?
ended=$(date +%s)
echo "== build exit $code in $(( (ended - started) / 60 )) min $(( (ended - started) % 60 )) s"
grep -E "Build (succeeded|failed)|error CS|Error building|IL2CPP error|il2cpp.*error" "$dir/Logs/build_il2cpp.log" | head -20
exe="$dir/Builds/WindowsIl2Cpp/TowerDefense.exe"
[ -f "$exe" ] || { echo "no exe"; exit 1; }
echo "== size: $(du -sh "$dir/Builds/WindowsIl2Cpp" | cut -f1), exe folder files: $(find "$dir/Builds/WindowsIl2Cpp" -type f | wc -l)"
log="$ROOT\w$n\Logs\player_il2cpp.log"
rm -f "$(cygpath -u "$log")"
"$exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "$log" &
pid=$!
sleep "$wait_s"
taskkill //F //IM TowerDefense.exe > /dev/null 2>&1 || kill "$pid" 2>/dev/null
sleep 2
plog="$(cygpath -u "$log")"
echo "== player log: $(wc -l < "$plog") lines"
grep -n -E "MissingMethodException|TypeLoadException|ExecutionEngineException|NullReferenceException|MissingReferenceException|Exception|Error|error" "$plog" | grep -v -i "d3d11\|shader warm" | head -30
grep -n -E "Scene|main menu|MainMenu|Loaded scene" "$plog" | head -10
