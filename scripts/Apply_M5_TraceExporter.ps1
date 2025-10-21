param(
    [Parameter(Mandatory=$false)]
    [string]$RepoRoot = "C:\Users\米山\DuelMasters"
)

$ErrorActionPreference = "Stop"

Write-Host "=== M5 TraceExporter kit: applying into $RepoRoot ==="

# Ensure target folders
$traceSrc = Join-Path $RepoRoot "src\DMRules.Trace"
$testsDir = Join-Path $RepoRoot "tests\DMRules.Tests"
$scriptsDir = Join-Path $RepoRoot "scripts"

New-Item -ItemType Directory -Force -Path $traceSrc | Out-Null
New-Item -ItemType Directory -Force -Path $testsDir | Out-Null
New-Item -ItemType Directory -Force -Path $scriptsDir | Out-Null

# Copy files from kit into repo
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Copy-Item -Path (Join-Path $here "..\src\DMRules.Trace\*") -Destination $traceSrc -Recurse -Force
Copy-Item -Path (Join-Path $here "..\tests\DMRules.Tests\TraceExporterFixture.cs") -Destination $testsDir -Force

# Add project if not exists
Push-Location $RepoRoot
try {
    if (-not (Test-Path ".\src\DMRules.Trace\DMRules.Trace.csproj")) {
        Write-Host "Creating DMRules.Trace project (classlib, net8.0)..."
        dotnet new classlib -n DMRules.Trace -o .\src\DMRules.Trace --framework net8.0 | Out-Null
    }

    # Ensure package references (none needed beyond BCL)
    # Add project to solution if a .sln exists
    $sln = Get-ChildItem -Path $RepoRoot -Filter *.sln | Select-Object -First 1
    if ($sln) {
        Write-Host "Adding DMRules.Trace to solution $($sln.Name)..."
        dotnet sln $sln add .\src\DMRules.Trace\DMRules.Trace.csproj | Out-Null
    } else {
        Write-Warning "No .sln found at repo root. Skipping sln add."
    }

    # Add reference from Engine to Trace if Engine project exists
    $engineProj = Get-ChildItem -Path .\src -Filter "DMRules.Engine.csproj" -Recurse | Select-Object -First 1
    if ($engineProj) {
        Write-Host "Linking Engine -> Trace reference..."
        dotnet add $engineProj.FullName reference .\src\DMRules.Trace\DMRules.Trace.csproj | Out-Null
    } else {
        Write-Warning "Could not find DMRules.Engine.csproj to set reference; please wire manually if needed."
    }
}
finally {
    Pop-Location
}

Write-Host "Done. Next steps:"
Write-Host "  1) To enable tracing for test runs: set DM_TRACE=1 (system/user env)"
Write-Host "  2) Optional output dir: set DM_TRACE_DIR (default: .\.trace)"
Write-Host "  3) Run: dotnet test tests\DMRules.Tests\DMRules.Tests.csproj -v n"
