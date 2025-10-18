Param(
  [string]$Root = $(Resolve-Path "..").Path
)
$engineDir = Join-Path $Root "src\DuelMasters.Engine"
$backup = Get-ChildItem $engineDir -Directory -Filter "_backup_initialphase_*" | Sort-Object Name -Descending | Select-Object -First 1
if (-not $backup) { Write-Host "No backup folder found."; exit 1 }
Write-Host "Restoring from: $($backup.FullName)"
Copy-Item "$($backup.FullName)\*" $engineDir -Recurse -Force
Write-Host "Done."
