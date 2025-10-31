param(
  [Parameter(Mandatory=$true)][string]$DbPath,
  [int]$Limit = 100,
  [string]$OutDir = ".\artifacts"
)
Write-Host "[M15.2c] Building scanner..." -ForegroundColor Cyan
dotnet build src\DMRules.Tools -v minimal if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[M15.2c] Running scanner..." -ForegroundColor Cyan
dotnet run --project src\DMRules.Tools -- "$DbPath" --limit $Limit --output "$OutDir"
