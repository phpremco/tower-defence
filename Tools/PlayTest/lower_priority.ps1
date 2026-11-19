# Round 85: see lower_priority.sh. Sets BelowNormal on every Unity process of the worker clones and the stage, and on
# their children; the open editor's project ("Tower defense") is never matched.
$all = Get-CimInstance Win32_Process | Where-Object { $_.Name -like 'Unity*.exe' }
$roots = @($all | Where-Object { $_.CommandLine -like '*TowerDefenseWorkers*' } | ForEach-Object { $_.ProcessId })
$ids = $roots + @($all | Where-Object { $roots -contains $_.ParentProcessId } | ForEach-Object { $_.ProcessId })
foreach ($id in ($ids | Sort-Object -Unique)) {
  try { $p = Get-Process -Id $id -ErrorAction Stop; if ($p.PriorityClass -ne 'BelowNormal') { $p.PriorityClass = 'BelowNormal' } } catch {}
}
