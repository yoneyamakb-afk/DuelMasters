param(
  [Parameter(Mandatory=$true, Position=0)]
  [string]$Pattern,
  [Parameter(Mandatory=$false)]
  [string]$Dir = ".\.trace"
)
$ErrorActionPreference = "Stop"

if (-not (Test-Path $Dir)) { Write-Error ("Not found: {0}" -f $Dir); exit 1 }
Get-ChildItem -Path $Dir -Filter *.jsonl | Sort-Object LastWriteTime | ForEach-Object {
  $file = $_.FullName
  Write-Host ("=== {0} ===" -f $file)
  Get-Content $file | Select-String -Pattern $Pattern
}
