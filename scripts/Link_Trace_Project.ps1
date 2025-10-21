param(
  [Parameter(Mandatory=$false)]
  [string]$RepoRoot = "C:\Users\米山\DuelMasters"
)
$ErrorActionPreference = "Stop"
Write-Host "=== Linking DMRules.Trace into solution and Engine reference ==="
Push-Location $RepoRoot
try {
  $traceProj = Join-Path $RepoRoot "src\DMRules.Trace\DMRules.Trace.csproj"
  if (-not (Test-Path $traceProj)) {
    throw "DMRules.Trace.csproj not found at $traceProj. Unzip v4 kit to repo root first."
  }
  $sln = Get-ChildItem -Path $RepoRoot -Filter *.sln | Select-Object -First 1
  if ($sln) {
    Write-Host "Adding Trace project to solution $($sln.Name)..."
    dotnet sln $sln add $traceProj | Out-Null
  } else {
    Write-Warning "No .sln found. Skipping sln add."
  }
  $engineProj = Get-ChildItem -Path .\src -Filter "DMRules.Engine.csproj" -Recurse | Select-Object -First 1
  if ($engineProj) {
    Write-Host "Linking Engine -> Trace reference..."
    dotnet add $engineProj.FullName reference $traceProj | Out-Null
  } else {
    Write-Warning "Could not find DMRules.Engine.csproj to set reference; please wire manually if needed."
  }
} finally {
  Pop-Location
}
Write-Host "Done."
