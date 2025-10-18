Param(
  [string]$Root = (Resolve-Path "..\..").Path
)
$ErrorActionPreference = "Stop"
$engineDir = Join-Path $Root "src\DuelMasters.Engine"
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $engineDir "_backup_v14_7_$stamp"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
Get-ChildItem $engineDir -Filter *.cs -Recurse | ForEach-Object {
  $dest = Join-Path $backupDir (Resolve-Path $_.FullName | Split-Path -Leaf)
  Copy-Item $_.FullName $dest -Force
}
$patchLog = New-Object System.Collections.Generic.List[string]
function Replace-InFile {
  param([string]$file,[string]$pattern,[string]$replacement,[switch]$Regex)
  $content = Get-Content $file -Raw
  $new = if ($Regex) { [regex]::Replace($content, $pattern, $replacement) } else { $content -replace [regex]::Escape($pattern), $replacement }
  if ($new -ne $content) {
    Set-Content $file $new -Encoding UTF8
    $patchLog.Add("Patched: $($file) :: pattern: $pattern")
    return $true
  }
  return $false
}
# Initial Phase
Get-ChildItem $engineDir -Filter *.cs -Recurse | ForEach-Object {
  $f=$_.FullName
  $a = Replace-InFile -file $f -pattern "Phase\s*=\s*Phase\.Start" -replacement "Phase = Phase.Main" -Regex
  if ($a) { $patchLog.Add("Set Phase.Main in $f") }
  $content = Get-Content $f -Raw
  if ($content -match "InitializeGame\s*\(" -and $content -notmatch "PriorityPlayer\s*=\s*TurnPlayer") {
    $patched = $content -replace "(InitializeGame\s*\([^\)]*\)\s*\{)", "`$1`r`n    state.PriorityPlayer = state.TurnPlayer;"
    if ($patched -ne $content) { Set-Content $f $patched -Encoding UTF8; $patchLog.Add("Set PriorityPlayer in InitializeGame() of $f") }
  }
}
# APNAP
Get-ChildItem $engineDir -Filter *.cs -Recurse | ForEach-Object {
  $f=$_.FullName
  Replace-InFile -file $f -pattern "\.OrderBy\(\s*\w+\s*=>\s*\w+\.Controller\s*\)" -replacement ".OrderBy(t => t.Controller == state.TurnPlayer ? 0 : 1)" -Regex | Out-Null
  Replace-InFile -file $f -pattern "\.ThenBy\(\s*\w+\s*=>\s*\w+\.Controller\s*\)" -replacement ".ThenBy(t => t.Controller == state.TurnPlayer ? 0 : 1)" -Regex | Out-Null
}
# SBA zero power
Get-ChildItem $engineDir -Filter *.cs -Recurse | ForEach-Object {
  $f=$_.FullName
  $content = Get-Content $f -Raw
  if ($content -match "ApplyStateBasedActions\s*\(" -and $content -notmatch "Power\s*<=\s*0") {
    $code = @"
    // v14.7 SBA patch
    var __dying = state.BattleZone.Where(c => c.Power <= 0).ToList();
    if (__dying.Count > 0) {
        foreach (var d in __dying) state.Destroy(d);
        changed = true;
    }
"@
    $patched = $content -replace "(ApplyStateBasedActions\s*\([^\)]*\)\s*\{)", "`$1`r`n$code"
    if ($patched -ne $content) { Set-Content $f $patched -Encoding UTF8; $patchLog.Add("Inserted SBA zero-power in $f") }
  }
}
# Deck-out
Get-ChildItem $engineDir -Filter *.cs -Recurse | ForEach-Object {
  $f=$_.FullName
  $content = Get-Content $f -Raw
  if ($content -match "Draw" -and $content -match "Library" -and $content -notmatch "Lose\(") {
    $code = @"
    // v14.7 Deck-out patch
    if (player.Library.Count == 0) {
        state = state.Lose(player);
        return state;
    }
"@
    $patched = $content -replace "(Draw\s*\([^\)]*\)\s*\{)", "`$1`r`n$code"
    if ($patched -ne $content) { Set-Content $f $patched -Encoding UTF8; $patchLog.Add("Inserted deck-out in $f") }
  }
}
$logFile = Join-Path $engineDir ("_patch_v14_7_log_" + $stamp + ".txt")
$patchLog | Set-Content $logFile -Encoding UTF8
Write-Host "✅ Patch complete. Backup: $backupDir"
Write-Host "📝 Log: $logFile"
Write-Host "Next: dotnet clean; dotnet build; dotnet test"
