param(
    [string]$TraceDir = "C:\dmtrace"
)
if (-not (Test-Path $TraceDir)) { New-Item -ItemType Directory -Path $TraceDir | Out-Null }
$env:DM_TRACE = "1"
$env:DM_TRACE_DIR = $TraceDir
Write-Host "DM_TRACE=1"
Write-Host "DM_TRACE_DIR=$TraceDir"
