param(
  [Parameter(Mandatory=$true)][string]$OldJson,
  [Parameter(Mandatory=$true)][string]$DbPath,
  [int]$Limit = 100,
  [string]$OutDir = ".\artifacts"
)
# 1) Build tools
Write-Host "[M15.2e] Build Tools" -ForegroundColor Cyan
dotnet build src\DMRules.Tools -v minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# 2) Re-scan unresolved TopN (post-templates)
Write-Host "[M15.2e] Re-scan DB -> NEW JSON" -ForegroundColor Cyan
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$newJson = Join-Path $OutDir ("UNRESOLVED_top{0}_{1}.json" -f $Limit, $stamp)
$newCsv  = Join-Path $OutDir ("UNRESOLVED_top{0}_{1}.csv"  -f $Limit, $stamp)
dotnet run --project src\DMRules.Tools -- "..\..\src\DMRules.Tools\Program.cs" | Out-Null  # no-op ensure restore

# Reuse M15.2c scanner via explicit invocation (if present). Otherwise, user can supply the new JSON manually.
# For convenience, try to run if previous Program exists (from M15.2c).
$scanner = Join-Path "src\DMRules.Tools" "Program.cs"
if (Test-Path $scanner) {
  Write-Host "[M15.2e] Detected scanner (M15.2c). Running it..." -ForegroundColor Yellow
  dotnet run --project src\DMRules.Tools -- "$DbPath" --limit $Limit --output "$OutDir"
  if ($LASTEXITCODE -ne 0) { Write-Warning "Scanner failed. Proceeding to delta if NEW JSON already exists."; }
} else {
  Write-Warning "Scanner source not found. Ensure M15.2c is applied or provide NEW JSON manually."
}

# Find latest NEW JSON in OutDir
$newJsonFile = Get-ChildItem $OutDir -Filter "UNRESOLVED_top*_*.json" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $newJsonFile) { Write-Error "NEW JSON not found in $OutDir. Aborting delta."; exit 3 }

# 3) Run DELTA
Write-Host "[M15.2e] Delta compare" -ForegroundColor Cyan
dotnet run --project src\DMRules.Tools -- delta "$OldJson" "$($newJsonFile.FullName)" --out "$OutDir"
