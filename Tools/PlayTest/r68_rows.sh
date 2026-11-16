#!/usr/bin/env bash
# Round 68: one line per cell from a records file (default calibration_records.txt): every strategy with its wins over the
# seeds and the earliest wave a lost run fell at. No editor needed.
#   bash Tools/PlayTest/r68_rows.sh [records file] [strategy prefix, default "mono "]
cd "$(dirname "$0")/../.." || exit 1
records="${1:-Tools/PlayTest/calibration_records.txt}"
prefix="${2:-mono }"
awk -F'|' -v prefix="$prefix" '
  $1 == "run" && index($4, prefix) == 1 && $13 != "fresh" {
    cell = substr($2, 7) " d" $3; name = substr($4, length(prefix) + 1); key = cell "|" name
    if (!(cell in seen)) { seen[cell] = 1; cells[++n] = cell }
    if (!(key in runs)) { names[cell] = names[cell] "|" name }
    runs[key]++
    if ($7 == "won") { wins[key]++ } else if (!(key in fell) || $9 < fell[key]) { fell[key] = $9 }
  }
  END {
    for (i = 1; i <= n; i++) {
      cell = cells[i]; line = cell ":"
      count = split(substr(names[cell], 2), list, "|")
      for (j = 1; j <= count; j++) {
        key = cell "|" list[j]
        line = line "  " list[j] " " (wins[key] + 0) "/" runs[key] ((key in fell) ? " (w" fell[key] ")" : "")
      }
      print line
    }
  }' "$records" | sort
