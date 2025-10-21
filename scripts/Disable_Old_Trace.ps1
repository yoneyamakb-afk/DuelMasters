param(
  [Parameter(Mandatory=$false)]
  [string]$RepoRoot = "C:\Users\米山\DuelMasters"
)
$ErrorActionPreference = "Stop"
$old = Join-Path $RepoRoot "src\DMRules.Engine\TraceExporter.cs"
if (Test-Path $old) {
  $dstDir = Join-Path $RepoRoot "src\DMRules.Engine\_disabled"
  New-Item -ItemType Directory -Force -Path $dstDir | Out-Null
  Move-Item -Force $old (Join-Path $dstDir "TraceExporter.cs")
  Write-Host "Old Engine TraceExporter moved to _disabled."
} else {
  Write-Host "No old Engine TraceExporter found. Nothing to disable."
}
