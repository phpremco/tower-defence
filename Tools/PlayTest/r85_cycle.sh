#!/usr/bin/env bash
# Round 85 helper: in the stage, compile and solve with the stage editor, stop it, then EditMode classes on w6.
#   bash Tools/PlayTest/r85_cycle.sh <log> ["Class1;Class2"]
cd "$(dirname "$0")/../.."
log="Tools/PlayTest/$1"; classes="${2:-Round83Tests;BalanceLedgerTests;WaveGrammarTests;ContinentFinaleTests;Round62Tests}"
{
  bash Tools/PlayTest/stage.sh start
  n=0; until bash Tools/PlayTest/stage.sh cmd editor_status 2>/dev/null | grep -q '"status":"ready"' || [ $n -ge 60 ]; do sleep 5; n=$((n+1)); done
  bash Tools/PlayTest/stage.sh compile | tail -3
  bash Tools/PlayTest/stage.sh run ApplyRound68.cs ApplyRound68.Run 2>&1 | grep -o '"result":"[^}]*' | cut -c1-120
  bash Tools/PlayTest/stage.sh stop; sleep 4
  if [ "$classes" != "none" ]; then bash Tools/PlayTest/r34b_editmode_worker.sh 6 "$classes" | grep -v 'result="Passed"'; fi
  echo DONE
} > "$log" 2>&1
